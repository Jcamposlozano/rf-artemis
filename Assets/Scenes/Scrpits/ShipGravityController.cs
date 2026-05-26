using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipGravityController : MonoBehaviour
{
    public GravitySource[] gravitySources;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        foreach (GravitySource source in gravitySources)
        {
            if (source == null) continue;

            Vector3 direction = source.GetGravityDirection(rb.position);
            float magnitude = source.GetGravityForceMagnitude(rb.position);

            Vector3 force = direction * magnitude;
            force.z = 0f;

            rb.AddForce(force, ForceMode.Force);
        }
    }
}