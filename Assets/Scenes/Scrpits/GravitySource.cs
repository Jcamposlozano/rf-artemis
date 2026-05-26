using UnityEngine;

public class GravitySource : MonoBehaviour
{
    public float gravityStrength = 100f;

    public Vector3 GetGravityDirection(Vector3 targetPosition)
    {
        Vector3 dir = transform.position - targetPosition;
        dir.z = 0f;
        return dir.normalized;
    }

    public float GetGravityForceMagnitude(Vector3 targetPosition)
    {
        Vector3 offset = transform.position - targetPosition;
        offset.z = 0f;

        float sqrDistance = offset.sqrMagnitude;

        if (sqrDistance < 0.01f)
            return 0f;

        return gravityStrength / sqrDistance;
    }
}