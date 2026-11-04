using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Part A.1: particles emitted from a point with random initial velocity,
/// moving under gravity + wind, bouncing (restitution 0.7) on the faces of an
/// invisible axis-aligned cube. Own Euler integration, no Unity physics.
///
/// Attach to an empty GameObject. Optionally assign a small sphere prefab;
/// otherwise a primitive sphere is created for each particle.
/// </summary>
public class CubeParticleSystem : MonoBehaviour
{
    [Header("Emitter")]
    public Vector3 emitterPosition = new Vector3(0f, 5f, 0f);
    public float particlesPerSecond = 50f;
    public int maxParticles = 500;
    public float lifetime = 15f;
    public Vector2 speedRange = new Vector2(2f, 8f);

    [Header("Forces")]
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    [Tooltip("f_wind = k_w * v_w")]
    public float windCoefficient = 0.5f;
    public Vector3 windVelocity = new Vector3(2f, 0f, 0f);
    [Tooltip("Optional: wind changes over time (sinusoidal).")]
    public bool animateWind = false;
    public float windAmplitude = 3f;
    public float windFrequency = 0.3f;

    [Header("Cube (invisible)")]
    public Vector3 cubeCenter = new Vector3(0f, 5f, 0f);
    public float cubeSize = 10f;
    [Range(0f, 1f)] public float restitution = 0.7f;

    [Header("Rendering")]
    public GameObject particlePrefab;
    public float particleScale = 0.25f;

    readonly List<Particle> particles = new List<Particle>();
    float spawnAccumulator;
    Vector3 baseWind;

    void Start()
    {
        baseWind = windVelocity;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        UpdateWind();
        Emit(dt);

        Vector3 half = Vector3.one * (cubeSize * 0.5f);
        Vector3 min = cubeCenter - half;
        Vector3 max = cubeCenter + half;

        Vector3 windForce = windCoefficient * windVelocity; // f_wind = k_w * v_w

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            p.age += dt;

            if (p.IsDead)
            {
                if (p.view != null) Destroy(p.view.gameObject);
                particles.RemoveAt(i);
                continue;
            }

            // acceleration = gravity + force / mass
            Vector3 acceleration = gravity + windForce / p.mass;

            // Euler: velocity first, then position (with collision handling)
            p.velocity += acceleration * dt;
            CollisionUtils.StepInBox(ref p.position, ref p.velocity, dt, min, max, restitution);

            p.SyncView();
        }
    }

    void UpdateWind()
    {
        if (!animateWind) return;
        float s = Mathf.Sin(Time.time * windFrequency * 2f * Mathf.PI);
        windVelocity = baseWind + new Vector3(s * windAmplitude, 0f, Mathf.Cos(Time.time * windFrequency) * windAmplitude * 0.5f);
    }

    void Emit(float dt)
    {
        spawnAccumulator += particlesPerSecond * dt;
        while (spawnAccumulator >= 1f && particles.Count < maxParticles)
        {
            spawnAccumulator -= 1f;

            // random direction (uniform on sphere) and random magnitude
            Vector3 dir = Random.onUnitSphere;
            float speed = Random.Range(speedRange.x, speedRange.y);

            Particle p = new Particle(emitterPosition, dir * speed);
            p.lifetime = lifetime;
            p.view = CreateView();
            p.SyncView();
            particles.Add(p);
        }
        if (particles.Count >= maxParticles) spawnAccumulator = 0f;
    }

    Transform CreateView()
    {
        GameObject go;
        if (particlePrefab != null)
        {
            go = Instantiate(particlePrefab, transform);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>()); // no Unity physics
            go.transform.SetParent(transform);
        }
        go.transform.localScale = Vector3.one * particleScale;
        return go.transform;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(cubeCenter, Vector3.one * cubeSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(emitterPosition, 0.2f);
    }
}
