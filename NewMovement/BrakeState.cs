using UnityEngine;

namespace NewMovement
{
    [ExtensionOfNativeClass]
    public class BrakeState : PlayerState
    {
        public override void Awake()
        {
        }

        public override void Enter()
        {
            // can add some dust particles when character starts braking
            player.FlipCharacter(player.input.horizontal);
        }

        // handle braking when character switches direction
        public override void Update()
        {
            player.ApplyDeceleration(Time.deltaTime);
            player.ApplyGravity(Time.deltaTime);
            if (!player.grounded)
            {
                player.ApplyFall();
            }
            if (player.grounded)
            {
                if (player.input.jumpAction)
                    stateMachine.ChangeState<JumpState>();
                else if (Mathf.Abs(player.velocity.x) <= 0.1f || player.input.horizontal == 0.0)
                {
                    stateMachine.ChangeState<NormalState>();
                }
            }
            else
                stateMachine.ChangeState<FallState>();
        }
    }
}
