using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for player states
/// Abstracts common fields and functions required by all player states
/// </summary>
public class PlayerStateBase : StateBase
{
    protected Player_Controller player; //Player controller reference

    //This is an override function. The base class StateBase has an Init function, 
    //and we override it to adapt to the specific requirements of player states.
    //The purpose of the Init function is to initialize the state and assign ownership.
    public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
    {
        base.Init(owner, stateType, stateMachine); //Call the base Init to ensure parent initialization logic is executed
        player = owner as Player_Controller; //Cast owner to Player_Controller and assign it to the player field, allowing access to its properties and methods within the state
    }

    /// <summary>
    /// Plays an animation
    /// </summary>
    protected void PlayAnimation(string animationName, float fixedTime = 0.25f) //This function plays an animation where animationName is the state name and fixedTime is the crossfade duration (defaulting to 0.25s)
    {
        //Call CrossFadeInFixedTime on the player controller's animator to transition to the specified animation state over a fixed duration
        player.animator.CrossFadeInFixedTime(animationName, fixedTime);
    }
}