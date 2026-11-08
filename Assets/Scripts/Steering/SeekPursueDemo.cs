using UnityEngine;

/// <summary>
/// Part A.2: Seek & Pursue steering behaviours in 2D (XZ plane, top-down view).
///
/// A target particle moves along a straight line (plain translation, no dynamics).
/// Two particles start from the same point: one SEEKS the target (aims at its current
/// position), the other PURSUES it (aims at its predicted future position).
/// Followers are integrated with simple Euler:
///     velocity += acceleration * dt   (clipped to maxSpeed)
///     position += velocity * dt
/// The animation stops after 'duration' seconds.
///
/// Usage: put this on an empty GameObject in a new scene (e.g. "SeekPursue") and press Play.
/// Requires Steering.cs and Particle.cs from Assets/Scripts/Common.
/// </summary>
public class SeekPursueDemo : MonoBehaviour
{
    [Header("Target (moves along a straight line)")]
    public Vector3 targetStart = new Vector3(-10f, 0f, 5f);
    public Vector3 targetVelocity = new Vector3(2.5f, 0f, 0f);

    [Header("Followers")]
    public Vector3 followersStart = new Vector3(-5f, 0f, -8f);
    public float maxAccel = 8f;
    public float maxSpeed = 5f;
    public float maxPrediction = 2f;

    [Header("Simulation")]
    public float duration = 12f;

    [Header("Rendering")]
    public float particleScale = 0.6f;
    public bool setupTopDownCamera = true;
    public float cameraHeight = 25f;
    public bool createGround = true;

    Particle target, seeker, pursuer;
    float elapsed;
    bool running = true;

    void Start()
    {
        target  = MakeParticle(targetStart, targetVelocity, Color.red, "Target");
        seeker  = MakeParticle(followersStart, Vector3.zero, Color.blue, "Seeker");
        pursuer = MakeParticle(followersStart, Vector3.zero, Color.green, "Pursuer");

        if (setupTopDownCamera) SetupCamera();
        if (createGround) CreateGround();
    }

    void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;
        elapsed += dt;
        if (elapsed >= duration) { running = false; return; }

        // Target: straight-line translation, no dynamics
        target.position += target.velocity * dt;

        // Seek: accelerate towards the target's CURRENT position
        Vector3 aSeek = Steering.Seek(seeker.position, target.position, maxAccel);
        Integrate(seeker, aSeek, dt);

        // Pursue: accelerate towards the target's PREDICTED position (target velocity known)
        Vector3 aPursue = Steering.Pursue(pursuer.position, pursuer.velocity,
                                          target.position, target.velocity,
                                          maxAccel, maxSpeed, maxPrediction);
        Integrate(pursuer, aPursue, dt);

        target.SyncView();
        seeker.SyncView();
        pursuer.SyncView();
    }

    void Integrate(Particle p, Vector3 accel, float dt)
    {
        p.velocity += accel * dt;
        if (p.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            p.velocity = p.velocity.normalized * maxSpeed;
        p.velocity.y = 0f;
        p.position += p.velocity * dt;
    }

    // ---------------- helpers ----------------

    Particle MakeParticle(Vector3 pos, Vector3 vel, Color color, string label)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = label;
        Destroy(go.GetComponent<Collider>()); // no Unity physics
        go.transform.SetParent(transform);
        go.transform.localScale = Vector3.one * particleScale;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = color;
        go.GetComponent<Renderer>().material = m;

        // Trail so the different paths are visible from above
        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.time = 20f;
        tr.widthMultiplier = 0.15f;
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Sprites/Default");
        Material tm = new Material(unlit);
        tm.color = color;
        tr.material = tm;
        tr.startColor = color;
        tr.endColor = color;

        Particle p = new Particle(pos, vel);
        p.view = go.transform;
        p.SyncView();
        return p;
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0f, cameraHeight, 0f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // look straight down
    }

    void CreateGround()
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "Ground";
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(transform);
        g.transform.position = new Vector3(0f, -0.5f, 0f);
        g.transform.localScale = new Vector3(4f, 1f, 4f);
    }
}
