using UnityEngine;

namespace NewMovement
{
    [ExtensionOfNativeClass]
    public class FallState : PlayerState
    {
        public override void Awake()
        {
        }

        public override void Update()
        {
            player.FlipCharacter(player.input.horizontal);
            player.ApplyAcceleration(Time.deltaTime);
            player.ApplyGravity(Time.deltaTime);
            if (!player.grounded)
            {
                player.ApplyFall();
            }

            if (player.grounded)
            {
                stateMachine.ChangeState<NormalState>();
            }
        }
    }
}
