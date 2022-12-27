using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayersBaseState 
{
    public abstract void EnterState(PlayersStateManagement player);

    public abstract void UpdateState(PlayersStateManagement player);

    public abstract void FixedUpdateState(PlayersStateManagement player);

    public abstract void LateUpdateState(PlayersStateManagement player);

    public abstract void OnCollisionEnter(PlayersStateManagement player, Collision collision);

    public abstract void OnMouseDown(PlayersStateManagement player);
}
