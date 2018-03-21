using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtTarget : MonoBehaviour {

    void LateUpdate()
    {
        Vector3 target = Camera.main.transform.position;
        Vector3 targetPosition = new Vector3(target.x,
                                        this.transform.position.y,
                                        target.z);
        this.transform.LookAt(targetPosition);
    }
}
