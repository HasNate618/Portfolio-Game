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
            childParticles.transform.SetParent(null);
            //childParticles.transform.position = transform.position;
            childParticles.Play();
/*            var main = childParticles.main;
            if (!main.loop)
            {
                Destroy(childParticles.gameObject, main.duration + main.startLifetime.constantMax);
            }*/
        }
    }
}
