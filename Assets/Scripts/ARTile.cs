using UnityEngine;

public class ARTile : MonoBehaviour
{
    public int x;
    public int y;
    public int type;
    public ARMatch3Manager manager;

    private GameObject currentVisual;

    public void Init(int px, int py, int ptype, ARMatch3Manager pmanager)
    {
        x = px;
        y = py;
        type = ptype;
        manager = pmanager;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (manager == null) return;
        if (manager.tileVisualPrefabs == null) return;
        if (manager.tileVisualPrefabs.Length == 0) return;
        if (type < 0 || type >= manager.tileVisualPrefabs.Length) return;

        if (currentVisual != null)
        {
            Destroy(currentVisual);
        }

        GameObject visualPrefab = manager.tileVisualPrefabs[type];
        if (visualPrefab != null)
        {
            currentVisual = Instantiate(visualPrefab, transform);
            currentVisual.transform.localPosition = manager.tileVisualOffset;
            currentVisual.transform.localRotation = Quaternion.identity;
            currentVisual.transform.localScale = manager.tileVisualScale;
        }
    }

    public void HideVisual()
    {
        if (currentVisual != null)
        {
            currentVisual.SetActive(false);
        }
    }

    public void ShowVisual()
    {
        if (currentVisual != null)
        {
            currentVisual.SetActive(true);
        }
    }
}