using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARMatch3Manager : MonoBehaviour
{
    [Header("Board")]
    public int width = 4;
    public int height = 4;
    public float spacing = 0.04f;
    public GameObject tileBasePrefab;
    public Transform boardRoot;

    [Header("Tile Visuals")]
    public GameObject[] tileVisualPrefabs;
    public Vector3 tileVisualScale = new Vector3(0.03f, 0.03f, 0.03f);
    public Vector3 tileVisualOffset = new Vector3(0f, 0.01f, 0f);

    [Header("Game")]
    public int maxMoves = 30;
    public int targetScore = 100;
    public int scorePerTile = 10;

    [Header("UI")]
    public TMP_Text movesText;
    public TMP_Text scoreText;
    public TMP_Text endText;

    private ARTile[,] board;
    private ARTile firstSelected;
    private int movesLeft;
    private int score;
    private bool busy;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        StartGame();
    }

    public void StartGame()
    {
        StopAllCoroutines();

        if (boardRoot == null)
        {
            boardRoot = transform;
        }

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(boardRoot.GetChild(i).gameObject);
        }

        board = new ARTile[width, height];
        firstSelected = null;
        movesLeft = maxMoves;
        score = 0;
        busy = false;

        GenerateBoard();
        StartCoroutine(ClearStartingMatches());

        if (endText != null)
        {
            endText.text = "";
        }

        UpdateUI();
    }

    void Update()
    {
        if (busy) return;
        if (movesLeft <= 0) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TrySelect(Input.GetTouch(0).position);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TrySelect(Input.mousePosition);
        }
#endif
    }

    void TrySelect(Vector2 screenPos)
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            ARTile tile = hit.collider.GetComponent<ARTile>();
            if (tile != null)
            {
                OnTileSelected(tile);
            }
        }
    }

    void OnTileSelected(ARTile tile)
    {
        if (firstSelected == null)
        {
            firstSelected = tile;
            return;
        }

        if (tile == firstSelected)
        {
            firstSelected = null;
            return;
        }

        if (AreAdjacent(firstSelected, tile))
        {
            StartCoroutine(TrySwap(firstSelected, tile));
        }
        else
        {
            firstSelected = tile;
        }
    }

    bool AreAdjacent(ARTile a, ARTile b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dy == 1;
    }

    void GenerateBoard()
    {
        if (tileBasePrefab == null) return;
        if (tileVisualPrefabs == null || tileVisualPrefabs.Length == 0) return;

        float startX = -(width - 1) * spacing * 0.5f;
        float startZ = -(height - 1) * spacing * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject obj = Instantiate(tileBasePrefab, boardRoot);
                obj.transform.localPosition = new Vector3(startX + x * spacing, 0f, startZ + y * spacing);
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                ARTile tile = obj.GetComponent<ARTile>();
                if (tile == null) continue;

                int type = Random.Range(0, tileVisualPrefabs.Length);
                tile.Init(x, y, type, this);
                board[x, y] = tile;
            }
        }
    }

    IEnumerator ClearStartingMatches()
    {
        yield return null;

        while (true)
        {
            List<ARTile> matches = FindMatches();
            if (matches.Count == 0) break;

            yield return StartCoroutine(RemoveAndRefill(matches, false));
        }
    }

    IEnumerator TrySwap(ARTile a, ARTile b)
    {
        busy = true;
        firstSelected = null;

        SwapTypes(a, b);
        yield return new WaitForSeconds(0.15f);

        List<ARTile> matches = FindMatches();

        if (matches.Count > 0)
        {
            movesLeft--;
            UpdateUI();

            while (matches.Count > 0)
            {
                yield return StartCoroutine(RemoveAndRefill(matches, true));
                matches = FindMatches();
            }

            CheckEndGame();
        }
        else
        {
            SwapTypes(a, b);
            yield return new WaitForSeconds(0.15f);
        }

        busy = false;
    }

    void SwapTypes(ARTile a, ARTile b)
    {
        int temp = a.type;
        a.type = b.type;
        b.type = temp;

        a.UpdateVisual();
        b.UpdateVisual();
    }

    List<ARTile> FindMatches()
    {
        HashSet<ARTile> result = new HashSet<ARTile>();

        for (int y = 0; y < height; y++)
        {
            int count = 1;

            for (int x = 1; x < width; x++)
            {
                if (board[x, y] != null && board[x - 1, y] != null && board[x, y].type == board[x - 1, y].type)
                {
                    count++;
                }
                else
                {
                    if (count >= 3)
                    {
                        for (int k = 0; k < count; k++)
                        {
                            result.Add(board[x - 1 - k, y]);
                        }
                    }

                    count = 1;
                }
            }

            if (count >= 3)
            {
                for (int k = 0; k < count; k++)
                {
                    result.Add(board[width - 1 - k, y]);
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            int count = 1;

            for (int y = 1; y < height; y++)
            {
                if (board[x, y] != null && board[x, y - 1] != null && board[x, y].type == board[x, y - 1].type)
                {
                    count++;
                }
                else
                {
                    if (count >= 3)
                    {
                        for (int k = 0; k < count; k++)
                        {
                            result.Add(board[x, y - 1 - k]);
                        }
                    }

                    count = 1;
                }
            }

            if (count >= 3)
            {
                for (int k = 0; k < count; k++)
                {
                    result.Add(board[x, height - 1 - k]);
                }
            }
        }

        return new List<ARTile>(result);
    }

    IEnumerator RemoveAndRefill(List<ARTile> matches, bool addScore)
    {
        if (addScore)
        {
            score += matches.Count * scorePerTile;
            UpdateUI();
        }

        foreach (ARTile tile in matches)
        {
            tile.type = -1;
            tile.HideVisual();
        }

        yield return new WaitForSeconds(0.2f);

        for (int x = 0; x < width; x++)
        {
            List<int> remaining = new List<int>();

            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != null && board[x, y].type != -1)
                {
                    remaining.Add(board[x, y].type);
                }
            }

            while (remaining.Count < height)
            {
                remaining.Add(Random.Range(0, tileVisualPrefabs.Length));
            }

            for (int y = 0; y < height; y++)
            {
                board[x, y].type = remaining[y];
                board[x, y].UpdateVisual();
                board[x, y].ShowVisual();
            }
        }

        yield return new WaitForSeconds(0.1f);
    }

    void CheckEndGame()
    {
        if (score >= targetScore)
        {
            if (endText != null)
            {
                endText.text = "Ganaste";
            }

            movesLeft = 0;
        }
        else if (movesLeft <= 0)
        {
            if (endText != null)
            {
                endText.text = "Perdiste";
            }
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (movesText != null)
        {
            movesText.text = "Movimientos: " + movesLeft;
        }

        if (scoreText != null)
        {
            scoreText.text = "Puntaje: " + score + " / " + targetScore;
        }
    }
}