using UnityEngine;

/// <summary>
/// Componente de cada dulce en el tablero.
/// El SpriteRenderer ya viene configurado desde el Prefab con tu PNG.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Candy : MonoBehaviour
{
    [HideInInspector] public int xIndex;
    [HideInInspector] public int yIndex;
    [HideInInspector] public int candyType;

    private static Candy selectedCandy;

    private SpriteRenderer sr;
    private Vector3 originalScale;

    // Drag
    private Vector3 dragStartWorld;
    private bool isDragging;

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

    // ─────────────────────────────────────────────
    // Input: Click
    // ─────────────────────────────────────────────

    void OnMouseDown()
    {
        if (Board.instance == null || Board.instance.IsProcessing()) return;

        dragStartWorld = GetMouseWorld();
        isDragging = true;

        if (selectedCandy == null)
        {
            Select();
        }
        else if (selectedCandy == this)
        {
            Deselect();
        }
        else
        {
            if (IsAdjacent(selectedCandy))
            {
                Candy other = selectedCandy;
                other.Deselect();
                Board.instance.TrySwap(other, this);
            }
            else
            {
                selectedCandy.Deselect();
                Select();
            }
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging || Board.instance == null || Board.instance.IsProcessing()) return;

        Vector3 delta = GetMouseWorld() - dragStartWorld;
        if (delta.magnitude < 0.35f) return; // umbral mínimo de arrastre

        // Determinar dirección dominante
        int dx = 0, dy = 0;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            dx = delta.x > 0 ? 1 : -1;
        else
            dy = delta.y > 0 ? 1 : -1;

        Candy neighbor = Board.instance.GetCandy(xIndex + dx, yIndex + dy);
        if (neighbor != null)
        {
            if (selectedCandy != null) selectedCandy.Deselect();
            Board.instance.TrySwap(this, neighbor);
            isDragging = false;
        }
    }

    void OnMouseUp() => isDragging = false;

    // ─────────────────────────────────────────────
    // Selección visual
    // ─────────────────────────────────────────────

    void Select()
    {
        selectedCandy = this;
        transform.localScale = originalScale * 1.18f;
        sr.sortingOrder = 2;

        SelectionEffect.instance?.ShowAt(transform.position);
    }

    public void Deselect()
    {
        if (selectedCandy == this) selectedCandy = null;
        transform.localScale = originalScale;
        sr.sortingOrder = 0;

        SelectionEffect.instance?.Hide();
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    bool IsAdjacent(Candy other)
    {
        int dx = Mathf.Abs(xIndex - other.xIndex);
        int dy = Mathf.Abs(yIndex - other.yIndex);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    static Vector3 GetMouseWorld()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }
}