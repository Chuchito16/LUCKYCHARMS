using UnityEngine;

/// <summary>
/// Efecto visual que aparece bajo el caramelo seleccionado.
/// Agrégale un SpriteRenderer con un sprite de brillo/glow.
/// </summary>
public class SelectionEffect : MonoBehaviour
{
    public static SelectionEffect instance;

    private SpriteRenderer sr;

    void Awake()
    {
        instance = this;
        sr = GetComponent<SpriteRenderer>();
        Hide();
    }

    void Update()
    {
        // Pulso animado
        float scale = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
        transform.localScale = Vector3.one * scale;
    }

    public void ShowAt(Vector3 position)
    {
        transform.position = position + Vector3.back * 0.1f;
        gameObject.SetActive(true);
        if (sr) sr.enabled = true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}