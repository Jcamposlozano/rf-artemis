using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    public float thrustForce = 20f;
    public float reverseThrustForce = 10f;
    public float rotationSpeed = 120f;

    private Rigidbody rb;
    private float rotationInput;
    private int thrustInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        rotationInput = 0f;
        thrustInput = 0;

        if (keyboard.aKey.isPressed)
            rotationInput = 1f;

        if (keyboard.dKey.isPressed)
            rotationInput = -1f;

        if (keyboard.wKey.isPressed)
            thrustInput = 1;
        else if (keyboard.sKey.isPressed)
            thrustInput = -1;
    }

    private void FixedUpdate()
    {
        HandleRotation();
        HandleThrust();
    }

    private void HandleRotation()
    {
        float deltaRotation = rotationInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion rotationStep = Quaternion.Euler(0f, 0f, deltaRotation);
        rb.MoveRotation(rb.rotation * rotationStep);
    }

    private void HandleThrust()
    {
        if (thrustInput == 0) return;

        Vector3 forward = transform.up;
        forward.z = 0f;
        forward = forward.normalized;

        if (thrustInput == 1)
        {
            rb.AddForce(forward * thrustForce, ForceMode.Force);
        }
        else if (thrustInput == -1)
        {
            rb.AddForce(-forward * reverseThrustForce, ForceMode.Force);
        }
    }
}