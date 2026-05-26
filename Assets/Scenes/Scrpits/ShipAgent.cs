using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShipAgent : Agent
{
    private enum MissionPhase
    {
        EarthCoast,
        TransferToMoon
    }

    [Header("References")]
    public Transform earth;
    public Transform moon;
    public MoonOrbit moonOrbit;

    [Header("Ship Control")]
    public float thrustForce = 20f;
    public float reverseThrustForce = 10f;
    public float rotationSpeed = 120f;

    [Header("Initial Orbit Physics")]
    public float earthMu = 1000f;
    public bool clockwiseOrbit = false;

    [Header("Ship Randomization")]
    public bool randomizeShipPhase = true;
    public float initialOrbitRadius = 8f;
    public Vector3 fallbackInitialPosition = new Vector3(0f, 8f, 0f);

    [Header("Moon Randomization")]
    public bool randomizeMoonPhase = true;

    [Header("Earth Coast")]
    public float earthOrbitMinDistance = 7f;
    public float earthOrbitMaxDistance = 12f;

    [Header("Transfer Window")]
    public float minTransferDegrees = 360f;
    public float maxTransferDegrees = 720f;

    public float idealMoonAngleMin = 20f;
    public float idealMoonAngleMax = 100f;

    [Header("Moon Target")]
    public float moonReachDistance = 3f;
    public float moonNearDistance = 12f;
    public float moonApproachRewardScale = 0.04f;

    [Header("System Boundary")]
    public float systemBoundaryMultiplier = 1.5f;
    public float outOfSystemPenalty = -5f;

    [Header("Failure Conditions")]
    public float earthCrashDistance = 4f;
    public float maxSpeed = 45f;

    [Header("Rewards")]
    public float stepPenalty = -0.001f;
    public float coastReward = 0.001f;
    public float transferStartReward = 2f;
    public float waitWindowReward = 0.002f;
    public float missWindowPenalty = -2f;
    public float moonSuccessReward = 10f;
    public float crashPenalty = -5f;
    public float speedPenaltyScale = 0.001f;

    private Rigidbody rb;

    private MissionPhase phase;

    private float previousDistanceToMoon;
    private float accumulatedEarthAngle;

    private Vector3 previousEarthDirection;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Randomizar Luna
        if (randomizeMoonPhase)
        {
            RandomizeMoonPhase();
        }

        // Randomizar Ship
        if (randomizeShipPhase)
        {
            RandomizeShipAroundEarth();
        }
        else
        {
            transform.position = fallbackInitialPosition;
        }

        transform.rotation = Quaternion.identity;

        // Inyectar velocidad orbital correcta
        SetCircularEarthOrbitVelocity();

        phase = MissionPhase.EarthCoast;

        previousDistanceToMoon =
            Vector3.Distance(transform.position, moon.position);

        accumulatedEarthAngle = 0f;

        previousEarthDirection =
            transform.position - earth.position;

        previousEarthDirection.z = 0f;

        if (previousEarthDirection.sqrMagnitude > 0.0001f)
        {
            previousEarthDirection.Normalize();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 relEarth = transform.position - earth.position;
        Vector3 relMoon = moon.position - transform.position;

        Vector3 shipVel = rb.linearVelocity;
        Vector3 forward = transform.up;
        Vector3 moonVel = GetMoonVelocity();

        relEarth.z = 0f;
        relMoon.z = 0f;
        shipVel.z = 0f;
        forward.z = 0f;
        moonVel.z = 0f;

        float distanceToEarth = relEarth.magnitude;
        float distanceToMoon = relMoon.magnitude;

        float moonPhaseAngle = GetMoonPhaseAngle();

        // 16 observaciones
        sensor.AddObservation(relEarth.x);
        sensor.AddObservation(relEarth.y);

        sensor.AddObservation(relMoon.x);
        sensor.AddObservation(relMoon.y);

        sensor.AddObservation(shipVel.x);
        sensor.AddObservation(shipVel.y);

        sensor.AddObservation(forward.x);
        sensor.AddObservation(forward.y);

        sensor.AddObservation(distanceToEarth);
        sensor.AddObservation(distanceToMoon);

        sensor.AddObservation(moonVel.x);
        sensor.AddObservation(moonVel.y);

        sensor.AddObservation(
            phase == MissionPhase.EarthCoast ? 1f : 0f);

        sensor.AddObservation(
            phase == MissionPhase.TransferToMoon ? 1f : 0f);

        sensor.AddObservation(
            Mathf.Clamp01(
                accumulatedEarthAngle / maxTransferDegrees));

        sensor.AddObservation(
            moonPhaseAngle / 360f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int rotateAction = actions.DiscreteActions[0];
        int thrustAction = actions.DiscreteActions[1];

        // SOLO control después de la órbita terrestre
        if (phase == MissionPhase.TransferToMoon)
        {
            ApplyRotation(rotateAction);
            ApplyThrust(thrustAction);
        }

        float distanceToEarth =
            Vector3.Distance(transform.position, earth.position);

        float distanceToMoon =
            Vector3.Distance(transform.position, moon.position);

        float speed =
            rb.linearVelocity.magnitude;

        AddReward(stepPenalty);

        // Crash Tierra
        if (distanceToEarth <= earthCrashDistance)
        {
            AddReward(crashPenalty);
            EndEpisode();
            return;
        }

        // Escape sistema
        float earthMoonDistance =
            Vector3.Distance(earth.position, moon.position);

        float maxAllowedDistance =
            earthMoonDistance * systemBoundaryMultiplier;

        if (distanceToEarth > maxAllowedDistance)
        {
            AddReward(outOfSystemPenalty);
            EndEpisode();
            return;
        }

        // Penalizar velocidad absurda
        if (speed > maxSpeed)
        {
            AddReward(
                -speedPenaltyScale *
                (speed - maxSpeed));
        }

        // Éxito lunar
        if (distanceToMoon <= moonReachDistance)
        {
            AddReward(moonSuccessReward);
            EndEpisode();
            return;
        }

        switch (phase)
        {
            case MissionPhase.EarthCoast:
                HandleEarthCoast(distanceToEarth);
                break;

            case MissionPhase.TransferToMoon:
                HandleTransferToMoon(distanceToMoon);
                break;
        }

        previousDistanceToMoon = distanceToMoon;
    }

    private void HandleEarthCoast(float distanceToEarth)
    {
        bool insideOrbitBand =
            distanceToEarth >= earthOrbitMinDistance &&
            distanceToEarth <= earthOrbitMaxDistance;

        if (insideOrbitBand)
        {
            float angleDelta =
                UpdateEarthOrbitAngle();

            AddReward(coastReward);
            AddReward(angleDelta * 0.005f);
        }

        if (accumulatedEarthAngle >= minTransferDegrees)
        {
            float moonPhaseAngle =
                GetMoonPhaseAngle();

            bool goodWindow =
                moonPhaseAngle >= idealMoonAngleMin &&
                moonPhaseAngle <= idealMoonAngleMax;

            if (goodWindow)
            {
                AddReward(transferStartReward);

                Debug.Log(
                    $"TRANSFER WINDOW | moonAngle={moonPhaseAngle}");

                phase = MissionPhase.TransferToMoon;
                return;
            }

            AddReward(waitWindowReward);

            if (accumulatedEarthAngle >= maxTransferDegrees)
            {
                AddReward(missWindowPenalty);
                EndEpisode();
            }
        }
    }

    private void HandleTransferToMoon(float distanceToMoon)
    {
        float improvement =
            previousDistanceToMoon - distanceToMoon;

        if (improvement > 0f)
        {
            AddReward(
                improvement *
                moonApproachRewardScale);
        }
        else
        {
            AddReward(-0.002f);
        }

        if (distanceToMoon < moonNearDistance)
        {
            AddReward(0.05f);
        }
    }

    private void RandomizeMoonPhase()
    {
        if (moonOrbit == null)
            return;

        float randomAngle =
            Random.Range(0f, 360f);

        moonOrbit.SetOrbitAngle(randomAngle);
    }

    private void RandomizeShipAroundEarth()
    {
        if (earth == null)
            return;

        float randomAngle =
            Random.Range(0f, 360f);

        float radians =
            randomAngle * Mathf.Deg2Rad;

        Vector3 offset =
            new Vector3(
                Mathf.Cos(radians),
                Mathf.Sin(radians),
                0f
            ) * initialOrbitRadius;

        transform.position =
            earth.position + offset;

        Debug.Log(
            $"Ship randomized at {randomAngle}°");
    }

    private float GetMoonPhaseAngle()
    {
        Vector3 shipDir =
            transform.position - earth.position;

        Vector3 moonDir =
            moon.position - earth.position;

        shipDir.z = 0f;
        moonDir.z = 0f;

        if (shipDir.sqrMagnitude < 0.0001f ||
            moonDir.sqrMagnitude < 0.0001f)
            return 0f;

        shipDir.Normalize();
        moonDir.Normalize();

        float angle =
            Vector3.SignedAngle(
                shipDir,
                moonDir,
                Vector3.forward);

        if (clockwiseOrbit)
            angle = -angle;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    private void SetCircularEarthOrbitVelocity()
    {
        if (earth == null)
            return;

        Vector3 radial =
            transform.position - earth.position;

        radial.z = 0f;

        float radius =
            radial.magnitude;

        if (radius < 0.001f)
            return;

        Vector3 radialDir =
            radial.normalized;

        Vector3 tangentialDir =
            clockwiseOrbit
                ? new Vector3(
                    radialDir.y,
                    -radialDir.x,
                    0f)
                : new Vector3(
                    -radialDir.y,
                    radialDir.x,
                    0f);

        float circularSpeed =
            Mathf.Sqrt(
                earthMu / radius);

        rb.linearVelocity =
            tangentialDir *
            circularSpeed;

        rb.WakeUp();
    }

    private float UpdateEarthOrbitAngle()
    {
        Vector3 currentDirection =
            transform.position - earth.position;

        currentDirection.z = 0f;

        if (currentDirection.sqrMagnitude < 0.0001f)
            return 0f;

        currentDirection.Normalize();

        float angleDelta =
            Vector3.SignedAngle(
                previousEarthDirection,
                currentDirection,
                Vector3.forward);

        float directionalDelta =
            clockwiseOrbit
                ? -angleDelta
                : angleDelta;

        float validDelta = 0f;

        if (directionalDelta > 0f &&
            directionalDelta < 10f)
        {
            validDelta = directionalDelta;
            accumulatedEarthAngle += validDelta;
        }

        previousEarthDirection =
            currentDirection;

        return validDelta;
    }

    private Vector3 GetMoonVelocity()
    {
        if (moonOrbit == null)
            return Vector3.zero;

        return moonOrbit.CurrentVelocity;
    }

    private void ApplyRotation(int action)
    {
        float dir = 0f;

        if (action == 1) dir = 1f;
        if (action == 2) dir = -1f;

        transform.Rotate(
            Vector3.forward,
            dir *
            rotationSpeed *
            Time.fixedDeltaTime);
    }

    private void ApplyThrust(int action)
    {
        Vector3 forward =
            transform.up;

        forward.z = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return;

        forward.Normalize();

        if (action == 1)
        {
            rb.AddForce(
                forward *
                thrustForce);
        }

        if (action == 2)
        {
            rb.AddForce(
                -forward *
                reverseThrustForce);
        }
    }

    public override void Heuristic(
        in ActionBuffers actionsOut)
    {
        var actions =
            actionsOut.DiscreteActions;

        actions[0] = 0;
        actions[1] = 0;

        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.aKey.isPressed)
            actions[0] = 1;

        if (keyboard.dKey.isPressed)
            actions[0] = 2;

        if (keyboard.wKey.isPressed)
            actions[1] = 1;

        if (keyboard.sKey.isPressed)
            actions[1] = 2;
    }
}