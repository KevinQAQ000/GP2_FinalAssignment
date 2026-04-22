using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 【修复1】：删掉 using JKFrame; 这一行！

/// <summary>
/// 玩家状态基类
/// 抽象出所有玩家状态所需要共同 字段、函数等
/// </summary>
public class PlayerStateBase : StateBase
{
    protected Player_Controller player;

    // 【无需修改】：因为我们刚刚升级了底层的 StateBase，现在这里的 3个参数 和 override 已经能完美对应上了！
    public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
    {
        base.Init(owner, stateType, stateMachine);
        player = owner as Player_Controller;
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    protected void PlayAnimation(string animationName, float fixedTime = 0.25f)
    {
        player.animator.CrossFadeInFixedTime(animationName, fixedTime);
    }
}