using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;

    private float destroyTime;

    void Start()
    {
        destroyTime = Time.time + lifetime;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnDestroy()
    {
        // Play effect if destroyed before lifetime
        if (Time.time < destroyTime)
        {
            PlayChildParticleEffect();
        }
    }

    void PlayChildParticleEffect()
    {
        ParticleSystem childParticles = GetComponentInChildren<ParticleSystem>();
        if (childParticles != null)
        {
            // Detach from parent so it persists after projectile is destroyed
            childParticles.transform.SetParent(null);
            childParticles.transform.position = transform.position;

            // Play the particle system
            childParticles.Play();

            // Calculate total duration and destroy after it finishes
            var main = childParticles.main;
            float totalDuration = main.duration + main.startLifetime.constantMax;

            // Destroy the particle system after it finishes playing
            Destroy(childParticles.gameObject, totalDuration);
        }
    }
}
