//********************************************************
//Original Version: NovintFalconの正しい使い方 - Unity Advent Calendar 2013 vol.23
//http://yunojy.github.io/blog/2013/12/23/how-to-use-novintfalcon-unity-advent-calendar-2013-vol-dot-23/
/// Use NovintFalcon as an operating device script ※ for Windows (dll dependent)
///
/// Usage:
/// 1. http://forum.unity3d.com/threads/6494-Novint-falcon
/// Falcon Wrapper Source - copy Update V1.zip the DL, the FalconWrapper.dll to Assets and the same hierarchy
/// Or after build put FalconWrapper.dll where you are through the Windows Path, put the binaries and the same hierarchy
/// 2. In this script to the project
/// 3. the operation you want to target GameObject Add Component Judging by suitably toying with 
/// 4. Inspector

/// How to Haptic the movement of the object to NovintFalcon is _feedback reference

//*********************************************************
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

public class FalconUnity2 : MonoBehaviour {

	#region FalconWrapper.dll Variables
  
	const string falcon = "Falcon Wrapper";

	[DllImport(falcon)]
	private static extern void StartHaptics();
	[DllImport(falcon)]
	private static extern void StopHaptics();
	[DllImport(falcon)]
	private static extern bool IsDeviceCalibrated();
	[DllImport(falcon)]
	private static extern bool IsDeviceReady();
	[DllImport(falcon)]
	private static extern double GetXPos();
	[DllImport(falcon)]
	private static extern double GetYPos();
	[DllImport(falcon)]
	private static extern double GetZPos();
	[DllImport(falcon)]
	private static extern void SetServo(double[] speed);
	[DllImport(falcon)]
	private static extern void SetServoPos(double[] pos, double strength);
	[DllImport(falcon)]
	private static extern bool IsHapticButtonDepressed();
	[DllImport(falcon)]
	private static extern int GetButtonsDown();
	[DllImport(falcon)]
	private static extern bool isButton0Down();
	[DllImport(falcon)]
	private static extern bool isButton1Down();
	[DllImport(falcon)]
	private static extern bool isButton2Down();
	[DllImport(falcon)]
	private static extern bool isButton3Down();

	public static FalconUnity2 main;
	#endregion

	#region Variables
	[Range(-10f, 100f)]
	float SpeedX = 0f;
	[Range(-10f, 100f)]
	float SpeedY = 0f;
	[Range(-10f, 100f)]
	float SpeedZ = 0f;

	[Range(-1.5f, 1.5f)]
	float PosX = 0f;
	[Range(-1.5f, 1.5f)]
	float PosY = 0f;
	[Range(-1.5f, 1.5f)]
	float PosZ = 0f;

	[Range(0f, 100f)]
	float Strength = 0f;

	protected CharacterController Charactor = null;
	private Vector3 MoveThrottle            = Vector3.zero;
	public float TranslationSensitivity     = 10.0f;
    float moveSpeedConst = 1.0f;
    
    
    Vector3 initialPos = Vector3.zero;
    Vector3 origContactPoint = Vector3.zero;
    Vector3 origContactObjectPoint = Vector3.zero;
    Vector3 contactNormal = Vector3.zero;
   
    //for each texture
    float stiffness = 50.0f;
    float surfaceStrength = 50.0f;
    float viscocity = 10.0f;
    int flag = 0; //1: surface, 2: water, 3: sand, 4: pop

    //marker to see where force is being guided towards
    public GameObject guideObject;

	#endregion
     
	//-------------------------------------------------
	#region Start()

	void Start() {
        Debug.Log("Starting code.");
		StartHaptics();
        initialPos = gameObject.transform.position;
		StartCoroutine(_initHaptics());
		Charactor = gameObject.GetComponent<CharacterController>();
	}
	
	private IEnumerator _initHaptics() {
		while (!IsDeviceCalibrated()) {
			Debug.LogWarning("Please calibrate the device!");
			yield return new WaitForSeconds(1.5f);
		}
		if (!IsDeviceReady())
			Debug.LogError("Device is not ready!");
		main = this;
	}

	#endregion

	//-------------------------------------------------
	#region Update()

	void Update() {
		// Setting movement of device
		_feedback();
		// Setting movement of character
		//_charaMove();

		gameObject.transform.position = (new Vector3((float)GetXPos(), (float)GetYPos(), -(float)GetZPos()));
        //Debug.Log("Position just set to " + gameObject.transform.position.ToString("G4"));
		//Debug.Log(isButton0Down() + " , " + isButton1Down() + " , " + isButton2Down() + " , " + isButton3Down());
	}

    #region Collision
    private void OnCollisionEnter(Collision c)
    {
		contactNormal = c.contacts[0].normal;
		//Debug.Log("OnCollisionEnter entered, normal is " + contactNormal.ToString());
		origContactObjectPoint = gameObject.transform.position;
		origContactPoint = c.contacts[0].point;
        if(c.collider.tag == "Surface")		flag = 1;
        else if(c.collider.tag == "Water")	flag = 2;
        else if(c.collider.tag == "Sand")	flag = 3;
        else if(c.collider.tag == "PopEffect")	flag = 4;
    }

