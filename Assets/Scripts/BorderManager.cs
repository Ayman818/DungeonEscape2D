
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    public class CellData
    {
        public bool Passable = true;
        public CellObject ContainedObject;
    }

    [Header("Board Settings")]
    [SerializeField, Range(4, 30)]
    private int width = 8;

    [SerializeField, Range(4, 30)]
    private int height = 8;

    [SerializeField, Range(0, 50)]
    private int wallPercentage = 15;

    [Header("Tilemap")]
    [SerializeField]
    private Tilemap groundTilemap;

    [Header("Ground Tiles")]
    [SerializeField]
    private Tile[] groundTiles;

    [Header("Wall Tiles")]
    [SerializeField]
    private Tile[] wallTiles;

    [Header("Food Settings")]
    [SerializeField]
    private FoodObject[] foodPrefabs;

    [SerializeField, Range(0, 100)]
    private int foodSpawnChance = 30;

    [Header("Level Objects")]
    [SerializeField]
    private WallObject wallPrefab;

    [SerializeField]
    private ExitCellObject exitPrefab;
    
    [Header("Enemy Settings")]
    [SerializeField]
    private EnemyController enemyPrefab;

    [SerializeField, Range(0, 10)]
    private int maximumEnemies = 4;

    [SerializeField, Range(1, 5)]
    private int enemiesPerLevel = 1;


    private CellData[,] boardData;
    private Transform objectContainer;

    public void Init()
    {
        if (groundTilemap == null ||
            groundTiles == null || groundTiles.Length == 0 ||
            wallTiles == null || wallTiles.Length == 0)
        {
            Debug.LogError("Missing Tilemap or Tile assets!");
            return;
        }

        if (wallPrefab == null || exitPrefab == null)
        {
            Debug.LogError("Assign Wall Prefab and Exit Prefab!");
            return;
        }

        // Clear objects from the previous level.
        if (objectContainer != null)
        {
            objectContainer.gameObject.SetActive(false);
            Destroy(objectContainer.gameObject);
        }

        GameObject container =
            new GameObject("LevelObjects");

        objectContainer = container.transform;
        objectContainer.SetParent(transform);

        groundTilemap.ClearAllTiles();

        boardData = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = new CellData();

                bool isBorder =
                    x == 0 ||
                    y == 0 ||
                    x == width - 1 ||
                    y == height - 1;

                bool isPlayerStart =
                    x == 1 && y == 1;

                bool isExit =
                    x == width - 2 &&
                    y == height - 2;

                // Preserve a simple path from start to exit.
                bool isSafePath =
                    (x == 1 && y >= 1 && y <= height - 2) ||
                    (y == height - 2 &&
                     x >= 1 && x <= width - 2);

                cell.Passable = !isBorder;
                boardData[x, y] = cell;

                Tile selectedTile;

                if (isBorder)
                {
                    selectedTile = wallTiles[
                        Random.Range(0, wallTiles.Length)
                    ];
                }
                else
                {
                    selectedTile = groundTiles[
                        Random.Range(0, groundTiles.Length)
                    ];
                }

                Vector3Int tilePosition =
                    new Vector3Int(x, y, 0);

                groundTilemap.SetTile(
                    tilePosition,
                    selectedTile
                );

                if (isBorder || isPlayerStart)
                    continue;

                Vector2Int cellPosition =
                    new Vector2Int(x, y);

                if (isExit)
                {
                    SpawnExit(tilePosition, cell);
                    continue;
                }

                // Keep the reserved route free of obstacles.
                if (!isSafePath &&
                    Random.Range(0, 100) < wallPercentage)
                {
                    SpawnWall(tilePosition, cellPosition, cell);
                    continue;
                }

                SpawnFood(tilePosition, cell);
            }
        }

        Debug.Log("Board generated: " + width + " x " + height);
    }

    private void SpawnWall(
        Vector3Int position,
        Vector2Int cellPosition,
        CellData cell)
    {
        Vector3 worldPosition =
            groundTilemap.GetCellCenterWorld(position);

        WallObject wall = Instantiate(
            wallPrefab,
            worldPosition,
            Quaternion.identity,
            objectContainer
        );

        wall.Setup(this, cellPosition);
        cell.ContainedObject = wall;
    }

    private void SpawnFood(
        Vector3Int position,
        CellData cell)
    {
        if (foodPrefabs == null ||
            foodPrefabs.Length == 0)
            return;

        if (Random.Range(0, 100) >= foodSpawnChance)
            return;

        FoodObject prefab = foodPrefabs[
            Random.Range(0, foodPrefabs.Length)
        ];

        if (prefab == null)
            return;

        Vector3 worldPosition =
            groundTilemap.GetCellCenterWorld(position);

        FoodObject food = Instantiate(
            prefab,
            worldPosition,
            Quaternion.identity,
            objectContainer
        );

        cell.ContainedObject = food;
    }

    private void SpawnExit(
        Vector3Int position,
        CellData cell)
    {
        Vector3 worldPosition =
            groundTilemap.GetCellCenterWorld(position);

        ExitCellObject exit = Instantiate(
            exitPrefab,
            worldPosition,
            Quaternion.identity,
            objectContainer
        );

        cell.ContainedObject = exit;
    }

    public void RemoveCellObject(
        Vector2Int position,
        CellObject cellObject)
    {
        CellData cell = GetCellData(position);

        if (cell != null &&
            cell.ContainedObject == cellObject)
        {
            cell.ContainedObject = null;
        }
    }

    public CellData GetCellData(Vector2Int position)
    {
        if (boardData == null ||
            position.x < 0 ||
            position.y < 0 ||
            position.x >= width ||
            position.y >= height)
        {
            return null;
        }

        return boardData[position.x, position.y];
    }
    
