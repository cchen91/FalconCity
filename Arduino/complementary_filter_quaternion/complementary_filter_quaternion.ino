 // for I2C and serial communication
#include <Wire.h>

// read raw data from imu
#include "imu.h"

// quaternion library
#include "quaternion.h"

double sign(double value) { 
 return double((value>0)-(value<0)); 
}

//////////////////////////////////////////////////////////////////////////////////////

// complementary filter gain
double alpha   = 0.9;

//  use for various timer functions
unsigned long current_time = 0;

// we need an estimate of the gyro bias here (implement!)
double gyroBiasX = -1.7098134994;
double gyroBiasY = 1.2787207365;
double gyroBiasZ = -0.7807808876;

// integrate yaw of gyro
double yawG = 0;

// quaternions for: initial rotation state, gyro-only, accelerometer-only, 
// complementary filter
quaternion qi, qG, qA, qC;

//////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////

// Initializations
void setup()
{
  // Arduino initializations
  Wire.begin();
  Serial.begin(9600);

  // initialize IMU
  initIMU();

  // initialize quaternions
  qi = quaternionFromAngleAxis(1, 0, 0, 0);
  qG = qi; qA = qi;  qC = qi;
  
}

//////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////

// Main loop, read and display data
void loop()
{

  ///////////////////////////////////////////////////////////////////////////////////
  // measure time 
  ///////////////////////////////////////////////////////////////////////////////////
  
  // get current time in milliseconds  
  unsigned long loop_time = millis();

  // time since last update in seconds
  double deltaT = ((double)loop_time - (double)current_time) / 1000.0;

  // update current time             
  current_time = loop_time;

  ///////////////////////////////////////////////////////////////////////////////////
  // get measurements from IMU
  ///////////////////////////////////////////////////////////////////////////////////

  // read IMU data
  double gyrX, gyrY, gyrZ;
  double accX, accY, accZ;
  double magX, magY, magZ;
  readIMU(  gyrX, gyrY, gyrZ, 
            accX, accY, accZ, 
            magX, magY, magZ );

  // remove bias
  gyrX -= gyroBiasX;
  gyrY -= gyroBiasY;
  gyrZ -= gyroBiasZ;

  ///////////////////////////////////////////////////////////////////////////////////
  // gyro integration (implement!)
  ///////////////////////////////////////////////////////////////////////////////////
  
  //  additive integration is from Madgewick's report, the quaternion multiplicative 
  //  version is listed by pretty much everyone else, but we can run into trouble 
  //  due to possible division by 0! but both seem to work fine
  bool bAdditiveIntegration = false;

  // integrate yaw from gyro
  yawG += gyrY*deltaT;

  double gyrXRad = gyrX * DEG_TO_RAD;
  double gyrYRad = gyrY * DEG_TO_RAD;
  double gyrZRad = gyrZ * DEG_TO_RAD;


  // norm of gyro measurements
  double gyrMagnitude = sqrt( gyrXRad*gyrXRad + gyrYRad*gyrYRad + gyrZRad*gyrZRad);

  // make incremental rotation quaternion
  quaternion qDelta = quaternionFromAngleAxis( RAD_TO_DEG*gyrMagnitude*deltaT, gyrXRad/gyrMagnitude, gyrYRad/gyrMagnitude, gyrZRad/gyrMagnitude);
  if (gyrMagnitude<0.000000001) {
    qDelta.s    = 1;
    qDelta.v[0] = 0;
    qDelta.v[1] = 0;
    qDelta.v[2] = 0;
  }

  // temporal integration of gyro-only quaternion
  qG = quaternionMult(qG,qDelta);    
  qG = quaternionNormalize(qG);

  // temporal integration of complementary filter quat
  qC = quaternionMult(qC,qDelta);
  qC = quaternionNormalize(qC);

  ///////////////////////////////////////////////////////////////////////////////////
  // quaternion from accelerometer (implement!)
  ///////////////////////////////////////////////////////////////////////////////////

  // tilt from accelerometer
  double yaw    = yawG;
  double pitch  = -atan2(accZ, sign(accY)*sqrt(accX*accX+accY*accY))*RAD_TO_DEG;
  double roll   = -atan2(-accX, accY)*RAD_TO_DEG; 

  // this is just for visualizion, it's not used in the complementary filter
  quaternion qA = quaternionFromEuler(yaw, pitch, roll);

  ///////////////////////////////////////////////////////////////////////////////////
  // complementary filter (implement!)
  ///////////////////////////////////////////////////////////////////////////////////

  // acc vector quaternion in sensor frame
  quaternion qAS;
  qAS.s=0;
  qAS.v[0] = accX;
  qAS.v[1] = accY;
  qAS.v[2] = accZ; 
  
  // rotate acceleration vector into inertial frame
  // note: this is different from Oculus paper (other way around)
  quaternion qAI = quaternionRotation(qAS,qC); 
  
  // norm of rotated vector
  double qAIvecNorm = sqrt( qAI.v[0]*qAI.v[0] + qAI.v[1]*qAI.v[1] + qAI.v[2]*qAI.v[2] );
  
  // compute angle to (0 1 0) axis (inner product between (0,1,0) and normalized, 
  //  rotated acc vector
  double phi = acos(qAI.v[1]/qAIvecNorm)*RAD_TO_DEG;
  
  // rotation axis is cross product between acc vector in inertial coordinates and 0 1 0
  double tiltAxisX = -qAI.v[2];
  double tiltAxisY = 0;
  double tiltAxisZ = qAI.v[0];
  
  // normalize tilt axis
  double tiltAxisNorm = sqrt( tiltAxisX*tiltAxisX + tiltAxisY*tiltAxisY + tiltAxisZ*tiltAxisZ );
  tiltAxisX /= tiltAxisNorm;
  tiltAxisY /= tiltAxisNorm;
  tiltAxisZ /= tiltAxisNorm;
  
  // complementary filter
  quaternion qAIncr = quaternionFromAngleAxis((1-alpha)*phi, tiltAxisX, tiltAxisY, tiltAxisZ);
  qAIncr = quaternionNormalize(qAIncr);
  
  qC = quaternionMult(qAIncr,qC);
  qC = quaternionNormalize(qC);

  ///////////////////////////////////////////////////////////////////////////////////
  // plot to serial plotter (great for debugging, but don't use when 
  //  streaming to OpenGL!)
  ///////////////////////////////////////////////////////////////////////////////////
      
  // this is for debugging on your serial plotter (turn off if not needed)
  // 0 - plot nothing 
  // 1 - plot pitch
  // 2 - plot roll
  // 3 - plot yaw
  int plotOption = 0;

  if (plotOption!=0) {
    
    double yawG, pitchG, rollG;
    quaternionToEuler(qG, yawG, pitchG, rollG);
  
    double yawA, pitchA, rollA;
    quaternionToEuler(qA, yawA, pitchA, rollA);
  
    double yawC, pitchC, rollC;
    quaternionToEuler(qC, yawC, pitchC, rollC);
  
    switch(plotOption) {
      
      // pitch from gyro, accelerometer, complementary filterL
      case 1:
        Serial.print (pitchG);
        Serial.print (" ");
        Serial.print (pitchA);
        Serial.print (" ");
        Serial.println (pitchC);  
        break;
  
      // roll from gyro, accelerometer, complementary filterL
      case 2:
        Serial.print (rollG);
        Serial.print (" ");
        Serial.print (rollA);
        Serial.print (" ");
        Serial.println (rollC);
        break;
  
      // yaw from gyro, accelerometer, complementary filter
      case 3:
        Serial.print (yawG);
        Serial.print (" ");
        Serial.print (yawA);
        Serial.print (" ");
        Serial.println (yawC);
        break;
    }
  }

  ///////////////////////////////////////////////////////////////////////////////////
  // stream quaternions to serial port for use with OpenGL
  ///////////////////////////////////////////////////////////////////////////////////
  bool bStreamToOpenGL = true;
      
  if (bStreamToOpenGL) {

//    // rotation quaternion from gyro
//    Serial.print(F("QG "));
//    Serial.print (qG.s,DEC); 
//    Serial.print (" ");
//    Serial.print (qG.v[0],DEC);
//    Serial.print (" ");
//    Serial.print (qG.v[1],DEC); 
//    Serial.print (" ");
//    Serial.println (qG.v[2],DEC); 
//
//    // rotation quaternion from accelerometer
//    Serial.print(F("QA "));
//    Serial.print (qA.s,DEC); 
//    Serial.print (" ");
//    Serial.print (qA.v[0],DEC);
//    Serial.print (" ");
//    Serial.print (qA.v[1],DEC); 
//    Serial.print (" ");
//    Serial.println (qA.v[2],DEC);

    // rotation quaternion from complementary filter
    Serial.print(F("QC "));
    Serial.print (qC.s,DEC); 
    Serial.print (" ");
    Serial.print (qC.v[0],DEC);
    Serial.print (" ");
    Serial.print (qC.v[1],DEC); 
    Serial.print (" ");
    Serial.println (qC.v[2],DEC);
  }
       
}

