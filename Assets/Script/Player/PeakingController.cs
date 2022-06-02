using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeakingController : MonoBehaviour
{
    public Transform peakLeft;
    public Transform peakRight;
    public Transform Idle;

    [SerializeField]
    private KeyCode leftPeak = KeyCode.Q;
    [SerializeField]
    private KeyCode rightPeak = KeyCode.E;

    [SerializeField]
    private float lerpTime;

    void Update()
    {
        if(Input.GetKey(leftPeak))
        {
            transform.position = Vector3.Lerp(transform.position, peakLeft.position, lerpTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, peakLeft.rotation, lerpTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, Idle.position, lerpTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, Idle.rotation, lerpTime);
        }

        if (Input.GetKey(rightPeak))
        {
            transform.position = Vector3.Lerp(transform.position, peakRight.position, lerpTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, peakRight.rotation, lerpTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, Idle.position, lerpTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, Idle.rotation, lerpTime);
        }
    }
}
