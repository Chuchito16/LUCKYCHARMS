using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public static Board instance;

    [Header("Board Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;

    [Header("Candy Prefabs")]
    [Tooltip("Arrastra aquí todos tus prefabs de dulces (uno por tipo). El sistema usará tantos tipos como prefabs haya.")]
    public GameObject[] candyPrefabs;

    [Header("Effects")]
    public GameObject destroyEffectPrefab;

    [Header("References")]
    public ScoreManager scoreManager;

    private Candy[,] candies;
    private bool isProcessing = false;

    void Awake() => instance = this;

    void Start()
    {
        if (candyPrefabs == null || candyPrefabs.Length == 0)
        {
            Debug.LogError("❌ No hay prefabs de dulces asignados en Board.");
            return;
        }

        candies = new Candy[width, height];
        InitializeBoard();
    }

    // ─────────────────────────────────────────────
    // Inicialización
    // ─────────────────────────────────────────────

    void InitializeBoard()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                SpawnCandy(x, y);

        // Garantizar que el tablero inicial no tenga matches
        int safetyLimit = 100;
        while (FindAllMatches().Count > 0 && safetyLimit-- > 0)
        {
            ClearBoard();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    SpawnCandy(x, y);
        }
    }

    void ClearBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] != null)
                {
                    Destroy(candies[x, y].gameObject);
                    candies[x, y] = null;
                }
            }
        }
    }

    void SpawnCandy(int x, int y)
    {
        int type = GetValidCandyType(x, y);
        Vector3 pos = GetWorldPosition(x, y);
        GameObject go = Instantiate(candyPrefabs[type], pos, Quaternion.identity, transform);
        go.name = $"Candy_{x}_{y}";
        Candy candy = go.GetComponent<Candy>();
        candy.Init(x, y, type);
        candies[x, y] = candy;
    }

    /// <summary>
    /// Elige un tipo aleatorio que no forme match de 3 con los ya existentes.
    /// Funciona con cualquier número de tipos (candyPrefabs.Length).
    /// </summary>
    int GetValidCandyType(int x, int y)
    {
        List<int> forbidden = new List<int>();

        // Evitar match horizontal
        if (x >= 2 && candies[x - 1, y] != null && candies[x - 2, y] != null)
            if (candies[x - 1, y].candyType == candies[x - 2, y].candyType)
                forbidden.Add(candies[x - 1, y].candyType);

        // Evitar match vertical
        if (y >= 2 && candies[x, y - 1] != null && candies[x, y - 2] != null)
            if (candies[x, y - 1].candyType == candies[x, y - 2].candyType)
                forbidden.Add(candies[x, y - 1].candyType);

        // Si todos los tipos están prohibidos (edge case extremo), usar cualquiera
        if (forbidden.Count >= candyPrefabs.Length)
            return Random.Range(0, candyPrefabs.Length);

        int chosen;
        int attempts = 0;
        do
        {
            chosen = Random.Range(0, candyPrefabs.Length);
            attempts++;
        } while (forbidden.Contains(chosen) && attempts < 50);

        return chosen;
    }

    // ─────────────────────────────────────────────
    // Utilidades públicas
    // ─────────────────────────────────────────────

    public Vector3 GetWorldPosition(int x, int y)
    {
        float startX = -(width * cellSize) / 2f + cellSize / 2f;
        float startY = -(height * cellSize) / 2f + cellSize / 2f;
        return new Vector3(startX + x * cellSize, startY + y * cellSize, 0f);
    }

    public Candy GetCandy(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return candies[x, y];
    }

    public bool IsProcessing() => isProcessing;

    // ─────────────────────────────────────────────
    // Swap
    // ─────────────────────────────────────────────

    public void TrySwap(Candy a, Candy b)
    {
        if (isProcessing) return;
        StartCoroutine(SwapAndCheck(a, b));
    }

    IEnumerator SwapAndCheck(Candy a, Candy b)
    {
        isProcessing = true;

        SwapData(a, b);
        yield return StartCoroutine(AnimateMove(a, b));

        List<Candy> matches = FindAllMatches();
        if (matches.Count > 0)
        {
            scoreManager?.UseMove();
            yield return StartCoroutine(ProcessMatches(matches));
        }
        else
        {
            // Revertir
            SwapData(a, b);
            yield return StartCoroutine(AnimateMove(a, b));
        }

        isProcessing = false;
    }

    void SwapData(Candy a, Candy b)
    {
        candies[a.xIndex, a.yIndex] = b;
        candies[b.xIndex, b.yIndex] = a;

        int ax = a.xIndex, ay = a.yIndex;
        a.xIndex = b.xIndex; a.yIndex = b.yIndex;
        b.xIndex = ax; b.yIndex = ay;
    }

    IEnumerator AnimateMove(Candy a, Candy b)
    {
        Vector3 targetA = GetWorldPosition(a.xIndex, a.yIndex);
        Vector3 targetB = GetWorldPosition(b.xIndex, b.yIndex);
        Vector3 startA = a.transform.position;
        Vector3 startB = b.transform.position;

        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = EaseInOut(elapsed / duration);
            a.transform.position = Vector3.Lerp(startA, targetA, t);
            b.transform.position = Vector3.Lerp(startB, targetB, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        a.transform.position = targetA;
        b.transform.position = targetB;
    }

    // ─────────────────────────────────────────────
    // Match Detection
    // ─────────────────────────────────────────────

    List<Candy> FindAllMatches()
    {
        HashSet<Candy> matchSet = new HashSet<Candy>();

        // Horizontal
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                Candy c0 = candies[x, y], c1 = candies[x + 1, y], c2 = candies[x + 2, y];
                if (c0 != null && c1 != null && c2 != null &&
                    c0.candyType == c1.candyType && c1.candyType == c2.candyType)
                {
                    matchSet.Add(c0); matchSet.Add(c1); matchSet.Add(c2);
                    // Extender
                    for (int ex = x + 3; ex < width; ex++)
                    {
                        if (candies[ex, y] != null && candies[ex, y].candyType == c0.candyType)
                            matchSet.Add(candies[ex, y]);
                        else break;
                    }
                }
            }
        }

        // Vertical
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                Candy c0 = candies[x, y], c1 = candies[x, y + 1], c2 = candies[x, y + 2];
                if (c0 != null && c1 != null && c2 != null &&
                    c0.candyType == c1.candyType && c1.candyType == c2.candyType)
                {
                    matchSet.Add(c0); matchSet.Add(c1); matchSet.Add(c2);
                    for (int ey = y + 3; ey < height; ey++)
                    {
                        if (candies[x, ey] != null && candies[x, ey].candyType == c0.candyType)
                            matchSet.Add(candies[x, ey]);
                        else break;
                    }
                }
            }
        }

        return new List<Candy>(matchSet);
    }

    // ─────────────────────────────────────────────
    // Match Processing (destruir → caer → rellenar)
    // ─────────────────────────────────────────────

    IEnumerator ProcessMatches(List<Candy> matches)
    {
        int points = matches.Count * 50;
        scoreManager?.AddScore(points);

        foreach (Candy c in matches)
        {
            if (destroyEffectPrefab != null)
                Instantiate(destroyEffectPrefab, c.transform.position, Quaternion.identity);
            candies[c.xIndex, c.yIndex] = null;
            Destroy(c.gameObject);
        }

        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(DropCandies());
        yield return StartCoroutine(FillBoard());
        yield return new WaitForSeconds(0.15f);

        // Cascada recursiva
        List<Candy> newMatches = FindAllMatches();
        if (newMatches.Count > 0)
            yield return StartCoroutine(ProcessMatches(newMatches));
    }

    IEnumerator DropCandies()
    {
        // Bajar cada dulce hasta el hueco más bajo disponible
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                if (candies[x, y] == null) continue;

                int dropTo = y;
                while (dropTo > 0 && candies[x, dropTo - 1] == null) dropTo--;

                if (dropTo == y) continue;

                candies[x, dropTo] = candies[x, y];
                candies[x, y] = null;
                candies[x, dropTo].xIndex = x;
                candies[x, dropTo].yIndex = dropTo;
            }
        }

        yield return StartCoroutine(AnimateToGrid());
    }

    IEnumerator FillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] != null) continue;

                int type = Random.Range(0, candyPrefabs.Length);
                // Spawnear desde arriba del tablero
                Vector3 spawnPos = GetWorldPosition(x, height + 1);
                GameObject go = Instantiate(candyPrefabs[type], spawnPos, Quaternion.identity, transform);
                Candy candy = go.GetComponent<Candy>();
                candy.Init(x, y, type);
                candies[x, y] = candy;
            }
        }

        yield return StartCoroutine(AnimateToGrid());
    }

    /// <summary>Anima todos los dulces a su posición correcta en el grid.</summary>
    IEnumerator AnimateToGrid()
    {
        var startPositions = new Dictionary<Candy, Vector3>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (candies[x, y] != null)
                    startPositions[candies[x, y]] = candies[x, y].transform.position;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = EaseOutCubic(elapsed / duration);
            foreach (var kvp in startPositions)
                kvp.Key.transform.position = Vector3.Lerp(kvp.Value, GetWorldPosition(kvp.Key.xIndex, kvp.Key.yIndex), t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var kvp in startPositions)
            kvp.Key.transform.position = GetWorldPosition(kvp.Key.xIndex, kvp.Key.yIndex);
    }

    // ─────────────────────────────────────────────
    // Easing helpers
    // ─────────────────────────────────────────────
    float EaseInOut(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}