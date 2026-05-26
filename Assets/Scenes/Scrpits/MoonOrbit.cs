using UnityEngine;

public class MoonOrbit : MonoBehaviour
{
    [Header("References")]
    public Transform earth;

    [Header("Orbit Settings")]
    public float orbitRadius = 30f;
    public float orbitSpeed = 3f; // grados por segundo

    private float currentAngle = 0f;
    private Vector3 lastPosition;

    // Velocidad orbital actual de la Luna.
    // ShipAgent usa esto como observación.
    public Vector3 CurrentVelocity { get; private set; }

    void Start()
    {
        if (earth == null)
        {
            Debug.LogError("MoonOrbit: Earth reference missing.");
            return;
        }

        UpdateOrbitPosition();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (earth == null)
            return;

        currentAngle += orbitSpeed * Time.deltaTime;

        if (currentAngle >= 360f)
            currentAngle -= 360f;

        UpdateOrbitPosition();
    }

    // Permite que ShipAgent randomice la posición inicial
    // de la Luna en cada episodio.
    public void SetOrbitAngle(float angleDegrees)
    {
        currentAngle = angleDegrees;

        while (currentAngle >= 360f)
            currentAngle -= 360f;

        while (currentAngle < 0f)
            currentAngle += 360f;

        UpdateOrbitPosition();

        // Reseteamos referencia para evitar picos falsos
        // de velocidad en el primer frame.
        lastPosition = transform.position;
        CurrentVelocity = Vector3.zero;
    }

    private void UpdateOrbitPosition()
    {
        if (earth == null)
            return;

        float radians = currentAngle * Mathf.Deg2Rad;

        Vector3 newPosition =
            earth.position +
            new Vector3(
                Mathf.Cos(radians),
                Mathf.Sin(radians),
                0f
            ) * orbitRadius;

        float dt = Time.deltaTime;

        if (dt > 0f)
        {
            CurrentVelocity = (newPosition - lastPosition) / dt;
        }

        transform.position = newPosition;
        lastPosition = newPosition;
    }

    // Opcional: útil para debugging
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
}