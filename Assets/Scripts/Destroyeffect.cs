using UnityEngine;

/// <summary>
/// Efecto de partículas al destruir un caramelo.
/// Adjunta un ParticleSystem a este prefab.
/// El script auto-destruye el objeto cuando las partículas terminan.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class DestroyEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Start()
    {
        ps.Play();
        Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}