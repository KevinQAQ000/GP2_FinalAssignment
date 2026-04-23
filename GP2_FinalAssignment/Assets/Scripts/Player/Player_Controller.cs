using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家状态枚举
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
/// 玩家控制器
/// </summary>
public class Player_Controller : MonoBehaviour, IStateMachineOwner
{
    public static Player_Controller Instance { get; private set; }

    public Animator animator;
    public CharacterController characterController;
    private StateMachine stateMachine;
    public Transform playerTransform { get; private set; }

    private void Awake()
    {
        Instance = this;
        playerTransform = transform;// 这里提前赋值，防止状态机在 Init 之前就访问到 playerTransform 导致空引用
    }
    private void Start()
    {
        Init();
    }
    public void Init()
    {
        playerTransform = transform; // 继承 MonoBehaviour 后，transform 就回来了
        stateMachine = new StateMachine();
        stateMachine.Init(this);

        // 初始状态为待机
        stateMachine.ChangeState<Player_Idle>((int)PlayerState.Idle);
    }

    private void Update()
    {
        if (stateMachine != null)
        {
            stateMachine.Update();
        }
    }

    /// <summary>
    /// 真正的物理位移执行者
    /// </summary>
    public void DoMove(float h, float v)
    {
        Vector3 moveDir = new Vector3(h, 0, v);

        // 【修改这里】：在末尾加上 Space.World，确保角色永远按真实世界的东南西北移动
        playerTransform.Translate(moveDir * Time.deltaTime * 4f, Space.World);

        if (moveDir != Vector3.zero)
        {
            playerTransform.rotation = Quaternion.LookRotation(moveDir);
        }
    }
}

/// <summary>
/// 状态机所有者接口 (给 Player_Controller 贴的标签)
/// </summary>
public interface IStateMachineOwner { }

public abstract class StateBase
{
    protected IStateMachineOwner owner;
    protected int stateType;
    protected StateMachine stateMachine;
    public virtual void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
    {
        this.owner = owner;
        this.stateType = stateType;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}

public class StateMachine
{
    private IStateMachineOwner owner;
    private StateBase currentState;
    private Dictionary<int, StateBase> stateDict = new Dictionary<int, StateBase>();

    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }

    public void ChangeState<T>(int stateId) where T : StateBase, new()
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        if (!stateDict.ContainsKey(stateId))
        {
            T newState = new T();
            newState.Init(owner, stateId, this);
            stateDict.Add(stateId, newState);
        }

        currentState = stateDict[stateId];
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

}