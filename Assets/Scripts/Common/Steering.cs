using UnityEngine;

/// <summary>
/// Steering behaviours (Game AI Movement 2). Every function returns an
/// ACCELERATION (not a force); the demo scripts integrate it with Euler:
///     velocity += acceleration * dt;   position += velocity * dt;
/// All behaviours work in 2D on the XZ plane (Y is forced to 0).
/// </summary>
public static class Steering
{
    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    static Vector3 Clip(Vector3 v, float max)
    {
        return v.sqrMagnitude > max * max ? v.normalized * max : v;
    }

    /// <summary>Accelerate at full power towards the target position.</summary>
    public static Vector3 Seek(Vector3 pos, Vector3 targetPos, float maxAccel)
    {
        Vector3 dir = Flat(targetPos - pos);
        if (dir.sqrMagnitude < 1e-8f) return Vector3.zero;
        return dir.normalized * maxAccel;
    }

    /// <summary>Same as Seek but away from the target.</summary>
    public static Vector3 Flee(Vector3 pos, Vector3 targetPos, float maxAccel)
    {
        return -Seek(pos, targetPos, maxAccel);
    }

    /// <summary>
    /// Seek where the target WILL be. The target velocity is assumed known.
    /// Prediction time = distance / own max speed (capped by maxPrediction).
    /// </summary>
    public static Vector3 Pursue(Vector3 pos, Vector3 vel,
                                 Vector3 targetPos, Vector3 targetVel,
                                 float maxAccel, float maxSpeed, float maxPrediction = 2f)
    {
        float distance = Flat(targetPos - pos).magnitude;
        float speed = vel.magnitude;

        float prediction = speed <= distance / maxPrediction
            ? maxPrediction
            : distance / speed;

        Vector3 predicted = targetPos + targetVel * prediction;
        return Seek(pos, predicted, maxAccel);
    }

    /// <summary>
    /// Like Seek but decelerates inside slowRadius so the particle stops at
    /// the target. Inside targetRadius it is considered "arrived".
    /// </summary>
    public static Vector3 Arrive(Vector3 pos, Vector3 vel, Vector3 targetPos,
                                 float maxAccel, float maxSpeed,
                                 float targetRadius, float slowRadius,
                                 float timeToTarget = 0.1f)
    {
        Vector3 dir = Flat(targetPos - pos);
        float distance = dir.magnitude;

        if (distance < targetRadius) return Flat(-vel) / timeToTarget; // brake

        float targetSpeed = distance > slowRadius
            ? maxSpeed
            : maxSpeed * distance / slowRadius;

        Vector3 desiredVel = dir.normalized * targetSpeed;
        Vector3 accel = (desiredVel - Flat(vel)) / timeToTarget;
        return Clip(accel, maxAccel);
    }
}
