
using System.Collections;
using UnityEngine;

public class EnemyController : CellObject
{
    [Header("Combat")]
    [SerializeField, Range(1, 20)]
    private int maxHealth = 3;

[Header("Death Effects")]
[SerializeField] private ParticleSystem enemyDeathEffectPrefab;

    [SerializeField, Range(1, 20)]
    private int attackDamage = 3;

    [Header("Movement")]
    [SerializeField, Range(0.05f, 1f)]
    private float moveDuration = 0.15f;

    private BoardManager boardManager;
    private Transform player;
    private TurnManager turnManager;

    private Vector2Int currentCell;
    private int currentHealth;

    private bool initialized;
    private bool defeated;
    private bool isMoving;

    // Called by BoardManager after the enemy is instantiated.
    public bool Initialize(
        BoardManager board,
        Transform playerTransform,
        TurnManager turns,
        Vector2Int spawnCell)
    {
        boardManager = board;
        player = playerTransform;
        turnManager = turns;

        if (boardManager == null ||
            player == null ||
            turnManager == null)
        {
            Debug.LogError("Enemy initialization failed!");
            return false;
        }

        if (!CanOccupy(spawnCell))
        {
            Debug.LogWarning("Enemy spawn cell is occupied!");
            return false;
        }

        currentHealth = maxHealth;
        currentCell = spawnCell;
        defeated = false;
        isMoving = false;

        transform.position = CellToWorld(spawnCell);

        // Reserve the starting cell.
        BoardManager.CellData cell =
            boardManager.GetCellData(spawnCell);

        cell.ContainedObject = this;

        // Subscribe exactly once to game turns.
        turnManager.OnTick += TakeTurn;
        initialized = true;

        Debug.Log("Enemy spawned at: " + spawnCell);
        return true;
    }

    private void TakeTurn()
    {
        if (!initialized || defeated || isMoving ||
            GameManager.Instance == null ||
            !GameManager.Instance.CanPlayerAct)
        {
            return;
        }

        Vector2Int playerCell =
            WorldToCell(player.position);

        Vector2Int difference = playerCell - currentCell;

        int distance =
            Mathf.Abs(difference.x) +
            Mathf.Abs(difference.y);

        // Attack if immediately adjacent.
        if (distance == 1)
        {
            GameManager.Instance.TakeDamage(attackDamage);

            Debug.Log("Enemy attacked player: -" +
                      attackDamage);
            return;
        }

        if (distance <= 0)
            return;

        int dx = difference.x > 0 ? 1 :
                 difference.x < 0 ? -1 : 0;

        int dy = difference.y > 0 ? 1 :
                 difference.y < 0 ? -1 : 0;

        Vector2Int horizontal = new Vector2Int(dx, 0);
        Vector2Int vertical = new Vector2Int(0, dy);

        // Try the axis with the greater distance first.
        if (Mathf.Abs(difference.x) >= Mathf.Abs(difference.y))
        {
            if (!TryMove(horizontal))
                TryMove(vertical);
        }
        else
        {
            if (!TryMove(vertical))
                TryMove(horizontal);
        }
    }

    private bool CanOccupy(Vector2Int position)
    {
        if (boardManager == null || player == null)
            return false;

        // Never overlap the player.
        if (position == WorldToCell(player.position))
            return false;

        BoardManager.CellData cell =
            boardManager.GetCellData(position);

        return cell != null &&
               cell.Passable &&
               cell.ContainedObject == null;
    }

    private bool TryMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return false;

        Vector2Int targetCell = currentCell + direction;

        if (!CanOccupy(targetCell))
            return false;

        BoardManager.CellData oldCell =
            boardManager.GetCellData(currentCell);

        BoardManager.CellData newCell =
            boardManager.GetCellData(targetCell);

        // Release the old cell.
        if (oldCell != null &&
            oldCell.ContainedObject == this)
        {
            oldCell.ContainedObject = null;
        }

        // Reserve the destination immediately.
        newCell.ContainedObject = this;
        currentCell = targetCell;

        StartCoroutine(SmoothMove(targetCell));
        return true;
    }

    private IEnumerator SmoothMove(Vector2Int targetCell)
    {
        isMoving = true;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = CellToWorld(targetCell);

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / moveDuration
            );

            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            yield return null;
        }

        transform.position = endPosition;
        isMoving = false;

        Debug.Log("Enemy moved to: " + currentCell);
    }

    public override bool PlayerWantsToEnter()
    {
        // Player attacks instead of moving into enemy cell.
        TakeDamage(1);
        return false;
    }

    public void TakeDamage(int damage)
    {
        if (!initialized || defeated || damage <= 0)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - damage
        );

        Debug.Log("Enemy HP: " + currentHealth);

        if (currentHealth == 0)
            Die();
    }

    
private void Die()
{
    if (defeated)
        return;

    defeated = true;

    Debug.Log("Enemy defeated!");

    // Create death particles at the enemy's position.
   
if (ParticlePool.Instance != null &&
    enemyDeathEffectPrefab != null)
{
    ParticlePool.Instance.Play(
        enemyDeathEffectPrefab,
        transform.position
    );
}


    // Play enemy death sound.
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlayEnemyDeath();
    }

    // OnDisable handles cell cleanup and event unsubscribe.
    gameObject.SetActive(false);
    Destroy(gameObject);
}

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (turnManager != null)
        {
            turnManager.OnTick -= TakeTurn;
            turnManager = null;
        }

        if (initialized && boardManager != null)
        {
            boardManager.RemoveCellObject(
                currentCell,
                this
            );
        }

        initialized = false;
        StopAllCoroutines();
        isMoving = false;
    }

    private Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x),
            Mathf.FloorToInt(position.y)
        );
    }

    private Vector3 CellToWorld(Vector2Int position)
    {
        return new Vector3(
            position.x + 0.5f,
            position.y + 0.5f,
            0f
        );
    }
}
