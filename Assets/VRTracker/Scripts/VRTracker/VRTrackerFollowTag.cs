using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class VRTrackerFollowTag : MonoBehaviour
{

    /* VR Tracker
	 * Attach the script to any object that you to follow a Tag position and/or orientation
	 * and choose the Tag type to follow.
	 */

    [Tooltip("The type of Tag chosen in VRTrackerTag script to follow")]
    public VRTracker.TagType tagTypeToFollow;
    public bool followOrientationX = true;
    public bool followOrientationY = true;
    public bool followOrientationZ = true;
    public bool followPositionX = true;
    public bool followPositionY = true;
    public bool followPositionZ = true;

    private Vector3 originalPosition;
    private Vector3 originalRotation;

    private VRTrackerTag tagToFollow;

    private NetworkIdentity NetIdent;

    // Use this for initialization
    void Start()
    {

        NetIdent = GetComponentsInParent<NetworkIdentity>()[0];
        if (NetIdent != null && !NetIdent.isLocalPlayer)
            return;

        originalPosition = transform.position;
        originalRotation = transform.rotation.eulerAngles;

        if (VRTracker.instance != null)
            tagToFollow = VRTracker.instance.GetTag(tagTypeToFollow);
        else
            Debug.LogError("No VR Tracker script found in current Scene. Import VRTrackeV2 prefab");
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (NetIdent != null && !NetIdent.isLocalPlayer)
            return;

        if (tagToFollow == null && VRTracker.instance != null)
            tagToFollow = VRTracker.instance.GetTag(tagTypeToFollow);
        else if (tagToFollow != null)
        {
            if (followPositionX || followPositionY || followPositionZ)
            {
                transform.position = new Vector3(followPositionX ? tagToFollow.transform.position.x : originalPosition.x, followPositionY ? tagToFollow.transform.position.y : originalPosition.y, followPositionZ ? tagToFollow.transform.position.z : originalPosition.z);
            }

            if (followOrientationX || followOrientationY || followOrientationZ)
            {
                Vector3 newRotation = tagToFollow.getOrientation();
                transform.rotation = Quaternion.Euler(followOrientationX ? newRotation.x : originalRotation.x, followOrientationY ? newRotation.y : originalRotation.y, followOrientationZ ? newRotation.z : originalRotation.z);
            }
        }
    }
}
