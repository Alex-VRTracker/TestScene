using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRTrackerBoundariesProximity : MonoBehaviour {

	public Transform player;
    public Transform controller;
	public Renderer render;


	// Use this for initialization
	void Start () {

		render = gameObject.GetComponent<Renderer> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (!player.Equals(null)) {
			render.sharedMaterial.SetVector ("_PlayerPosition", player.position);
            render.sharedMaterial.SetVector ("_ControllerPosition", controller.position);
        }
    }
}
