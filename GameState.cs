using UnityEngine;
using System.Collections.Generic;

namespace GameState
{
	public delegate void GameStateFunction();
	public delegate bool IsGameStateOver();
	
	public interface State
	{		
		void OnGameStateStart();
		void OnGameStateUpdate();
		void OnGameStateEnd();
		
		bool IsGameStateOver();
	}
	
	public class GameStatePipe
	{
		LinkedList<DelegateState> _pipe;
		bool _currentStateActivated;

		public GameStatePipe()
		{
			this._pipe = new LinkedList<DelegateState>();
			this._currentStateActivated = false;
		}
		
		private void AddState(DelegateState state)
		{
			this._pipe.AddLast(state);
		}
		
		public void AddState(GameStateFunction onStart, GameStateFunction onUpdate, GameStateFunction onEnd, IsGameStateOver isStateOver)
		{
			this.AddState(	new DelegateState(){
								onStart = onStart,
								onUpdate = onUpdate,
								onEnd = onEnd,
								isStateOver = isStateOver});
		}
	
		public void AddState(State state)
		{
			this.AddState(	() => {state.OnGameStateStart();},
							() => {state.OnGameStateUpdate();},
							() => {state.OnGameStateEnd();},
							() => {return state.IsGameStateOver();});
		}
		
		public void Update()
		{
			if (this._pipe.Count == 0)
			{
				return;
			}
			
			DelegateState currentState = this._pipe.First.Value;
			
			if (!this._currentStateActivated)
			{
				currentState.onStart();
				this._currentStateActivated = true;
			}
			
			currentState.onUpdate();
			
			if (currentState.isStateOver())
			{
				currentState.onEnd();
				
				this._currentStateActivated = false;
				this._pipe.RemoveFirst();
			}
		}
		
		private class DelegateState
		{
			static void EmptyStateFunction(){}
			static bool EmptyIsOverFunction(){return true;}
			
			public GameStateFunction onStart = EmptyStateFunction;
			public GameStateFunction onUpdate = EmptyStateFunction;
			public GameStateFunction onEnd = EmptyStateFunction;
			public IsGameStateOver isStateOver = EmptyIsOverFunction;
		}
	}
}
