
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField, Range(0.05f, 1f)]
    private float moveDuration = 0.2f;
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem walkingDust;
    [SerializeField] private ParticleSystem attackEffect;

    private Vector2Int currentCell;
    private Coroutine moveCoroutine;
    private bool isMoving;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.CanPlayerAct ||
            isMoving)
        {
            return;
        }

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            direction = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            direction = Vector2Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            direction = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            direction = Vector2Int.right;
        }

        if (direction != Vector2Int.zero)
        {
            TryMove(direction);
        }
    }

    public void Spawn(Vector2Int startCell)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        isMoving = false;
        currentCell = startCell;

        transform.position = CellToWorld(currentCell);

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.ResetTrigger("Attack");
        }
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            cell.x + 0.5f,
            cell.y + 0.5f,
            0f
        );
    }

    private void TryMove(Vector2Int direction)
    {
        if (boardManager == null)
        {
            Debug.LogError(
                "PlayerController: BoardManager is not assigned!"
            );
            return;
        }

        Vector2Int targetCell = currentCell + direction;

        BoardManager.CellData cell =
            boardManager.GetCellData(targetCell);

        // Prevent movement outside the board or into
        // permanent obstacles.
        if (cell == null || !cell.Passable)
        {
            Debug.Log("Movement blocked!");
            return;
        }

        CellObject cellObject = cell.ContainedObject;

        // Check for walls, enemies, food, or exit.
        if (cellObject != null)
        {
            bool canEnter = cellObject.PlayerWantsToEnter();

            // Enemy or wall: attack instead of moving.
            if (!canEnter)
            {
                if (animator != null)
                {
                    animator.SetBool("IsWalking", false);
                    animator.SetTrigger("Attack");
                }

                // Play attack sound.
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayAttack();
                }
     if (attackEffect != null)
    {
    attackEffect.Play();
}
                AdvanceTurn();
                return;
            }
        }

        // Start smooth movement.
        moveCoroutine = StartCoroutine(
            SmoothMove(targetCell, cellObject)
        );
    }

    private IEnumerator SmoothMove(
        Vector2Int targetCell,
        CellObject cellObject)
    {
        isMoving = true;

        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }

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

        // Ensure exact grid alignment.
        transform.position = endPosition;
        currentCell = targetCell;
if (walkingDust != null)
{
    walkingDust.Play();
}
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }

        // Play walking sound.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWalk();
        }

        isMoving = false;
        moveCoroutine = null;

        BoardManager.CellData cell =
            boardManager.GetCellData(currentCell);

        ExitCellObject exit =
            cellObject as ExitCellObject;

        // Collect food or activate other enterable objects.
        if (cellObject != null && exit == null)
        {
            if (cell != null &&
                cell.ContainedObject == cellObject)
            {
                cell.ContainedObject = null;
            }

            cellObject.PlayerEntered();
        }

        // Complete one player turn.
        AdvanceTurn();

        // Process exit after the turn is complete.
        if (exit != null &&
            GameManager.Instance != null &&
            GameManager.Instance.CanPlayerAct)
        {
            exit.PlayerEntered();
        }

        Debug.Log("Player moved to: " + currentCell);
    }

    private void AdvanceTurn()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.TurnManager != null)
        {
            GameManager.Instance.TurnManager.Tick();
        }
    }
}
