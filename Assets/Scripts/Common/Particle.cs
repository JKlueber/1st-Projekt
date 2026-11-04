using UnityEngine;

/// <summary>
/// Plain data holder for one simulated particle.
/// The simulation owns position/velocity; the Transform is only used for rendering.
/// </summary>
public class Particle
{
    public Vector3 position;
    public Vector3 velocity;
    public float mass = 1f;

    public float age;
    public float lifetime = float.PositiveInfinity;

    /// <summary>Visual representation (e.g. a small sphere). Optional.</summary>
    public Transform view;

    public bool IsDead => age >= lifetime;

    public Particle(Vector3 position, Vector3 velocity)
    {
        this.position = position;
        this.velocity = velocity;
    }

    /// <summary>Copy simulation state to the visual object.</summary>
    public void SyncView()
    {
        if (view != null) view.position = position;
    }
}
