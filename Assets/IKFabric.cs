#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
[ExecuteInEditMode]
public class IKFabric : MonoBehaviour
{
    public int chainLength = 2;
    public Transform target;
    public Transform pole;
    public float delta = 0.001f;

    Transform[] bones;
    float[] bonesLength;
    float completeLength;
    Vector3[] positions;
    Vector3[] startDirection;
    Quaternion[] startRotation;
    Quaternion startRotationTarget;
    Transform root;

    void Awake()
    {
        Init();
    }
    void OnEnable()
    {
        Init();
    }
    void Init()
    {
        //initial array
        bones = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        bonesLength = new float[chainLength];
        startDirection = new Vector3[chainLength + 1];
        startRotation = new Quaternion[chainLength + 1];
        //find root
        root = transform;
        for (int i = 0; i <= chainLength; i++)
        {
            root = root.parent;
        }
        startRotationTarget = GetRotationRootSpace(target);

        //init data
        Transform current = transform;
        completeLength = 0;
        for (int i = bones.Length - 1; i >= 0; i--)
        {
            bones[i] = current;
            startRotation[i] = GetRotationRootSpace(current);
            if (i != bones.Length - 1)
            {
                //mid bone
                startDirection[i] = GetPositionRootSpace(bones[i + 1]) - GetPositionRootSpace(bones[i]);
                bonesLength[i] = startDirection[i].magnitude;
                completeLength += bonesLength[i];
            }
            current = current.parent;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        ResolveIK();
    }

    private void ResolveIK()
    {
        if (target == null)
            return;
        delta = Mathf.Max(0.001f, delta);
        //Fabric

        //  root
        //  (bone0) (bonelen 0) (bone1) (bonelen 1) (bone2)...
        //   x--------------------x--------------------x---...

        //get position
        for (int i = 0; i < bones.Length; i++)
            positions[i] = GetPositionRootSpace(bones[i]);

        Vector3 targetPosition = GetPositionRootSpace(target);
        Quaternion targetRotation = GetRotationRootSpace(target);

        //1st is possible to reach?
        if ((targetPosition - GetPositionRootSpace(bones[0])).sqrMagnitude >= completeLength * completeLength)
        {
            //just strech it
            Vector3 direction = (targetPosition - positions[0]).normalized;
            //set everything after root
            for (int i = 1; i < positions.Length; i++)
                positions[i] = positions[i - 1] + direction * bonesLength[i - 1];
        }
        else
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                //https://www.youtube.com/watch?v=UNoX65PRehA
                //back
                for (int i = positions.Length - 1; i > 0; i--)
                {
                    if (i == positions.Length - 1)
                        positions[i] = targetPosition; //set it to target
                    else
                        positions[i] = positions[i + 1] + (positions[i] - positions[i + 1]).normalized * bonesLength[i]; //set in line on distance
                }

                //forward
                for (int i = 1; i < positions.Length; i++)
                    positions[i] = positions[i - 1] + (positions[i] - positions[i - 1]).normalized * bonesLength[i - 1];

                //close enough?
                if ((positions[positions.Length - 1] - targetPosition).sqrMagnitude < delta * delta)
                    break;
            }
        }

        //move towards pole
        if (pole != null)
        {
            Vector3 polePosition = GetPositionRootSpace(pole);
            for (int i = 1; i < positions.Length - 1; i++)
            {
                Plane plane = new Plane(positions[i + 1] - positions[i - 1], positions[i - 1]);
                Vector3 projectedPole = plane.ClosestPointOnPlane(polePosition);
                Vector3 projectedBone = plane.ClosestPointOnPlane(positions[i]);
                float angle = Vector3.SignedAngle(projectedBone - positions[i - 1], projectedPole - positions[i - 1], plane.normal);
                positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
            }
        }

        //set position & rotation
        for (int i = 0; i < positions.Length; i++)
        {
            if (i == positions.Length - 1)
                SetRotationRootSpace(bones[i], Quaternion.Inverse(targetRotation) * startRotationTarget * Quaternion.Inverse(startRotation[i]));
            else
                SetRotationRootSpace(bones[i], Quaternion.FromToRotation(startDirection[i], positions[i + 1] - positions[i]) * Quaternion.Inverse(startRotation[i]));
            SetPositionRootSpace(bones[i], positions[i]);
        }
    }

    Vector3 GetPositionRootSpace(Transform current)
    {
        return Quaternion.Inverse(root.rotation) * (current.position - root.position);
    }

    void SetPositionRootSpace(Transform current, Vector3 position)
    {
        current.position = root.rotation * position + root.position;
    }

    Quaternion GetRotationRootSpace(Transform current)
    {
        //inverse(after) * before => rot: before -> after
        return Quaternion.Inverse(current.rotation) * root.rotation;
    }

    void SetRotationRootSpace(Transform current, Quaternion rotation)
    {
        current.rotation = root.rotation * rotation;
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Transform current = transform;
        for (int i = 0; i < chainLength && current != null && current.parent != null; i++)
        {
            float scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
            Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
            current = current.parent;
        }
    }
#endif

}
