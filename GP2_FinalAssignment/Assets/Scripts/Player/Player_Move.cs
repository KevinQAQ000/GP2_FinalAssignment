using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家移动状态
/// </summary>
public class Player_Move : PlayerStateBase
{
    public override void Enter()
    {
        base.Enter();
        // 当状态机切换到“移动”时，立即播放走步动画
        // 注意：这里的字符串必须和你 Animator 里的动画名称完全一致
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