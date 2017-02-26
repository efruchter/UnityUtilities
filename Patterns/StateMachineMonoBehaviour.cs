using System;
using UnityEngine;
// ReSharper disable CheckNamespace

namespace Patterns.State
{
    /// <summary>
    /// Monobehaviour implementation of a state pattern. Each state is a seperate class.
    /// It is up to the behaviour to properly link up the exit states in each state.
    /// </summary>
    public abstract class StatefulMonoBehaviour : MonoBehaviour
    {
        private readonly StateMachine _stateMachine = new StateMachine();

        /// <summary>
        /// Get the reference to the starting state.
        /// </summary>
        /// <returns>the first state to run.</returns>
        public abstract IState GetStartingState();

        /// <summary>
        /// Link the exit state references in each state to the corresponding state.
        /// </summary>
        public abstract void SetupInterStateConnections();

        /// <summary>
        /// Set the state of the internal state machine.
        /// </summary>
        /// <param name="state">new state.</param>
        protected void SetState(IState state)
        {
            _stateMachine.SetState(state);
        }

        public void Start()
        {
            SetupInterStateConnections();
            SetState(GetStartingState());
        }

        public void Update()
        {
            _stateMachine.Update();
        }

        public void OnEnable()
        {
            _stateMachine.Resume();
        }

        public void OnDisable()
        {
            _stateMachine.Pause();
        }
    }

    public sealed class StateMachine
    {
        private IState _state;

        private static readonly IState NullState = new NullState();

        public StateMachine()
        {
            _state = NullState;
        }

        public StateMachine(IState state) : this()
        {
            SetState(state);
        }

        public void SetState(IState state)
        {
            if (state == null)
            {
                state = NullState;
            }

            _state.OnStop();
            _state = state;
            _state.OnStart();
        }

        public void Pause()
        {
            _state.OnStop();
        }

        public void Resume()
        {
            _state.OnStart();
        }

        public void Update()
        {
            _state.OnUpdate(this);
        }
    }

    public interface IState
    {
        void OnStart();
        void OnUpdate(StateMachine stateMachine);
        void OnStop();
    }

    public sealed class NullState : IState
    {
        public void OnStart() { }

        public void OnUpdate(StateMachine stateMachine) { }

        public void OnStop() { }
    }
}