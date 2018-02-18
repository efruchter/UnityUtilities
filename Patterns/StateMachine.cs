using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Patterns.StateMachine
{
	/// <summary>
	/// A Finite State Machine built to synergize with Unity's messaging system.
	/// Support for MonoBehaviours and pure C# classes.
	/// -Eric
	/// </summary>
	[ AddComponentMenu( "Patterns/State Machine" ) ]
	public class StateMachine : MonoBehaviour
	{
		[ Tooltip( "An IState or IStateProvider that will be used as the first state in this machine." ) ]
		public UnityEngine.Object EntryState;

		[ Tooltip( "Transition to EntryState every OnEnable." ) ]
		public bool RestartOnEnable;

		private IState _currentState = null;
		private bool _currentIsTickable;
		private bool _currentIsLateTickable;
		private bool _currentIsFixedTickable;

		#if UNITY_EDITOR
		public IState _previousState { private set; get; }
		#endif

		#region Unity Messages

		void OnValidate()
		{
			if ( !this.IsEntryStateValid() )
			{
				Debug.LogError( "EntryState components must implement the IState or IStateProvider interfaces.", this.EntryState );
				this.EntryState = null;
			}
		}

		void Start()
		{
			this.ProcessEntryState();
		}

		void OnEnable()
		{
			if ( this.RestartOnEnable )
			{
				this.ProcessEntryState();
			}
		}

		void Update()
		{
			this.EnsureCurrentStateIsValid();
			if ( this._currentIsTickable )
			{
				this.SetState( ( this._currentState as ITickableState ).OnStateTick() );
			}
		}

		void LateUpdate()
		{
			this.EnsureCurrentStateIsValid();
			if ( this._currentIsLateTickable )
			{
				this.SetState( ( this._currentState as ILateTickableState ).OnStateLateTick() );
			}
		}

		void FixedUpdate()
		{
			this.EnsureCurrentStateIsValid();
			if ( this._currentIsFixedTickable )
			{
				this.SetState( ( this._currentState as IFixedTickableState ).OnStateFixedTick() );
			}
		}

		[ContextMenu ("Clear State")]
		void ClearState ()
		{
			this.SetState( null );
		}
		#endregion Unity Messages

		private void ProcessEntryState()
		{
			if ( StateMachine.IsNull( this.EntryState ) )
			{
				this.SetState( null );
			}
			else if ( this.EntryState is IState )
			{
				this.SetState( this.EntryState.AsIState() );
			}
			else if ( this.EntryState is IStateProvider )
			{
				this.SetState( ( this.EntryState as IStateProvider ).InitialState() );
			}
		}

		/// <summary>
		/// Is the entry state valid (not null).
		/// </summary>
		/// <returns>True if valid.</returns>
		public bool IsEntryStateValid()
		{
			return IsEntryStateEmpty()
				|| this.EntryState is IState
				|| this.EntryState is IStateProvider;
		}

		public bool IsEntryStateEmpty()
		{
			return this.EntryState == null;
		}

		/// <summary>
		/// Set the state of the machine. States are checked by reference, not type.
		/// </summary>
		/// <param name="newState">The new state to switch to.</param>
		public void SetState( IState newState )
		{
			// Handles case where Unity has freed native Object referenced by the interface.
			if ( StateMachine.IsNull( newState ) )
			{
				newState = null;
			}

			// Exit if we do not need to transition
			if ( newState == this._currentState )
			{
				return;
			}

			// Exit old state
			if ( ! StateMachine.IsNull( this._currentState ) )
			{
				this._currentState.OnStateExit();
			}

			#if UNITY_EDITOR
			this._previousState = this._currentState;
			#endif

			// Store new state
			this._currentState = newState;

			// If new state is null, we stop and do nothing.
			if ( StateMachine.IsNull( this._currentState ) )
			{
				this._currentIsTickable = false;
				this._currentIsLateTickable = false;
				this._currentIsFixedTickable = false;
				return;
			}

			// Setup updates
			this._currentIsTickable = this._currentState is ITickableState;
			this._currentIsLateTickable = this._currentState is ILateTickableState;
			this._currentIsFixedTickable = this._currentState is IFixedTickableState;

			// Enter new state and be prepared to transition again, recursively if needed.
			this.SetState( this._currentState.OnStateEnter() );
		}

		public IState CurrentState()
		{
			return this._currentState;
		}

		/// <summary>
		/// Perform a null check against both Mono and Unity references.
		/// Handles the case where Unity has freed a native object but has not nulled
		/// the interface reference to it.
		/// </summary>
		/// <param name="obj">The native/C# object reference.</param>
		/// <returns>True if the object is null.</returns>
		private static bool IsNull( object obj )
		{
			return obj == null || obj.Equals( null );
		}

		/// <summary>
		/// Ensure the current state has not been deallocated.
		/// </summary>
		private void EnsureCurrentStateIsValid()
		{
			if ( StateMachine.IsNull( this._currentState ) )
			{
				this.SetState( null );
			}
		}
	}

	public interface IState
	{
		/// <summary>
		/// Called when this state is entered on the state machine.
		/// </summary>
		/// <returns>The next state, or itself if no further transition is needed.</returns>
		IState OnStateEnter();

		/// <summary>
		/// Called when this state is exited, before the enter of the new state.
		/// </summary>
		void OnStateExit();
	}

	/// <summary>
	/// A state that has an Update.
	/// </summary>
	public interface ITickableState : IState
	{
		IState OnStateTick();
	}

	/// <summary>
	/// A state that has a LateUpdate.
	/// </summary>
	public interface ILateTickableState : IState
	{
		IState OnStateLateTick();
	}

	/// <summary>
	/// A state that has a FixedUpdate.
	/// </summary>
	public interface IFixedTickableState : IState
	{
		IState OnStateFixedTick();
	}

	/// <summary>
	/// A class that provides a state.
	/// </summary>
	public interface IStateProvider
	{
		IState InitialState();
	}

	public static class StateExtensions
	{
		public static IState AsIState( this UnityEngine.Object stateObject )
		{
			return stateObject as IState;
		}
	}

	#if UNITY_EDITOR
	[ CustomEditor( typeof( StateMachine ) ) ]
	public class IngredientDrawer : Editor
	{
		public override void OnInspectorGUI()
		{
			StateMachine stateMachine = ( this.target as StateMachine );
			
			if ( stateMachine == null )
			{	
				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				GUIStyle greenStyle = new GUIStyle( EditorStyles.boldLabel );
				greenStyle.normal.textColor = Color.Lerp( Color.green, Color.black, 0.75f );

				EditorGUILayout.LabelField( "Current State: ", greenStyle );

				IState state = stateMachine.CurrentState();
				if ( state != null )
				{
					EditorGUILayout.LabelField( state.GetType().ToString(), greenStyle );
				}
				else
				{
					EditorGUILayout.LabelField( "None", greenStyle );
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUIStyle blueStyle = new GUIStyle( EditorStyles.boldLabel );
				blueStyle.normal.textColor = Color.Lerp( Color.green, Color.black, 0.50f );

				EditorGUILayout.LabelField( "Previous State: ", blueStyle );

				IState state = stateMachine._previousState;
				if ( state != null )
				{
					EditorGUILayout.LabelField( state.GetType().ToString(), blueStyle );
				}
				else
				{
					EditorGUILayout.LabelField( "None", blueStyle );
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			base.OnInspectorGUI();

			if ( stateMachine.IsEntryStateEmpty() )
			{
				EditorGUILayout.HelpBox( "You can link an IState or IStateProvider through EntryState to get the State Machine started.", MessageType.Info );
			}
		}
	}
	#endif
}