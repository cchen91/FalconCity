using UnityEngine;
using System.Collections;

public class BuildingScript : MonoBehaviour {

    Rigidbody buildingRigidbody; 

	// Use this for initialization
	void Start () {


        buildingRigidbody = gameObject.AddComponent<Rigidbody>(); //add rigidbody to any building
        //buildingRigidbody.isKinematic = true; //initially not really controlled
        buildingRigidbody.isKinematic = true;
        buildingRigidbody.useGravity = false; //in the beginning, no gravity
	}

    public void getsGrabbed()
    {
        Debug.Log("This building is grabbed!");
        gameObject.GetComponent<MeshRenderer>().material.SetColor(0, Color.black); //building burns..


    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
