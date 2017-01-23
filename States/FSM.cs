using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FSM
{
    /// <summary>
    /// State monobehaviour
    /// </summary>
    public abstract class FSM_State : MonoBehaviour
    {
        [System.NonSerialized]
        public FSM fsm;

        public abstract void OnStart();
        public abstract void OnUpdate();
        public abstract void OnStop();
    }

    /// <summary>
    /// State machine monobehaviour.
    /// </summary>
    public class FSM : MonoBehaviour
    {
#if UNITY_EDITOR
        public string comment = string.Empty;
#endif
        public FSM_State startingState;
        private FSM_State _state;

        private void Awake()
        {
            if (startingState != null)
            {
                state = startingState;
            }
        }

        public FSM_State state
        {
            set
            {
                TransitionTo(value);
            }

            get
            {
                return _state;
            }
        }

        private void TransitionTo(FSM_State state)
        {
            if (state != null)
            {
                state.fsm = this;
            }

            if (_state != null)
            {
                _state.OnStop();
            }
            
            _state = state;

            if (_state != null)
            {
                _state.OnStart();
            }
        }

        private void Update()
        {
            if (_state != null)
            {
                _state.OnUpdate();
            }
        }
    }
}
