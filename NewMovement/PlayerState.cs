using UnityEngine;
using NewMovement;

// base class for player states
public abstract class PlayerState : MonoBehaviour
{
    protected PlayerStateMachine stateMachine;
    protected Player player;

    public virtual void Awake()
    {
    }

    public void Initialize(PlayerStateMachine stateMachine, Player player)
    {
        this.stateMachine = stateMachine;
        this.player = player;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public abstract void Update();
}