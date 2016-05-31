// define structure for quaterion
typedef struct quaternion {
  double s;     // the scalar component
  double v[3];  // the axis
} quaternion;

//////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////
// overview of functions

// quaternion multiplication (not commutative)
quaternion quaternionMult(  quaternion q, quaternion p );

// inverse of a quaternion
quaternion quaternionInv( quaternion q );

// rotate a quaternion q by a rotation quaternion r as r*q*r^{-1}
quaternion quaternionRotation( quaternion q, quaternion r );

// normalize quaternion
quaternion quaternionNormalize(  quaternion q );

// make a quaternion from axis and angle (in degrees) 
quaternion quaternionFromAngleAxis(double angle, double axisX, double axisY, double axisZ);

// convert quaternion to axis and angle (in degrees) 
void quaternionToAngleAxis(quaternion q, double &angle, double &axisX, double &axisY, double &axisZ);

// convert quaternion to axis and angle (in degrees) 
void quaternionToEuler(quaternion q, double &yaw, double &pitch, double &roll);

// linear interpolation between quaternions
quaternion quaternionInterp(quaternion q, quaternion p, double alpha) ;

//////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////
// multiply two quaternions
quaternion 
quaternionMult(  quaternion q, quaternion p )
{ 
  // output 
  quaternion resultQ;
  
  // scalar part
  resultQ.s = q.s*p.s - q.v[0]*p.v[0] - q.v[1]*p.v[1] - q.v[2]*p.v[2];
  
  // vectorial part
  resultQ.v[0] = q.s*p.v[0] + q.v[0]*p.s    + q.v[1]*p.v[2] - q.v[2]*p.v[1];
  resultQ.v[1] = q.s*p.v[1] - q.v[0]*p.v[2] + q.v[1]*p.s    + q.v[2]*p.v[0];  
  resultQ.v[2] = q.s*p.v[2] + q.v[0]*p.v[1] - q.v[1]*p.v[0] + q.v[2]*p.s;

  return resultQ;
}

//////////////////////////////////////////////////////////////////////////////////////
// inverse of a quaternion
quaternion 
quaternionInv( quaternion q )
{
  double qSum = q.s*q.s + q.v[0]*q.v[0] + q.v[1]*q.v[1] + q.v[2]*q.v[2];
  q.s    = q.s/qSum;
  q.v[0] = -q.v[0]/qSum;
  q.v[1] = -q.v[1]/qSum;
  q.v[2] = -q.v[2]/qSum;
  return q;
}

//////////////////////////////////////////////////////////////////////////////////////
// rotate a quaternion q by a rotation quaternion r as r*q*r^{-1}
quaternion 
quaternionRotation( quaternion q, quaternion r )
{
  return quaternionMult(quaternionMult(r,q),quaternionInv(r)); 
}

//////////////////////////////////////////////////////////////////////////////////////
// norm of quaternion

double
quaternionLength( quaternion q )
{
  return sqrt( q.s*q.s + q.v[0]*q.v[0] + q.v[1]*q.v[1] + q.v[2]*q.v[2] );
}

//////////////////////////////////////////////////////////////////////////////////////
// normalize quaternion
quaternion 
quaternionNormalize(  quaternion q )
{
  // compute norm of 
  float qNorm = quaternionLength(q);

  q.s    = q.s/qNorm;
  q.v[0] = q.v[0]/qNorm;
  q.v[1] = q.v[1]/qNorm;
  q.v[2] = q.v[2]/qNorm;
  return q;
}

//////////////////////////////////////////////////////////////////////////////////////
// axis and angle (in degrees) to quaternion - axis is assumed to be normalized

quaternion
quaternionFromAngleAxis(double angle, double axisX, double axisY, double axisZ)
{  
  quaternion q;  

  double theta_half = DEG_TO_RAD*angle/2.0;

  // scalar
  q.s    = cos(theta_half);

  // vector part
  double s = sin(theta_half);
  q.v[0] = s*axisX;
  q.v[1] = s*axisY;
  q.v[2] = s*axisZ;  

  return q;  
}

//////////////////////////////////////////////////////////////////////////////////////
// quaternion from axis and angle (in degrees) 

void 
quaternionToAngleAxis(quaternion q, double &angle, double &axisX, double &axisY, double &axisZ)
{
  // compute angle  
  angle = 2*acos(q.s);
  angle = 360 * angle / (2*PI);

  // compute axis
  double qscale  = 1.0 / sqrt(1 - q.s*q.s);
  axisX =  q.v[0]*qscale;
  axisY =  q.v[1]*qscale;
  axisZ =  q.v[2]*qscale;
}

//////////////////////////////////////////////////////////////////////////////////////
// quaternion to euler angles (in degrees), adopted from from glm library 

void
quaternionToEuler(quaternion q, double &yaw, double &pitch, double &roll)
{
  yaw   = asin(double(-2) * (q.v[0] * q.v[2] - q.s * q.v[1]));
  pitch = atan2(double(2) * (q.v[1] * q.v[2] + q.s * q.v[0]), q.s * q.s - q.v[0] * q.v[0] - q.v[1] * q.v[1] + q.v[2] * q.v[2]);
  roll  = atan2(double(2) * (q.v[0] * q.v[1] + q.s * q.v[2]), q.s * q.s + q.v[0] * q.v[0] - q.v[1] * q.v[1] - q.v[2] * q.v[2]);

  // convert radians to degrees
  yaw   = yaw   * RAD_TO_DEG;
  pitch = pitch * RAD_TO_DEG;
  roll  = roll  * RAD_TO_DEG;
}

//////////////////////////////////////////////////////////////////////////////////////
// quaternion from euler angles (in degrees), adopted from from glm library 
quaternion 
quaternionFromEuler(double yaw, double pitch, double roll)
{ 
  double cx = cos(DEG_TO_RAD*pitch * double(0.5));
  double cy = cos(DEG_TO_RAD*yaw * double(0.5));
  double cz = cos(DEG_TO_RAD*roll * double(0.5));
    
  double sx = sin(DEG_TO_RAD*pitch * double(0.5));
  double sy = sin(DEG_TO_RAD*yaw * double(0.5));
  double sz = sin(DEG_TO_RAD*roll * double(0.5));
     
  quaternion q;
  q.s     = cx * cy * cz + sx * sy * sz;
  q.v[0]  = sx * cy * cz - cx * sy * sz;
  q.v[1]  = cx * sy * cz + sx * cy * sz;
  q.v[2]  = cx * cy * sz - sx * sy * cz;    

  return quaternionNormalize(q); 
}

//////////////////////////////////////////////////////////////////////////////////////
// linear interpolation between two quaternions: alpha*q + (1-alpha)*p 

quaternion
quaternionInterp(quaternion q, quaternion p, double alpha) 
{
  // http://run.usc.edu/cs520-s15/quaternions/quaternions-cs520.pdf

  double theta = acos(q.s*p.s + q.v[0]*p.v[0] + q.v[1]*p.v[1] + q.v[2]*p.v[2]);

  double scale1 = sin(alpha*theta)/sin(theta);
  double scale2 = sin((1-alpha)*theta)/sin(theta); 

  quaternion resultQ;
  resultQ.s = scale1*q.s + scale2*p.s;
  resultQ.v[0] = scale1*q.v[0] + scale2*p.v[0];
  resultQ.v[1] = scale1*q.v[1] + scale2*p.v[1];
  resultQ.v[2] = scale1*q.v[2] + scale2*p.v[2];

  return resultQ;
}

