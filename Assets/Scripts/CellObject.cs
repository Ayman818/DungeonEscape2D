
using UnityEngine;

public class CellObject : MonoBehaviour
{
    public virtual bool PlayerWantsToEnter()
    {
        return true;
    }

    public virtual void PlayerEntered()
    {
        // Overridden by child classes.
    }
}
