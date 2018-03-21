using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRTrackerEyeTagOffset : MonoBehaviour {

	/// <summary>
	/// VR Tracker : This scripts corrects the offset between the Tag position and the users eye
	/// This position offset depends on the users head rotation
	/// The offset value is the transform local position at start.
	/// </summary>

	public Camera userEyesCamera;
	public Vector3 eyeTagOffset;

	// Use this for initialization
	void Start () {
		if (userEyesCamera == null)
			userEyesCamera = GetComponentInChildren<Camera>();

		if (eyeTagOffset == null)
			eyeTagOffset = transform.localPosition;

	}

	// Update is called once per frame
	void LateUpdate () {
		transform.localPosition = userEyesCamera.transform.rotation * eyeTagOffset;

	}
}