    private void OnCollisionStay(Collision c)
    {
       // Debug.Log("Within OnCollision, gameObject.position is " + gameObject.transform.position.ToString("G4"));
        Vector3 vecToOriginal = c.contacts[0].point - origContactObjectPoint;

        float ang = Mathf.Deg2Rad * Vector3.Angle(vecToOriginal, contactNormal);
        float scalarProj = vecToOriginal.magnitude * Mathf.Cos(ang);
        //Debug.Log("normal magnitude" + contactNormal.magnitude);
        Vector3 vectorProj = scalarProj * (contactNormal);
       // Debug.DrawLine(c.contacts[0].point, c.contacts[0].point-vectorProj, Color.red);
        if(c.collider.tag == "Surface")
        {
            Vector3 normalOutsidePos = c.contacts[0].point - vectorProj;
            //guideObject.transform.position = normalOutsidePos;
            PosX = normalOutsidePos.x;
            PosY = normalOutsidePos.y;
            PosZ = normalOutsidePos.z;
            Strength = surfaceStrength;
            //float scale = vectorProj.sqrMagnitude;
            //Debug.Log("Amount of Strength is " + vectorProj.magnitude);
        }
        else if(c.collider.tag == "Water")
        {
        	SpeedX = viscocity*vectorProj.x;
        	SpeedY = viscocity*vectorProj.y;
        	SpeedZ = viscocity*vectorProj.z;
        }
        else if(c.collider.tag == "Sand") 
        {
        	Vector3 normalOutsidePos = c.contacts[0].point - vectorProj;
            //Debug.Log("Oncollisionstay detected, strength is " + Strength + ", difference between object and goal is " + (normalOutsidePos - gameObject.transform.position).ToString("G4"));
            //Debug.Log("goalPos is " + normalOutsidePos.ToString("G4") + ", and object pos is " + gameObject.transform.position.ToString("G4"));
            //guideObject.transform.position = normalOutsidePos;

            float posrand = Random.value * 0.01f; 

            PosX = normalOutsidePos.x + posrand;
            PosY = normalOutsidePos.y + posrand;
            PosZ = normalOutsidePos.z + posrand;
            Strength = surfaceStrength;
            //float scale = vectorProj.sqrMagnitude;
            //Debug.Log("Amount of Strength is " + vectorProj.magnitude);
        }
        else if(c.collider.tag == "PopEffect")
        {
            Vector3 vectorMoved = GetServoPos() - origContactObjectPoint;
            float distanceMoved = vectorMoved.magnitude;
        	Debug.Log("Distance moved is" + distanceMoved);
            if (distanceMoved < 0.5)
            {
        		//SpeedX = -viscocity*vectorProj.x;
        		//SpeedY = -viscocity*vectorProj.y;
        		//SpeedZ = -viscocity*vectorProj.z;
        		SpeedX = contactNormal.x*distanceMoved*15;
        		SpeedY = contactNormal.y*distanceMoved*15;
        		SpeedZ = contactNormal.z*distanceMoved*15;
        		c.collider.transform.position = GetServoPos();
            }
            else if (distanceMoved < 1) 
            {
            	SpeedX = 0;
            	SpeedY = 0;
            	SpeedZ = 0;
            	c.collider.transform.position = GetServoPos();
            }
        }
        //SetServo(new double[3] { forceVector.x, forceVector.y, forceVector.z });
    }

    private void OnCollisionExit(Collision c)
    {
    	// Reset everything when leaving collision
        SpeedX = 0.0f;
        SpeedY = 0.0f;
        SpeedZ = 0.0f;
        Strength = 0.0f;
        PosX = 0.0f;
        PosY = 0.0f;
        PosZ = 0.0f;
        flag = 0;
        Debug.Log("Leaving collision");
    }

    #endregion


    #region ForceUpdate

	private void _feedback() {
		// Return the grip of NovintFalcon to deformation position
		//SetServo(new double[3] { SpeedX, SpeedY, -SpeedZ });
        //Debug.Log("Position being fed to setservopos as " + PosX + ", " + PosY + ", " + PosZ);
        if (flag == 1)	SetServoPos(new double[3] { PosX, PosY, -PosZ }, Strength);
        else if (flag == 2)	SetServo(new double[3] { SpeedX, SpeedY, SpeedZ });
        else if (flag == 3)	SetServoPos(new double[3] { PosX, PosY, -PosZ }, Strength);
        else if (flag == 4)	SetServo(new double[3] { SpeedX, SpeedY, SpeedZ });
        else if (flag == 0)	SetServo(new double[3] { 0.0f, 0.0f, 0.0f });
        //SetServo(new double[3] { SpeedX, SpeedY, SpeedZ });
		//Debug.Log(GetServoPos());
	}

	private void _charaMove() {
		if ((float)GetZPos() >= 0.8f)  MoveThrottle += __moveVector(Vector3.forward);
		if ((float)GetZPos() <= -0.8f) MoveThrottle += __moveVector(Vector3.back);
		if ((float)GetXPos() >= 0.8f)  MoveThrottle += __moveVector(Vector3.left);
		if ((float)GetXPos() <= -0.8f) MoveThrottle += __moveVector(Vector3.right);
		if ((float)GetYPos() >= 0.8f)  MoveThrottle += __moveVector(Vector3.up * 2);
		if ((float)GetYPos() <= -0.8f) MoveThrottle += __moveVector(Vector3.down);

		float motorDamp = (1.0f + 0.15f);
		MoveThrottle.x /= motorDamp;
		MoveThrottle.y = (MoveThrottle.y > 0.0f) ? (MoveThrottle.y / motorDamp) : MoveThrottle.y;
		MoveThrottle.z /= motorDamp;

		Charactor.Move(MoveThrottle);
	}

	private Vector3 __moveVector(Vector3 moveVector) {
		return Charactor.transform.TransformDirection(
			moveVector * Time.deltaTime * TranslationSensitivity);
	}

	#endregion

    #endregion

    //-------------------------------------------------
	#region Misc
	
	void OnApplicationQuit() {
		StopHaptics();
	}
	public Vector3 GetServoPos() {
		return new Vector3((float)GetXPos(), (float)GetYPos(), -(float)GetZPos());
	}
	
	#endregion
}