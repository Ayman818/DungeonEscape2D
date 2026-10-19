
using UnityEngine;

public class WallObject : CellObject
{
    [SerializeField, Range(1, 10)]
    [Tooltip("Number of hits required to destroy this wall")]
    private int health = 3;

    private BoardManager boardManager;
    private Vector2Int cellPosition;

    public void Setup(BoardManager board, Vector2Int position)
    {
        boardManager = board;
        cellPosition = position;
    }

    public override bool PlayerWantsToEnter()
    {
        health--;

        Debug.Log("Wall hit! HP remaining: " + health);

        if (health <= 0)
        {
            Debug.Log("Wall destroyed!");

            if (boardManager != null)
            {
                boardManager.RemoveCellObject(
                    cellPosition,
                    this
                );
            }

            Destroy(gameObject);
        }

        // Do not enter the cell on the same action.
        return false;
    }
}
