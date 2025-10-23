using UnityEngine;

namespace NewMovement
{
    [ExtensionOfNativeClass]
    public class JumpState : PlayerState
    {
        private bool release;

        public override void Awake()
        {
        }

        public override void Enter()
        {
            release = false;
            player.velocity.y = player.jumpImpulse;
        }

        public override void Update()
        {
            player.FlipCharacter(player.input.horizontal);
            player.ApplyAcceleration(Time.deltaTime);
            player.ApplyGravity(Time.deltaTime);
            if (!player.grounded)
            {
                if (player.input.jumpActionDown && player.canJump)
                {
                    player.LockJump(player.jumpTime);
                    stateMachine.ChangeState<FallState>();
                }
                // Handle variable jump height when jump button is released
                else if (!player.input.jumpAction && !release)
                {
                    release = true;
                    if (player.velocity.y > player.minJumpHeight)
                    {
                        player.velocity.y = player.minJumpHeight;
                    }
                }
            }
            else
                stateMachine.ChangeState<NormalState>(); // Return to normal state when grounded
        }
    }
}
