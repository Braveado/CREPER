using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform player;
    Vector3 target, targetDirection, refVel, shakeOffset;
    public float maxCameraDistance;
    public float fullMovementTime;
    float zStart;

    float shakeMag;
    float shakeTimeEnd;
    Vector3 shakeVector;
    bool shaking;

    void Start()
    {
        // set default target, since we are 2D we lock z
        target = player.position;
        zStart = transform.position.z;
    }

    void Update()
    {
        targetDirection = CaptureTargetDirection();
        shakeOffset = UpdateShake(); //account for screen shake
        target = UpdateTargetPos();
        UpdateCameraPosition();
    }

    Vector3 CaptureTargetDirection()
    {
        // get direction
        Vector2 ret = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // set max radius boundaries for corners to not see more in diagonals
        float max = 0.9f;
        if (Mathf.Abs(ret.x) > max || Mathf.Abs(ret.y) > max)
            ret = ret.normalized;

        return ret;
    }

    Vector3 UpdateShake()
    {
        if (!shaking || Time.time > shakeTimeEnd)
        {
            shaking = false; //set shaking false when the shake time is up
            return Vector3.zero; //return zero so that it won't effect the target
        }
        Vector3 tempOffset = shakeVector;
        tempOffset *= shakeMag; //find out how far to shake, in what direction
        return tempOffset;
    }

    Vector3 UpdateTargetPos()
    {
        Vector3 targetOffset = targetDirection * maxCameraDistance;
        Vector3 ret = player.position + targetOffset;
        ret += shakeOffset; //add the screen shake vector to the target
        ret.z = zStart;
        return ret;
    }

    void UpdateCameraPosition()
    {
        Vector3 tempPos;
        tempPos = Vector3.SmoothDamp(transform.position, target, ref refVel, fullMovementTime);
        transform.position = tempPos;
    }

    public void Shake(Vector3 direction, float magnitude, float duration)
    { 
        //capture values set for where it's called
        shaking = true; //to know whether it's shaking
        shakeVector = direction; //direction to shake towards
        shakeMag = -magnitude; //how far in that direction
        shakeTimeEnd = Time.time + duration; //how long to shake
    }

}
