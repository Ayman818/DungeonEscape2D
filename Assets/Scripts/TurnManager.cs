
using System;
using UnityEngine;

public class TurnManager
{
    public event Action OnTick;

    public int TurnCount { get; private set; }

    public TurnManager()
    {
        TurnCount = 0;
    }

    public void Tick()
    {
        TurnCount++;

        Debug.Log("Turn: " + TurnCount);

        OnTick?.Invoke();
    }
}
