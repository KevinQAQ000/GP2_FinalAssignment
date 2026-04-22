using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家待机状态
/// </summary>
public class Player_Idle : PlayerStateBase
{
    public override void Enter()
    {
        base.Enter();
        // 切回待机动画
        PlayAnimation("idle");
    }

    public override void Update()
    {
        base.Update();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            stateMachine.ChangeState<Player_Move>((int)PlayerState.Move);
        }
    }
}