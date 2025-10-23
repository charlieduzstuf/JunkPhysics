using UnityEngine;

namespace NewMovement
{
    [ExtensionOfNativeClass]
    public class NormalState : PlayerState
    {
        public override void Awake()
        {
        }

        public override void Update()
        {
            player.FlipCharacter(player.input.horizontal); // Update character direction based on input
            player.ApplySlopeFactor(Time.deltaTime);
            player.ApplyAcceleration(Time.deltaTime);
            player.ApplyFriction(Time.deltaTime);
            player.ApplyFall();
            player.ApplyGravity(Time.deltaTime);
            if (player.grounded)
            {
                if (Mathf.Abs(Mathf.Sign(player.velocity.x) - Mathf.Sign(player.input.horizontal)) > .01f && player.input.horizontal != 0.0)  // Check for direction change
                {
                    if (Mathf.Abs(player.velocity.x) >= player.minSpeedToBrake) // Check if speed is enough to brake
                        stateMachine.ChangeState<BrakeState>(); // Switch to braking state
                    if (player.velocity.x > 0.0) // Apply initial brake velocity
                        player.velocity.x = -0.1f;
                    else
                        player.velocity.x = 0.1f;
                }
                if (player.input.jumpActionDown) // Check for jump input
                {
                    player.LockJump(player.jumpTime); // Lock jump for specified duration
                    stateMachine.ChangeState<JumpState>();// Switch to jumping state
                }
            }
            else
                stateMachine.ChangeState<FallState>();// Switch to falling state if not grounded
        }
    }
}
