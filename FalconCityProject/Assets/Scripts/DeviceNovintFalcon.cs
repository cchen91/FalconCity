using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

/// <summary>
/// NovintFalconを操作デバイスとして使うscript ※Windows用（dll依存）
///
/// usage:
///  1. http://forum.unity3d.com/threads/6494-Novint-falcon から
///     Falcon Wrapper Source - Update V1.zip をDL、FalconWrapper.dllをAssetsと同階層へコピー
///     ビルド後はWindowsのPathが通っているところにFalconWrapper.dllを置くか、バイナリと同階層に置く
///  2. このスクリプトをプロジェクトへIn
///  3. 操作したい対象のGameObjectにAdd Component
///  4. Inspectorで適当に弄って察する
/// ※対象の動きをNovintFalconへHapticする方法は_feedback参照
///
/// NovintFalconの正しい使い方 - Unity Advent Calendar 2013 vol.23
/// http://yunojy.github.io/blog/2013/12/23/how-to-use-novintfalcon-unity-advent-calendar-2013-vol-dot-23/
/// </summary>
public class DeviceNovintFalcon : MonoBehaviour {

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

	public static DeviceNovintFalcon main;
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
		// デバイス動作制御
		_feedback();
		// キャラクター動作制御
		//_charaMove();

		gameObject.transform.position = (new Vector3((float)GetXPos(), (float)GetYPos(), -(float)GetZPos()));
        //Debug.Log("Position just set to " + gameObject.transform.position.ToString("G4"));
		//Debug.Log(isButton0Down() + " , " + isButton1Down() + " , " + isButton2Down() + " , " + isButton3Down());
	}

    #region Collision
    private void OnCollisionEnter(Collision c)
    {
        if(c.collider.tag == "Surface")
        {
            contactNormal = c.contacts[0].normal;
            Debug.Log("OnCollisionEnter entered, normal is " + contactNormal.ToString());
            origContactObjectPoint = gameObject.transform.position;
            origContactPoint = c.contacts[0].point;
        }
    }

    private void OnTriggerEnter(Collider c) //when triggering walll
    {
        //Debug.Log("Entered contact with something.");
        //Debug.Log("entering trigger."  );
        if(c.tag == "Surface" || c.tag == "Ground")
        {
           // Debug.Log("Entered contact with " + c.tag);

            //contactPoint = gameObject.transform.position;
            //PosX = gameObject.transform.position.x;
            //PosY = gameObject.transform.position.y;
            //PosZ = gameObject.transform.position.z;
            //Strength = 50.0f;
        }
        
        //_feedback();
        //SetServo(new double[3] { SpeedX, SpeedY, SpeedZ });
        //SetServoPos(new double[3] {x,y,z}, 30.0f);
    }

    private void OnCollisionStay(Collision c)
    {
       // Debug.Log("Within OnCollision, gameObject.position is " + gameObject.transform.position.ToString("G4"));
        if(c.collider.tag == "Surface")
        {
            Vector3 vecToOriginal = c.contacts[0].point - origContactObjectPoint;
            //Debug.DrawLine(c.contacts[0].point, origContactPoint,Color.black);

            float ang = Mathf.Deg2Rad * Vector3.Angle(vecToOriginal, contactNormal);
            float scalarProj = vecToOriginal.magnitude * Mathf.Cos(ang);
            Debug.Log("normal magnitude" + contactNormal.magnitude);
            Vector3 vectorProj = scalarProj * (contactNormal);

            Debug.DrawLine(c.contacts[0].point, c.contacts[0].point-vectorProj, Color.red);
            Vector3 normalOutsidePos = c.contacts[0].point - vectorProj;
            //Debug.Log("Oncollisionstay detected, strength is " + Strength + ", difference between object and goal is " + (normalOutsidePos - gameObject.transform.position).ToString("G4"));
            //Debug.Log("goalPos is " + normalOutsidePos.ToString("G4") + ", and object pos is " + gameObject.transform.position.ToString("G4"));
            guideObject.transform.position = normalOutsidePos;

            PosX = normalOutsidePos.x;
            PosY = normalOutsidePos.y;
            PosZ = normalOutsidePos.z;
            Strength = surfaceStrength;
            float scale = vectorProj.sqrMagnitude;
            Debug.Log("Amount of Strength is " + vectorProj.magnitude);
            //SpeedX = -stiffness*vectorProj.x;
            //SpeedY = -stiffness*vectorProj.y;
            //SpeedZ = -stiffness*vectorProj.z;
        }
        
        //SetServo(new double[3] { forceVector.x, forceVector.y, forceVector.z });
    }

    private void OnCollisionExit(Collision c)
    {
        if(c.collider.tag == "Surface")
        {
            //Reset everything
            SpeedX = 0.0f;
            SpeedY = 0.0f;
            SpeedZ = 0.0f;
            Strength = 0.0f;
        }
        
    }

    #endregion


    #region ForceUpdate

	private void _feedback() {
		// NovintFalconのグリップをデフォ位置に戻す
		//SetServo(new double[3] { SpeedX, SpeedY, -SpeedZ });
        //Debug.Log("Position being fed to setservopos as " + PosX + ", " + PosY + ", " + PosZ);		
        SetServoPos(new double[3] { PosX, PosY, -PosZ }, Strength);
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