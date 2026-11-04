using UnityEngine;

/// <summary>
/// Collision detection + response helpers. No Unity physics involved.
///
/// Both functions assume LINEAR motion with constant velocity during the step,
/// compute the exact time of impact, reflect the normal velocity component
/// (scaled by the coefficient of restitution), and then continue the Euler step
/// from the impact point with the time that is left (dt - tHit).
/// </summary>
public static class CollisionUtils
{
    const float Eps = 1e-5f;
    const int MaxBounces = 8; // safety: several bounces inside one step (corners)

    // ------------------------------------------------------------------
    // Axis-aligned box (the "invisible cube"), particle is inside the box
    // ------------------------------------------------------------------

    /// <summary>
    /// Advance a particle by dt with position += velocity * dt, bouncing on the
    /// faces of the box [min, max]. Updates pos and vel in place.
    /// </summary>
    public static void StepInBox(ref Vector3 pos, ref Vector3 vel, float dt,
                                 Vector3 min, Vector3 max, float restitution)
    {
        float remaining = dt;

        for (int i = 0; i < MaxBounces && remaining > 0f; i++)
        {
            // Earliest face crossing within the remaining time, if any
            float tHit = remaining;
            int hitAxis = -1;
            float hitSign = 0f; // +1 = max face, -1 = min face

            for (int a = 0; a < 3; a++)
            {
                float v = vel[a];
                if (Mathf.Abs(v) < Eps) continue;

                float wall = v > 0f ? max[a] : min[a];
                float t = (wall - pos[a]) / v;      // time to reach that wall
                if (t < 0f) t = 0f;                  // already past it: bounce now

                if (t < tHit)
                {
                    tHit = t;
                    hitAxis = a;
                    hitSign = v > 0f ? 1f : -1f;
                }
            }

            // Move to the impact point (or to the end of the step if no impact)
            pos += vel * tHit;
            remaining -= tHit;

            if (hitAxis < 0) break; // no collision in this step

            // Response: flip + damp only the component perpendicular to the face
            vel[hitAxis] = -vel[hitAxis] * restitution;

            // Keep the particle exactly on/inside the wall (avoids numerical drift)
            pos[hitAxis] = hitSign > 0f ? max[hitAxis] - Eps : min[hitAxis] + Eps;
        }
    }

    // ------------------------------------------------------------------
    // Sphere (optional part 5), particle is inside the sphere
    // ------------------------------------------------------------------

    /// <summary>
    /// Advance a particle by dt, bouncing on the inside of a sphere.
    /// Solves |p + v t - c|^2 = R^2 (2nd degree in t) and keeps the root where
    /// the particle leaves the sphere (the larger one).
    /// </summary>
    public static void StepInSphere(ref Vector3 pos, ref Vector3 vel, float dt,
                                    Vector3 center, float radius, float restitution)
    {
        float remaining = dt;

        for (int i = 0; i < MaxBounces && remaining > 0f; i++)
        {
            Vector3 endPos = pos + vel * remaining;
            if ((endPos - center).sqrMagnitude <= radius * radius)
            {
                pos = endPos;      // stays inside, no collision
                return;
            }

            // a t^2 + b t + c = 0
            Vector3 d = pos - center;
            float a = Vector3.Dot(vel, vel);
            float b = 2f * Vector3.Dot(d, vel);
            float c = Vector3.Dot(d, d) - radius * radius;

            float disc = b * b - 4f * a * c;
            if (a < Eps || disc < 0f)
            {
                pos = endPos;      // degenerate, just move
                return;
            }

            float sq = Mathf.Sqrt(disc);
            float tHit = (-b + sq) / (2f * a);   // larger root = exit point
            tHit = Mathf.Clamp(tHit, 0f, remaining);

            pos += vel * tHit;
            remaining -= tHit;

            // Normal at impact (pointing towards the centre = inward)
            Vector3 n = (center - pos).normalized;
            float vn = Vector3.Dot(vel, n);
            if (vn < 0f) // moving outwards
            {
                // remove the normal component and add it back reversed and damped
                vel = vel - vn * n - restitution * vn * n;
            }
            pos = center - n * (radius - Eps);
        }
    }
}
