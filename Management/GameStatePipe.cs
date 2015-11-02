using UnityEngine;
using System.Collections.Generic;

namespace State {
	
	public class GameStatePipeObject : MonoBehaviour {	
		public LinkedList< GameState > statePipe;
		public bool startCalledOnGameState;
		
		void Awake () {
			statePipe = new LinkedList< GameState >();
			startCalledOnGameState = false;
		}
		
		void Update () {
			if ( statePipe.Count == 0 ) {
				return;
			}
			
			while ( statePipe.Count > 0 ) {
				var first = statePipe.First.Value;
				
				if ( !startCalledOnGameState ) {
					startCalledOnGameState = true;
					first.OnGameStateStart();
				}
				
				first.OnGameStateUpdate();
				
				if ( first.IsStateComplete() ) {
					first.OnGameStateEnd();
					statePipe.RemoveFirst();
					startCalledOnGameState = false;
				} else {
					break;
				}
			}
		}
	}
	
	public static class GameStatePipe< T > {
		static GameStatePipeObject singleton;
		
		public static void Enqueue ( GameState state ) {
			CreateIfNeeded();
			
			singleton.statePipe.AddLast( state );
			
			if ( singleton.statePipe.Count == 1 ) {
				state.OnGameStateStart();
				singleton.startCalledOnGameState = true;
			}
		}
		
		public static void EnqueueNext ( GameState state ) {
			CreateIfNeeded();
			
			var pipe = singleton.statePipe;
			
			if ( pipe.Count > 0 ) {
				pipe.AddAfter( pipe.First, state );
				return;
			}
			
			Enqueue( state );
		}
		
		public static void ClearQueue ( bool endRunningState = false ) {
			CreateIfNeeded();
			
			var pipe = singleton.statePipe;
			
			if ( pipe.Count == 0 ) {
				return;
			}
			
			if ( endRunningState ) {
				singleton.startCalledOnGameState = false;
			}
			
			if ( !singleton.startCalledOnGameState ) {
				pipe.Clear();
				
				return;
			}
			
			while ( pipe.Count > 1 ) {
				pipe.RemoveLast();
			}
		}
		
		public static bool isEmpty {
			get {
				CreateIfNeeded();
				
				return singleton.statePipe.Count == 0;
			}
		}
		
		public static int count {
			get {
				CreateIfNeeded();
				
				return singleton.statePipe.Count;
			}
		}
		
		static void CreateIfNeeded () {
			if ( exists ) {
				return;
			}
			
			singleton = new GameObject( "Game State Pipe <" + typeof( T ).ToString() + ">" ).AddComponent< GameStatePipeObject >();
			MonoBehaviour.DontDestroyOnLoad( singleton.gameObject );
		}
		
		public static void Destroy() {
			if ( !exists ) {
				return;
			}
			
			MonoBehaviour.DestroyImmediate( singleton );
			singleton = null;
		}
		
		public static bool exists {
			get {
				return singleton != null;
			}
		}
	}
	
	public interface GameState {
		void OnGameStateStart ();
		void OnGameStateUpdate ();
		void OnGameStateEnd ();
		bool IsStateComplete ();
	}
}
