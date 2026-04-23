using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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

    // 玩家移动速度 (你可以把它提取出来方便在面板上改)
    public float moveSpeed = 4f;

    private void Awake()
    {
        Instance = this;
        playerTransform = transform; // 提前赋值，防止状态机在 Init 之前就访问到导致空引用

        // 【新增安全机制】：自动获取身上的 CharacterController，防止你在面板忘拖导致报错
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        playerTransform = transform;
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

        if (moveDir != Vector3.zero)
        {
            playerTransform.rotation = Quaternion.LookRotation(moveDir);
        }

        Vector3 motion = moveDir * moveSpeed * Time.deltaTime;
        motion.y = -9.8f * Time.deltaTime;

        // ==========================================
        // 【修改这里】：加上 && characterController.enabled 的安全检查！
        // ==========================================
        if (characterController != null)
        {
            // 只有当控制器不仅存在，而且是“开启状态”时，才允许移动
            if (characterController.enabled)
            {
                characterController.Move(motion);
            }
            // 如果它存在但是关闭了（比如正在读档传送中），这一帧就什么都不做，乖乖等传送完
        }
        else
        {
            // 兜底方案（万一你真的没挂载组件）
            Debug.LogWarning("⚠️ 警告：玩家身上没有 CharacterController 组件！退回穿墙模式。");
            playerTransform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}

// ==========================================
// 下面是状态机框架 (与你原来的一模一样，保持不变)
// ==========================================

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