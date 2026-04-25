using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player movement state
/// </summary>
public class Player_Move : PlayerStateBase
{
    public override void Enter() //Enter state
    {
        base.Enter();
        PlayAnimation("walk");
    }

    public override void Update() //Update every frame
    {
        base.Update();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (h == 0 && v == 0) //No input, switch to Idle state
        {
            stateMachine.ChangeState<Player_Idle>((int)PlayerState.Idle); //Switch to Idle state
            return;
        }

        player.DoMove(h, v); //Execute movement
    }
}