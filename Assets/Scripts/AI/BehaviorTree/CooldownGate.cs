using UnityEngine;

// Tambahan untuk framework BT modul P06.
// CooldownDecorator milik modul mengembalikan Running selama cooldown, sehingga Selector
// berhenti di cabang itu. Untuk boss dan Wizard kita butuh cabang lain tetap dicoba,
// jadi CooldownGate mengembalikan Failure selama cooldown.
public class CooldownGate : BTNode
{
    private readonly BTNode child;
    private readonly float cooldown;
    private float nextAllowedTime;

    public CooldownGate(BTNode child, float cooldown, float initialDelay = 0f)
    {
        this.child = child;
        this.cooldown = cooldown;
        nextAllowedTime = Time.time + initialDelay;
    }

    public float Remaining => Mathf.Max(0f, nextAllowedTime - Time.time);

    public void Delay(float seconds)
    {
        nextAllowedTime = Mathf.Max(nextAllowedTime, Time.time + seconds);
    }

    public override NodeState Tick()
    {
        if (Time.time < nextAllowedTime)
            return NodeState.Failure;

        NodeState state = child.Tick();
        if (state == NodeState.Success)
            nextAllowedTime = Time.time + cooldown;

        return state;
    }
}

// Membalik hasil anak: Success <-> Failure (Running tetap Running).
public class InverterNode : BTNode
{
    private readonly BTNode child;

    public InverterNode(BTNode child)
    {
        this.child = child;
    }

    public override NodeState Tick()
    {
        NodeState state = child.Tick();
        if (state == NodeState.Success) return NodeState.Failure;
        if (state == NodeState.Failure) return NodeState.Success;
        return NodeState.Running;
    }
}
