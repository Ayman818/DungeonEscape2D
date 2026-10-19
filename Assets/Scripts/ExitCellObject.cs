
using UnityEngine;

public class ExitCellObject : CellObject
{
    public override void PlayerEntered()
    {
        Debug.Log("Exit reached!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NextLevel();
        }
    }
}
