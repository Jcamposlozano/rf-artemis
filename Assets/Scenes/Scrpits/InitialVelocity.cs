using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class InitialVelocity : MonoBehaviour
{
    public Vector3 initialVelocity = new Vector3(8f, 0f, 0f);

    private Rigidbody rb;
    private bool applied = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (!applied)
        {
            rb.linearVelocity = initialVelocity;
            applied = true;
        }
    }
}