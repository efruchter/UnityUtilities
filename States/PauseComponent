using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Component and manager for handling Pausing with Unity GameObjects.
/// Version 1.1a
/// -Eric
/// </summary>
namespace Pausing {
	public class PauseComponent : MonoBehaviour {
		[Tooltip( "The pause groups that this object is a part of." )]
		public PauseGroup pauseGroup;

		/// <summary>
		/// Methods to call when pause state has changed.
		/// </summary>
		public Action<bool> onPauseStateChanged;

		/// <summary>
		/// True if this module is paused, false otherwise.
		/// </summary>
		public bool paused { private set; get; }

		void Awake () {
			PauseManager._AddComponent( this );
		}

		void Start () {
			SetPaused( PauseManager.GetPaused( pauseGroup ) );
		}

		void OnDestroy () {
			PauseManager._RemoveComponent( this );
		}

		public void SetPaused ( bool paused ) {
			this.paused = paused;

			if ( onPauseStateChanged != null ) {
				onPauseStateChanged( paused );
			}
		}

		/// <summary>
		/// Generate a yield instruction that waits for seconds.
		/// </summary>
		/// <param name="delay">The time in seconds.</param>
		/// <returns>The Yield instruction.</returns>
		public PausableWaitForSeconds WaitForSeconds ( float delay ) {
			return new PausableWaitForSeconds( this, delay );
		}

		/// <summary>
		/// Generate a yield instruction that waits for a frame.
		/// </summary>
		/// <returns>The Yield instruction.</returns>
		public PausableWaitForFrame WaitForFrame () {
			return new PausableWaitForFrame( this );
		}

		/// <summary>
		/// Generate a yield instruction that waits until a delegate is true.
		/// </summary>
		/// <param name="waitUntil">The delegate to evaluate.</param>
		/// <returns>The Yield instruction.</returns>
		public PausableWaitUntil WaitUntil ( Func<bool> waitUntil ) {
			return new PausableWaitUntil( this, waitUntil );
		}

		/// <summary>
		/// Generate a yield instruction that waits until a delegate is false.
		/// </summary>
		/// <param name="waitWhile">The delegate to evaluate.</param>
		/// <returns>The Yield instruction.</returns>
		public PausableWaitWhile WaitWhile ( Func<bool> waitWhile ) {
			return new PausableWaitWhile( this, waitWhile );
		}
	}

	public static class PauseManager {
		static Action<bool>[] notifiers;
		static bool[] pauseStates;

		/// <summary>
		/// Mask for all pause groups.
		/// </summary>
		public const int ALL_MASK = ~0;

		static PauseManager () {
			notifiers = new Action<bool>[ 32 ];
			pauseStates = new bool[ 32 ];
		}

		public static void _AddComponent ( PauseComponent pauser ) {
			notifiers[ ( int ) pauser.pauseGroup ] += pauser.SetPaused;
		}

		public static void _RemoveComponent ( PauseComponent pauser ) {
			notifiers[ ( int ) pauser.pauseGroup ] -= pauser.SetPaused;
		}

		/// <summary>
		/// Sets the pause status for groups of pause components.
		/// </summary>
		/// <param name="groupMask">The int mask of the pause groups.</param>
		/// <param name="paused">True if paused, false if unpaused.</param>
		public static void SetPaused ( int groupMask, bool paused ) {
			for ( int i = 0; i < 32; i++ ) {
				if ( ( ( 1 << i ) & groupMask ) > 0 ) {
					SetPaused( ( PauseGroup ) i, paused );
				}
			}
		}

		/// <summary>
		/// Sets the pause status for groups of pause components.
		/// </summary>
		/// <param name="pauseGroups">List of pause groups.</param>
		/// <param name="paused">True if paused, false if unpaused</param>
		public static void SetPaused ( PauseGroup[] pauseGroups, bool paused ) {
			for ( int i = 0; i < pauseGroups.Length; i++ ) {
				SetPaused( pauseGroups[ i ], paused );
			}
		}

		/// <summary>
		/// Sets the pause status for a group of pause components.
		/// </summary>
		/// <param name="pauseGroup">The pause group.</param>
		/// <param name="paused">True if paused, false if unpaused</param>
		public static void SetPaused ( PauseGroup pauseGroup, bool paused ) {
			if ( GetPaused( pauseGroup ) == paused ) {
				return;
			}

			pauseStates[ ( int ) pauseGroup ] = paused;

			if ( ( notifiers[ ( int ) pauseGroup ] == null ) ) {
				return;
			}

			notifiers[ ( int ) pauseGroup ]( paused );
		}

		/// <summary>
		/// Get the pause status of a given group.
		/// </summary>
		/// <param name="pauseGroup">The pause group.</param>
		/// <returns>True if paused, false otherwise.</returns>
		public static bool GetPaused ( PauseGroup pauseGroup ) {
			return pauseStates[ ( int ) pauseGroup ];
		}
	}

	public static class PauseGroupUtils {
		public static int AsPauseMask ( this PauseGroup L ) {
			return ( 1 << ( int ) L );
		}

		public static int Add ( this PauseGroup L, PauseGroup R ) {
			return L.AsPauseMask().Add( R.AsPauseMask() );
		}

		public static int Add ( this int L, int R ) {
			return L | R;
		}

		public static int Sub ( this int L, PauseGroup R ) {
			return L.Sub( R.AsPauseMask() );
		}

		public static int Sub ( this int L, int R ) {
			return L &= ~R;
		}
	}

	public class PausableWaitForSeconds : CustomYieldInstruction {
		PauseComponent pauser;
		float timeRemaining;

		public PausableWaitForSeconds ( PauseComponent pauser, float waitTime ) {
			this.pauser = pauser;
			this.timeRemaining = waitTime;
		}

		public override bool keepWaiting {
			get {
				if ( pauser.paused ) {
					return true;
				}
				timeRemaining -= Time.deltaTime;
				return timeRemaining > 0;
			}
		}
	}

	public class PausableWaitForFrame : CustomYieldInstruction {
		PauseComponent pauser;

		public PausableWaitForFrame ( PauseComponent pauser ) {
			this.pauser = pauser;
		}
		public override bool keepWaiting {
			get {
				return pauser.paused;
			}
		}
	}

	public class PausableWaitWhile : CustomYieldInstruction {
		PauseComponent pauser;
		Func<bool> waitDelegate;
		bool wait;

		public PausableWaitWhile ( PauseComponent pauser, Func<bool> waitDelegate ) {
			this.pauser = pauser;
			this.waitDelegate = waitDelegate;
			wait = true;
		}

		public override bool keepWaiting {
			get {
				if ( ( wait ) && ( !waitDelegate() ) ) {
					wait = false;
				}

				return wait || pauser.paused;
			}
		}
	}

	public class PausableWaitUntil : CustomYieldInstruction {
		PauseComponent pauser;
		Func<bool> waitDelegate;
		bool wait;

		public PausableWaitUntil ( PauseComponent pauser, Func<bool> waitDelegate ) {
			this.pauser = pauser;
			this.waitDelegate = waitDelegate;
			wait = true;
		}

		public override bool keepWaiting {
			get {
				if ( ( wait ) && ( waitDelegate() ) ) {
					wait = false;
				}

				return wait || pauser.paused;
			}
		}
	}

	/// <summary>
	/// The pause groups for this game.
	/// The number must not exceed 31.
	/// </summary>
	public enum PauseGroup {
		DEFAULT = 0,
		PLAYERS = 1,
		ENEMIES = 2,
		ENVIRONMENT = 3,
		UI = 4
	}
}