public void SpawnEnemies(
    int level,
    Transform player,
    TurnManager turns)
{
    if (enemyPrefab == null)
    {
        Debug.LogWarning("Enemy Prefab is not assigned!");
        return;
    }

    if (boardData == null || objectContainer == null ||
        player == null || turns == null)
    {
        Debug.LogError("Cannot spawn enemies yet!");
        return;
    }

    int desiredCount = Mathf.Min(
        Mathf.Max(0, level) * enemiesPerLevel,
        maximumEnemies
    );

    Vector2Int playerCell = new Vector2Int(
        Mathf.FloorToInt(player.position.x),
        Mathf.FloorToInt(player.position.y)
    );

    // Collect available cells, excluding the exit.
    System.Collections.Generic.List<Vector2Int> candidates =
        new System.Collections.Generic.List<Vector2Int>();

    for (int y = 1; y < height - 1; y++)
    {
        for (int x = 1; x < width - 1; x++)
        {
            Vector2Int position = new Vector2Int(x, y);

            bool isExit =
                x == width - 2 && y == height - 2;

            int distanceFromPlayer =
                Mathf.Abs(position.x - playerCell.x) +
                Mathf.Abs(position.y - playerCell.y);

            CellData cell = GetCellData(position);

            if (!isExit &&
                distanceFromPlayer >= 3 &&
                cell != null &&
                cell.Passable &&
                cell.ContainedObject == null)
            {
                candidates.Add(position);
            }
        }
    }

    // Shuffle available positions.
    for (int i = candidates.Count - 1; i > 0; i--)
    {
        int j = Random.Range(0, i + 1);

        Vector2Int temp = candidates[i];
        candidates[i] = candidates[j];
        candidates[j] = temp;
    }

    int spawnCount = Mathf.Min(
        desiredCount,
        candidates.Count
    );

    for (int i = 0; i < spawnCount; i++)
    {
        Vector2Int position = candidates[i];

        Vector3 worldPosition =
            groundTilemap.GetCellCenterWorld(
                new Vector3Int(position.x, position.y, 0)
            );

        EnemyController enemy = Instantiate(
            enemyPrefab,
            worldPosition,
            Quaternion.identity,
            objectContainer
        );

        bool success = enemy.Initialize(
            this,
            player,
            turns,
            position
        );

        if (!success)
        {
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
    }

    Debug.Log("Enemies generated: " + spawnCount);
}

}
