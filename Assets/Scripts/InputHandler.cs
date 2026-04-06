using UnityEngine;

/// <summary>
/// Maneja TODO el input del juego mediante Raycast 2D.
/// Colócalo en un GameObject vacío en la escena (ej. "InputHandler").
/// NO necesita estar en los dulces.
/// </summary>
public class InputHandler : MonoBehaviour
{
    private Candy selectedCandy = null;
    private Vector3 dragStartWorld;
    private bool isDragging = false;

    void Update()
    {
        if (Board.instance == null || Board.instance.IsProcessing()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Candy hit = RaycastCandy();
            if (hit != null)
            {
                dragStartWorld = GetMouseWorld();
                isDragging = true;

                if (selectedCandy == null)
                {
                    Select(hit);
                }
                else if (selectedCandy == hit)
                {
                    Deselect();
                }
                else if (IsAdjacent(selectedCandy, hit))
                {
                    Candy a = selectedCandy;
                    Deselect();
                    Board.instance.TrySwap(a, hit);
                }
                else
                {
                    Deselect();
                    Select(hit);
                }
            }
        }

        // Drag
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 delta = GetMouseWorld() - dragStartWorld;

            if (delta.magnitude >= 0.35f && selectedCandy != null)
            {
                int dx = 0, dy = 0;
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                    dx = delta.x > 0 ? 1 : -1;
                else
                    dy = delta.y > 0 ? 1 : -1;

                Candy neighbor = Board.instance.GetCandy(
                    selectedCandy.xIndex + dx,
                    selectedCandy.yIndex + dy
                );

                if (neighbor != null)
                {
                    Candy a = selectedCandy;
                    Deselect();
                    Board.instance.TrySwap(a, neighbor);
                    isDragging = false;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
            isDragging = false;
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    Candy RaycastCandy()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
            return hit.collider.GetComponent<Candy>();
        return null;
    }

    void Select(Candy c)
    {
        selectedCandy = c;
        c.SetSelected(true);
        SelectionEffect.instance?.ShowAt(c.transform.position);
    }

    void Deselect()
    {
        if (selectedCandy != null)
        {
            selectedCandy.SetSelected(false);
            selectedCandy = null;
        }
        SelectionEffect.instance?.Hide();
    }

    bool IsAdjacent(Candy a, Candy b)
    {
        int dx = Mathf.Abs(a.xIndex - b.xIndex);
        int dy = Mathf.Abs(a.yIndex - b.yIndex);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    static Vector3 GetMouseWorld()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }
}