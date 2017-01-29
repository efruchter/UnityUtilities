using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kazoo.FiniteStateMachine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kazoo.FiniteStateMachine
{
    #region Implementation
    public class FiniteStateMachineComponent : MonoBehaviour
    {
        public UpdateBehaviour updateBehaviour;
        public enum UpdateBehaviour
        {
            Update, LateUpdate, FixedUpdate, Manual
        }

        public StateMonoBehaviour initialBehaviour;

        FiniteStateMachine fsm = new FiniteStateMachine();

        public IState state
        {
            get
            {
                return fsm.state;
            }
        }

        void Start()
        {
            fsm.TransitionTo(initialBehaviour);
        }

        void Update()
        {
            if (updateBehaviour == UpdateBehaviour.Update)
            {
                UpdateState();
            }
        }

        void FixedUpdate()
        {
            if (updateBehaviour == UpdateBehaviour.FixedUpdate)
            {
                UpdateState();
            }
        }

        void LateUpdate()
        {
            if (updateBehaviour == UpdateBehaviour.LateUpdate)
            {
                UpdateState();
            }
        }

        public void UpdateState()
        {
            fsm.Update();
        }

        void OnEnable()
        {
            fsm.Play();
        }

        void OnDisable()
        {
            fsm.Pause();
        }
    }

    public abstract class StateMonoBehaviour : MonoBehaviour, IState
    {
        public abstract void OnStart(FiniteStateMachine stateMachine);
        public abstract void OnStop(FiniteStateMachine stateMachine);
        public abstract void OnUpdate(FiniteStateMachine stateMachine);
    }
    #endregion

    #region Abstract
    public interface IState
    {
        void OnStart(FiniteStateMachine stateMachine);
        void OnUpdate(FiniteStateMachine stateMachine);
        void OnStop(FiniteStateMachine stateMachine);
    }

    public class FiniteStateMachine
    {
        public IState state { private set; get; }

        public FiniteStateMachine()
        {
            state = Null;
        }

        public FiniteStateMachine(IState initialState) : this()
        {
            TransitionTo(initialState);
        }

        public void TransitionTo(IState newState)
        {
            UnityEngine.Assertions.Assert.AreNotEqual(state, null);
            if (newState == null)
                newState = Null;
            state.OnStop(this);
            state = newState;
            state.OnStart(this);
        }

        public void TransitionToNullState()
        {
            TransitionTo(Null);
        }

        public void Update()
        {
            UnityEngine.Assertions.Assert.AreNotEqual(state, null);
            state.OnUpdate(this);
        }

        public void Play()
        {
            UnityEngine.Assertions.Assert.AreNotEqual(state, null);
            state.OnStart(this);
        }

        public void Pause()
        {
            UnityEngine.Assertions.Assert.AreNotEqual(state, null);
            state.OnStop(this);
        }

        public static readonly IState Null = new NullState();
        public sealed class NullState : IState
        {
            public void OnStart(FiniteStateMachine stateMachine) { }
            public void OnStop(FiniteStateMachine stateMachine) { }
            public void OnUpdate(FiniteStateMachine stateMachine) { }
        }
    }
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(FiniteStateMachineComponent))]
public class FiniteStateMachineBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var fsm = target as FiniteStateMachineComponent;

        var state = fsm.state;
        string stateName = string.Empty;
        if (state == null)
        {
            stateName = "[Destroyed]";
        }
        else if (state is MonoBehaviour)
        {
            if ((state as MonoBehaviour) == null)
            {
                stateName = "[Destroyed]";
            }
            else
            {
                stateName = (state).ToString();
            }
        }
        else if (state == Kazoo.FiniteStateMachine.FiniteStateMachine.Null)
        {
            stateName = "[None]";
        }
        else
        {
            stateName = state.GetType().ToString();
        }
        EditorGUILayout.LabelField("CurrentState: " + stateName);
    }
}
#endif