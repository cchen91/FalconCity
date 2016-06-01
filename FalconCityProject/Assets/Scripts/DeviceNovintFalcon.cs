using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

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
    float surfaceStrength = 50.0f;
    float viscocity = 7.0f;
    int flag = 0; //1: surface, 2: water, 3: sand, 4: pop
    bool somethingInTouch; //allow only one collision at once
    GameObject touchingObject;

    //marker to see where force is being guided towards
    public GameObject guideObject;
    public GameObject rockpile;
    float lastRockpileUpdateTime;

    //to grab buildings
    bool isBuildingInTouch;
    GameObject buildingInTouch;
    bool isGrabbingBuilding;
    GameObject grabbedBuilding;
    BoxCollider grabbedBuildingCollider;

    public GameObject explosion;
    public GameObject fire;
    public AudioClip screaming;
    public AudioClip splash;
    AudioSource thisAudioSource;
    bool isPeaceful = true;

	#endregion
     
	//-------------------------------------------------
	#region Start()

	void Start() {
        Debug.Log("Starting code.");
		StartHaptics();
        initialPos = gameObject.transform.position;
		StartCoroutine(_initHaptics());
		Charactor = gameObject.GetComponent<CharacterController>();
        grabbedBuildingCollider = gameObject.AddComponent<BoxCollider>();
        grabbedBuildingCollider.enabled = false;
        thisAudioSource = gameObject.GetComponent<AudioSource>();

        somethingInTouch = false;
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

        //hand tries to grab
        MainButtonControl();
    
    }

    #region Button
    void MainButtonControl()
    {
        if(isButton0Down())
        {
            if (isPeaceful)
            {
                isPeaceful = false;
                thisAudioSource.PlayOneShot(screaming);
            }

            //If button is pressed but I'm not grabbing a building that I'm touching
            if (isBuildingInTouch && grabbedBuilding != buildingInTouch) 
            { //then grab it! 
                isGrabbingBuilding = true;
                gameObject.GetComponent<SphereCollider>().enabled = false; //remove this hand's box
                resetHapticParams(); //let go of any force 
                
                //attach building as child object
                grabbedBuilding = buildingInTouch;
                grabbedBuilding.transform.parent = gameObject.transform;

                //copy collider
                //CopyColliderProperties(grabbedBuildingCollider, grabbedBuilding); 
                //grabbedBuildingCollider.enabled = true;

                //reset touching seting
                somethingInTouch = false;

                ///gameObject.AddComponent(grabbedBuilding.GetComponent<Collider>());

                //elements of destruction
                if(grabbedBuilding.GetComponent<BuildingScript>().destroyed == false)
                    GameObject.Instantiate(explosion,grabbedBuilding.transform.position, new Quaternion()); //BAM!! But only once
                GameObject.Instantiate(fire, grabbedBuilding.transform.position, new Quaternion()); //create flame!!

                
                grabbedBuilding.transform.localPosition = new Vector3(0, -grabbedBuilding.transform.lossyScale.y / 2, 0);
                grabbedBuilding.GetComponent<BuildingScript>().getsGrabbed();
            }
        }
        else
        {
            if(grabbedBuilding != null) //button released but there still is a grabbed building.
            {
                isGrabbingBuilding = false;
             
                grabbedBuilding.transform.parent = null;
                //grabbedBuildingCollider.enabled = false;
                gameObject.GetComponent<SphereCollider>().enabled = true;
                grabbedBuilding.GetComponent<BuildingScript>().letGo();

                grabbedBuilding = null;
            }
        }
    }
    #endregion


    #region Collision
    public void OnCollisionEnter(Collision c)
    {

        Debug.Log("SomethinginTouch is " + somethingInTouch + " and touchingobject currently is " + touchingObject);

        //ignore irrevelent colliders
        if ((somethingInTouch && c.collider.gameObject != touchingObject) ||
            (c.collider.tag != "Surface" && c.collider.tag != "Water" && c.collider.tag != "Sand" && c.collider.tag != "Pop")) return;

        Debug.Log("OnCollisionEnter entered, after filter and collision type is " + c.collider.tag + "and object name is " + c.collider.gameObject.name);


        somethingInTouch = true;
        touchingObject = c.collider.gameObject;

        contactNormal = c.contacts[0].normal;
        origContactObjectPoint = gameObject.transform.position;
        origContactPoint = c.contacts[0].point;

        if (c.collider.tag == "Surface") flag = 1;
        else if (c.collider.tag == "Water")
        {
            thisAudioSource.PlayOneShot(splash);
            flag = 2;
        }
        else if (c.collider.tag == "Sand") flag = 3;
        else if (c.collider.tag == "PopEffect") flag = 4;
    }

    public void OnCollisionStay(Collision c)
    {
        //Debug.Log("Tag is " + c.collider.tag + ", flag is " + flag);

        //ignore irrevelent colliders
        if ((somethingInTouch && c.collider.gameObject != touchingObject) || 
            (c.collider.tag != "Surface" && c.collider.tag != "Water" && c.collider.tag != "Sand" && c.collider.tag != "Pop")) return;

        Debug.Log("OnCollisionStay running. Tag is " + c.collider.tag + ", flag is " + flag);

        //Debug.Log("Within OnCollision, gameObject.position is " + gameObject.transform.position.ToString("G4"));
        Vector3 vecToOriginal = c.contacts[0].point - origContactObjectPoint;

        float ang = Mathf.Deg2Rad * Vector3.Angle(vecToOriginal, contactNormal);
        float scalarProj = vecToOriginal.magnitude * Mathf.Cos(ang);
        Vector3 vectorProj = scalarProj * (contactNormal);

        if (c.collider.tag == "Surface")
        {
            isBuildingInTouch = true;
            buildingInTouch = c.collider.gameObject;
            surfaceHaptics(c, vectorProj);
        }
        else if (c.collider.tag == "Water")
        {
            waterHaptics(c, vectorProj);
        }
        else if (c.collider.tag == "Sand")
        {
            sandHaptics(c, vectorProj);
        }
        else if (c.collider.tag == "PopEffect")
        {
            popHaptics(c, vectorProj);
        }

    }

    void surfaceHaptics(Collision c, Vector3 vectorProj)
    {
       // Debug.Log("Surface object in contact is " + c.gameObject.name);
        float stiffnesss = c.gameObject.GetComponent<BuildingScript>().stiffness;
        Vector3 normalOutsidePos = c.contacts[0].point - vectorProj;
        PosX = normalOutsidePos.x;
        PosY = normalOutsidePos.y;
        PosZ = normalOutsidePos.z;
        Strength = surfaceStrength;
    }

    void waterHaptics(Collision c, Vector3 vectorProj)
    {
        //vectorProj = vectorProj;
        SpeedX = -viscocity * vectorProj.x;
        SpeedY = -viscocity * vectorProj.y;
        SpeedZ = -viscocity * vectorProj.z;
    }

    void sandHaptics(Collision c, Vector3 vectorProj)
    {
        float posrand = Random.value * 0.001f;
        Vector3 basePoint = gameObject.transform.position;

        if(Time.time > lastRockpileUpdateTime + 0.5)
        {
            GameObject.Instantiate(rockpile,
              new Vector3(basePoint.x, basePoint.y - 0.1f, basePoint.z), new Quaternion());
            lastRockpileUpdateTime = Time.time;
        }
           

        PosX = basePoint.x + posrand;
        PosY = basePoint.y + posrand;
        PosZ = basePoint.z + posrand;
        Strength = surfaceStrength;
    }

    void popHaptics(Collision c, Vector3 vectorProj)
    {
        Vector3 vectorMoved = GetServoPos() - origContactObjectPoint;
        float distanceMoved = vectorMoved.magnitude;
        //Debug.Log("Distance moved is" + distanceMoved);
        if (distanceMoved < 0.5)
        {
            SpeedX = contactNormal.x * distanceMoved * 15;
            SpeedY = contactNormal.y * distanceMoved * 15;
            SpeedZ = contactNormal.z * distanceMoved * 15;
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

    void resetHapticParams()
    {
        Debug.Log("Resetting values.");
        SpeedX = 0.0f;
        SpeedY = 0.0f;
        SpeedZ = 0.0f;
        Strength = 0.0f;
        PosX = 0.0f;
        PosY = 0.0f;
        PosZ = 0.0f;
        flag = 0;
    }

    public void OnCollisionExit(Collision c)
    {
        //Debug.Log("OnCollisionExit exiting, before filter and collision type is " + c.collider.tag + "and object name is " + c.collider.gameObject.name);

        //ignore irrevelent colliders
        if ((somethingInTouch && c.collider.gameObject != touchingObject) ||
            c.collider.tag != "Surface" && c.collider.tag != "Water" && c.collider.tag != "Sand" && c.collider.tag != "Pop") return;

        //Debug.Log("OnCollisionExit exiting, filter passed and collision type is " + c.collider.tag + "and object name is " + c.collider.gameObject.name);

        if(c.collider.gameObject == touchingObject)
        {
            somethingInTouch = false; //only allow one collision at once
            touchingObject = null;
        }

        if(c.collider.tag == "Surface")
        {
            isBuildingInTouch = false;
            buildingInTouch = null;
        }



        //Reset everything
        resetHapticParams();
        
    }

    #endregion


    #region ForceUpdate

	private void _feedback() {

        if (flag == 1) SetServoPos(new double[3] { PosX, PosY, -PosZ }, Strength);
        else if (flag == 2) SetServo(new double[3] { SpeedX, SpeedY, -SpeedZ });
        else if (flag == 3) SetServoPos(new double[3] { PosX, PosY, -PosZ }, Strength);
        else if (flag == 4) SetServo(new double[3] { SpeedX, SpeedY, -SpeedZ });
        else if (flag == 0) SetServo(new double[3] { 0.0f, 0.0f, 0.0f });

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