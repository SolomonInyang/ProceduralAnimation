using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MovementProceduralAnimation : MonoBehaviour
{
    [Header("Main")]
    public float stepDistance = 1;
    public Vector3 lastPostion;
    public Vector3 currentPostion;
    public Vector3 deltaPostion;
    [Header("Body")]
    public Transform body;
    public AnimationCurve bodySetpCurve;
    public Vector3 bodyOffset;
    public Vector3 bodyOrigin;
    public float stepOffset = 1f;
    public float stepSpeed = 1f;
    public float stepPrecent = 0;

    [Header("Legs")]
    public Transform rightTarget;
    public Transform leftTarget;


    void Start()
    {
        bodyOrigin = body.position;
    }
    void Update()
    {
        currentPostion = transform.position;
        deltaPostion = currentPostion - lastPostion;
        if (deltaPostion.z != 0 || deltaPostion.x != 0)
        {
            BodyStep();
            TargetStep(leftTarget);
            TargetStep(rightTarget);
        }
        lastPostion = currentPostion;
    }
    void BodyStep()
    {
        stepPrecent += deltaPostion.magnitude / stepDistance;
        stepPrecent = GetPrecent(stepPrecent * stepSpeed);
        bodyOffset.y = bodySetpCurve.Evaluate(stepPrecent) * stepOffset;
        body.localPosition = bodyOrigin + bodyOffset;
    }
    void TargetStep(Transform target)
    {
        Vector3 direction = currentPostion - lastPostion;
        if (Physics.SphereCast(target.position, 1f, direction, out RaycastHit hit, direction.magnitude * 2f))
        {
            target.position = hit.point;
        }
        else
        {
        }
    }
    float GetPrecent(float value)
    {
        return value - (int)value;
    }
}
