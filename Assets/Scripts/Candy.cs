using UnityEngine;

/// <summary>
/// Datos y visual de cada dulce.
/// El input lo maneja InputHandler.cs — este script NO detecta clicks.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Candy : MonoBehaviour
{
    [System.NonSerialized] public int xIndex;
    [System.NonSerialized] public int yIndex;
    [System.NonSerialized] public int candyType;

    private SpriteRenderer sr;
    private Vector3 originalScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    public void Init(int x, int y, int type)
    {
        xIndex = x;
        yIndex = y;
        candyType = type;
    }

    /// <summary>Muestra u oculta el efecto visual de selección.</summary>
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            transform.localScale = originalScale * 1.18f;
            sr.sortingOrder = 2;
        }
        else
        {
            transform.localScale = originalScale;
            sr.sortingOrder = 0;
        }
    }
}