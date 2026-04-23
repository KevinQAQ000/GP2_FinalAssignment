using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Íæ¼ÒÒÆ¶¯×´Ì¬
/// </summary>
public class Player_Move : PlayerStateBase
{
    public override void Enter()
    {
        base.Enter();
        PlayAnimation("walk");
    }

    public override void Update()
    {
        base.Update();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (h == 0 && v == 0)
        {
            stateMachine.ChangeState<Player_Idle>((int)PlayerState.Idle);
            return;
        }

        player.DoMove(h, v);
    }
}