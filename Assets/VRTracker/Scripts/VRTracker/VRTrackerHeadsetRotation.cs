using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* VR Tracker
 * This script is to be set on a Gameobject between the Camera and the Object to which the Headset Tag position is applied
 * 
 */

public class VRTrackerHeadsetRotation : MonoBehaviour
{

    public Camera camera;
    public VRTrackerTag tag;

    private Quaternion previousOffset;
    private Quaternion destinationOffset;

    private Vector3 newRotation;

    private float t;
    private float timeToReachTarget = 5.0f;

    [Tooltip("The minimum offset in degrees to blink instead of rotating.")]
    public float minOffsetToBLink = 30.0f;

    /*[Tooltip("The VRTK Headset Fade script to use when fading the headset. If this is left blank then the script will need to be applied to the same GameObject.")]
    public VRTK.VRTK_HeadsetFade headsetFade;
    */
    void OnEnable()
    {
        newRotation = Vector3.zero;

        StartCoroutine(FixOffset());
        previousOffset = Quaternion.Euler(Vector3.zero);
        destinationOffset = Quaternion.Euler(Vector3.zero);

    }

    // Update is called once per frame
    void LateUpdate()
    {
        t += Time.deltaTime / timeToReachTarget;
        transform.localRotation = Quaternion.Lerp(previousOffset, destinationOffset, t);
    }

    IEnumerator FixOffset()
    {
        while (true)
        {
            if (VRTracker.instance != null)
            {
                if (tag == null)
                    tag = VRTracker.instance.GetHeadsetTag();
                if (tag != null)
                {
                    Vector3 tagRotation = UnmultiplyQuaternion(Quaternion.Euler(tag.getOrientation()));
                    Vector3 cameraRotation = UnmultiplyQuaternion(camera.transform.localRotation);
                    newRotation.y = tagRotation.y - cameraRotation.y;


                    float offsetY = Mathf.Abs(destinationOffset.eulerAngles.y - newRotation.y) % 360;
                    offsetY = offsetY > 180.0f ? offsetY - 360 : offsetY;

                    previousOffset = destinationOffset;

                    destinationOffset = Quaternion.Euler(newRotation);
                    if (Mathf.Abs(offsetY) > minOffsetToBLink)
                        t = timeToReachTarget;
                    else
                        t = 0;
                }
                yield return new WaitForSeconds(5);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private Vector3 UnmultiplyQuaternion(Quaternion quaternion)
    {
        Vector3 ret;

        var xx = quaternion.x * quaternion.x;
        var xy = quaternion.x * quaternion.y;
        var xz = quaternion.x * quaternion.z;
        var xw = quaternion.x * quaternion.w;

        var yy = quaternion.y * quaternion.y;
        var yz = quaternion.y * quaternion.z;
        var yw = quaternion.y * quaternion.w;

        var zz = quaternion.z * quaternion.z;
        var zw = quaternion.z * quaternion.w;

        var check = zw + xy;
        if (Mathf.Abs(check - 0.5f) <= 0.00001f)
            check = 0.5f;
        else if (Mathf.Abs(check + 0.5f) <= 0.00001f)
            check = -0.5f;

        ret.y = Mathf.Atan2(2 * (yw - xz), 1 - 2 * (yy + zz));
        ret.z = Mathf.Asin(2 * check);
        ret.x = Mathf.Atan2(2 * (xw - yz), 1 - 2 * (zz + xx));

        if (check == 0.5f)
        {
            ret.x = 0;
            ret.y = 2 * Mathf.Atan2(quaternion.y, quaternion.w);
        }
        else if (check == -0.5f)
        {
            ret.x = 0;
            ret.y = -2 * Mathf.Atan2(quaternion.y, quaternion.w);
        }

        ret = ret * 180 / Mathf.PI;
        return ret;
    }

}
