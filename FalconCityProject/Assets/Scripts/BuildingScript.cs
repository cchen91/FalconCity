using UnityEngine;
using System.Collections;

public class BuildingScript : MonoBehaviour {

    Rigidbody buildingRigidbody;
    public bool destroyed;
    bool grabbed;
    public float stiffness = 35.0f;
    BoxCollider myBox;
    public GameObject smoke;

    DeviceNovintFalcon parentHapticsScript;

	// Use this for initialization
	void Start () {

        myBox = gameObject.GetComponent<BoxCollider>();
        buildingRigidbody = gameObject.AddComponent<Rigidbody>(); //add rigidbody to any building
        buildingRigidbody.mass = 0.5f;
        //buildingRigidbody.isKinematic = true; //initially not really controlled
        destroyed = false;
        grabbed = false;
        buildingRigidbody.isKinematic = true;
        buildingRigidbody.useGravity = true; //in the beginning, no gravity because kinematic?
	}

    public void getsGrabbed()
    {
        destroyed = true;
        grabbed = true;
        parentHapticsScript = gameObject.transform.parent.GetComponent<DeviceNovintFalcon>();
        //GameObject.Instantiate(smoke, gameObject.transform.position, new Quaternion());
        buildingRigidbody.isKinematic = true;
        myBox.enabled = false;
        
        stiffness = 10.0f;
        Debug.Log("This building is grabbed!");
        //gameObject.GetComponent<MeshRenderer>().material.SetColor(0, Color.black); //building burns..

    }

    public void letGo()
    {
        myBox.enabled = true;
        grabbed = false;
        buildingRigidbody.isKinematic = false;
    }
	
    void OnCollisionEnter(Collision c)
    {
        if (grabbed) parentHapticsScript.OnCollisionEnter(c);
        Debug.Log("Just called paren'ts oncollisionenter.");
    }

    void OnCollisionStay(Collision c)
    {
        if (grabbed) parentHapticsScript.OnCollisionStay(c);
    }

    void OnCollisionExit(Collision c)
    {
        if (grabbed) parentHapticsScript.OnCollisionExit(c);
    }

	// Update is called once per frame
	void Update () {
	
	}
}
