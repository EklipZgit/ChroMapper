using UnityEngine;

public class ParticleSystemEffect : MonoBehaviour
{
    public ColorSchemeProvider ColorSchemeProvider;
    public bool LightOnStart;
    public ParticleSystem ParticleSystem;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.Particle[] particles;
    private Color particleColor;
    private bool initialized;
    private bool hasColor;

    public void Initialize()
    {
        if (initialized)
            return;

        mainModule = ParticleSystem.main;
        particles = new ParticleSystem.Particle[mainModule.maxParticles];
        initialized = true;
    }

    public void SetColor(Color color)
    {
        if (hasColor && color.Equals(particleColor))
            return;

        particleColor = color;
        hasColor = true;
        mainModule.startColor = color;
        var count = ParticleSystem.GetParticles(particles, particles.Length);
        for (var index = 0; index < count; index++)
            particles[index].startColor = color;

        ParticleSystem.SetParticles(particles, count);
    }
}
