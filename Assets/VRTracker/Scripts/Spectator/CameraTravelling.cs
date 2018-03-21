using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTravelling : MonoBehaviour
{

    public float duration = 5f;
    public Transform from;
    public Transform to;
    public bool returnToPosition = true;
    private float t;

    private Vector3 position1;
    private Vector3 position2;

    // Use this for initialization
    void Start()
    {
        position1 = from.position;
        position2 = to.position;
    }

    // Update is called once per frame
    void Update()
    {
        t += Time.deltaTime / duration;
        transform.position = Vector3.Lerp(position1, position2, t);
        if (t > 1.0f)
        {
            t = 0;
            if (returnToPosition)
            {
                Vector3 temp = position1;
                position1 = position2;
                position2 = temp;
            }
        }
    }
}
