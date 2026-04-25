using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Player state enumeration
/// </summary>
public enum PlayerState
{
    Idle,
    Move,
    Attack,
    BeAttack,
    Dead
}

/// <summary>
/// Player controller class
/// </summary>
public class Player_Controller : MonoBehaviour, IStateMachineOwner
{
    //Singleton pattern (for easy access by states)
    public static Player_Controller Instance { get; private set; }

    public Animator animator; //Animator component
    public CharacterController characterController; //CharacterController component
    private StateMachine stateMachine; //State machine instance
    public Transform playerTransform { get; private set; } //Reference to player's Transform (for state access)

    // Player movement speed
    public float moveSpeed = 4f;

    private void Awake()
    {
        //Set singleton as a safety mechanism to prevent confusion if multiple players exist in a scene
        Instance = this;
        playerTransform = transform; //Assign early to prevent null references during state initialization

        //Automatically get CharacterController to prevent errors if not assigned in the Inspector
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void Start()
    {
        Init();
    }

    public void Init() //Initialization interface for the state machine
    {
        playerTransform = transform; //Re-ensure Transform is assigned
        stateMachine = new StateMachine(); //Create state machine instance
        stateMachine.Init(this); //Initialize state machine with this object as owner

        // Set initial state to Idle
        stateMachine.ChangeState<Player_Idle>((int)PlayerState.Idle);
    }

    private void Update()
    {
        if (stateMachine != null) //Safety check to prevent calling Update before initialization
        {
            stateMachine.Update(); //Update state machine every frame
        }
    }

    /// <summary>
    /// Executes the actual physical movement
    /// </summary>
    public void DoMove(float h, float v)
    {
        Vector3 moveDir = new Vector3(h, 0, v); //Calculate movement direction based on input

        if (moveDir != Vector3.zero) //Only rotate if there is input (prevents snapping to forward when idle)
        {
            //Instantly rotate the player to face the movement direction
            playerTransform.rotation = Quaternion.LookRotation(moveDir);
        }

        //Calculate movement vector (horizontal components * speed * time, with gravity simulation)
        Vector3 motion = moveDir * moveSpeed * Time.deltaTime;
        motion.y = -9.8f * Time.deltaTime; //Simple gravity simulation


        if (characterController != null) //If CharacterController component exists
        {
            //Only allow movement if the controller is enabled
            if (characterController.enabled) //Check prevents errors during specific cases like loading/teleporting
            {
                //Use CharacterController for movement to handle collisions and slopes automatically
                characterController.Move(motion);
            }
            //If it exists but is disabled, do nothing this frame and wait for it to be re-enabled
        }
        else
        {
            //Fallback solution (if the component is missing)
            Debug.LogWarning("⚠️ Warning: The player does not have the CharacterController component! Reverting to wallhack mode.");
            //Move via Transform directly; this lacks collision but allows movement for testing
            playerTransform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}

/// <summary>
/// Interface for state machine owners
/// </summary>
public interface IStateMachineOwner { }

public abstract class StateBase //Base class for all specific states
{
    protected IStateMachineOwner owner; //Reference to the owner (allows access to player components)
    protected int stateType; //State type (mapped to PlayerState enum for management)
    protected StateMachine stateMachine; //Reference to the state machine (allows state switching)

    // Initialization interface for states, called when the state instance is created
    public virtual void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
    {
        this.owner = owner;
        this.stateType = stateType;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { } //Logic executed upon entering the state (e.g., play animation, reset timers)
    public virtual void Update() { } //Logic executed every frame (e.g., handle input, check transitions)
    public virtual void Exit() { } //Logic executed upon exiting the state (e.g., stop animation, cleanup)
}

public class StateMachine
{
    private IStateMachineOwner owner; //Reference to the owner
    private StateBase currentState; //Reference to the current active state
    private Dictionary<int, StateBase> stateDict = new Dictionary<int, StateBase>(); //Cache for state instances to avoid GC allocation

    public void Init(IStateMachineOwner owner) //Initialize state machine with an owner
    {
        this.owner = owner;
    }

    public void ChangeState<T>(int stateId) where T : StateBase, new()
    {
        if (currentState != null)
        {
            currentState.Exit(); //Exit current state if it exists
        }

        if (!stateDict.ContainsKey(stateId)) //Check if the state instance already exists in the cache
        {
            T newState = new T();
            newState.Init(owner, stateId, this);
            stateDict.Add(stateId, newState);
        }

        currentState = stateDict[stateId]; //Switch to the new state
        currentState.Enter(); //Execute the entry logic for the new state
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update(); //Update the current state every frame
        }
    }
}