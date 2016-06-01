using UnityEngine;
using System.Collections;

public class BuildingScript : MonoBehaviour {

    Rigidbody buildingRigidbody;
    public GameObject smoke;

	// Use this for initialization
	void Start () {

        Debug.Log("This obejct contains box, " + gameObject.GetComponent<BoxCollider>().center);
        buildingRigidbody = gameObject.AddComponent<Rigidbody>(); //add rigidbody to any building
        //buildingRigidbody.isKinematic = true; //initially not really controlled
        buildingRigidbody.isKinematic = true;
        buildingRigidbody.useGravity = true; //in the beginning, no gravity because kinematic?
	}

    public void getsGrabbed()
    {
        GameObject.Instantiate(smoke, gameObject.transform.position, new Quaternion());
        buildingRigidbody.isKinematic = true;
        Debug.Log("This building is grabbed!");
        //gameObject.GetComponent<MeshRenderer>().material.SetColor(0, Color.black); //building burns..

    }

    public void letGo()
    {
        buildingRigidbody.isKinematic = false;
    }
	
	// Update is called once per frame
	void Update () {

        Debug.Log("This obejct contains box, " + gameObject.GetComponent<BoxCollider>().center);

	
	}
}
