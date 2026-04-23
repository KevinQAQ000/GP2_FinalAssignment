using System;
using System.Collections.Generic;

// 节点的状态：运行中、成功、失败
public enum NodeState { Running, Success, Failure }

// 行为树基类节点
public abstract class BTNode
{
    public NodeState state;
    public abstract NodeState Evaluate();
}

// 行为节点（执行具体动作：如移动、吃草）
public class BTAction : BTNode
{
    private Func<NodeState> action;
    public BTAction(Func<NodeState> action) { this.action = action; }
    public override NodeState Evaluate() { return action(); }
}

// 序列节点（Sequence：相当于逻辑上的 AND）
// 依次执行子节点，只有全部成功才算成功，遇到一个失败就立刻返回失败
public class BTSequence : BTNode
{
    private List<BTNode> nodes = new List<BTNode>();
    public BTSequence(params BTNode[] nodes) { this.nodes.AddRange(nodes); }

    public override NodeState Evaluate()
    {
        bool anyChildRunning = false;
        foreach (var node in nodes)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    state = NodeState.Failure;
                    return state;
                case NodeState.Success:
                    continue;
                case NodeState.Running:
                    anyChildRunning = true;
                    continue;
            }
        }
        state = anyChildRunning ? NodeState.Running : NodeState.Success;
        return state;
    }
}

// 选择节点（Selector：相当于逻辑上的 OR）
// 依次执行子节点，遇到一个成功的就立刻返回成功，全失败才返回失败
public class BTSelector : BTNode
{
    private List<BTNode> nodes = new List<BTNode>();
    public BTSelector(params BTNode[] nodes) { this.nodes.AddRange(nodes); }

    public override NodeState Evaluate()
    {
        foreach (var node in nodes)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    continue;
                case NodeState.Success:
                    state = NodeState.Success;
                    return state;
                case NodeState.Running:
                    state = NodeState.Running;
                    return state;
            }
        }
        state = NodeState.Failure;
        return state;
    }
}