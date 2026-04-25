using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player idle state
/// </summary>
public class Player_Idle : PlayerStateBase
{
    public override void Enter() //Logic executed when entering the state
    {
        base.Enter(); //Call the base class Enter method
        //Switch back to the idle animation
        PlayAnimation("idle");
    }

    public override void Update()
    {
        base.Update();

        float h = Input.GetAxisRaw("Horizontal"); //Get horizontal input
        float v = Input.GetAxisRaw("Vertical");   //Get vertical input

        if (h != 0 || v != 0) //If there is input
        {
            //Transition to the Move state
            stateMachine.ChangeState<Player_Move>((int)PlayerState.Move);
        }
    }
}