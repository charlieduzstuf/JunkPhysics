using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewMovement
{
    public class PlayerStateMachine
    {
        public Player player { get; private set; }
        public PlayerState currentState { get; private set; }
        public Dictionary<Type, PlayerState> states { get; private set; }

        public PlayerStateMachine(Player player)
        {
            this.player = player;
            states = new Dictionary<Type, PlayerState>();

            // The states will be added externally by the Player class
        }

        public void AddState(Type type, PlayerState state)
        {
            states.Add(type, state);
        }

        public void ChangeState<T>() where T : PlayerState
        {
            var type = typeof(T);
            if (states.ContainsKey(type))
            {
                currentState?.Exit();
                currentState = states[type];
                currentState.Enter();
            }
        }

        public void UpdateState()
        {
            currentState?.Update();
        }
    }
}