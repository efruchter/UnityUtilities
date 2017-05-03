using UnityEngine;
using System.Collections.Generic;
using System;

/**
 * Version 2.2
 * -Eric
 *
 * Advanced sound loader with some basic features that are useful in common scenarios.
 * When asked to play a sound, the method will return a SoundInterface, which you can use to customize your sound.
 *
 * Several methods have been added that modify the output of PlaySoundGeneric(string) to accomodate common use cases.
 * You can find examples below.
 *
 * Common Use-Cases:
 * ==========
 *	// Set the directory sound files are pulled from.
 *	SoundManager.configuration.directory = "Sound/";
 *
 *	// Load a bg tune and fade it in when it's done loading.
 *	SoundManager.PreLoadAsync( "bg1", () => SoundManager.PlayBG( "bg1" ).FadeIn() );
 *
 *	// Play a 2D SFX.
 *	SoundManager.PlaySFX( "SFX/birdJingle" );
 *
 *	// Play a 3D SFX at a transform's location. It will track.
 *	SoundManager.PlaySFX( "SFX/bird_call", birdTransform );
 *
 *	// Set the volume of a sound channel.
 *	SoundManager.SetVolume( SoundChannel.SFX, 0.1f );
 *
 *	// Load a BG tune. When it's done loading, fade out the other BG's and fade in the new one.
 *	SoundManager.PreLoadAsync( "bg2", () => {
 *		SoundManager.FadeOutStopSoundsByChannel( SoundChannel.BG );
 *		SoundManager.PlayBG( "bg2" ).FadeIn();
 *	} );
 *
 *	// Play a VO channel sound, and store an interface to the playing sound.
 *	var vo = SoundManager.PlayVO( "vo1" );
 *
 *	// Add an action to play after the VO is done playing.
 *	vo.AddOnCompletedAction( () => { Debug.Log( "VO is done!" ); } );
 *
 * 	// Pause the VO.
 *	vo.Pause();
 *	vo.UnPause();
 *
 * 	// Fade Out (but not kill) the vo. Then fade it in after 3 seconds.
 *	vo.FadeOut();
 *	wait( 3 );
 *	vo.FadeIn();
 *
 *	// Play a loopingsound and store a nullable ref to it.
 *	SoundInterface? walkSound = SoundManager.PlaySFXLoop( "walking" ).FadeIn();
 *
 *	// Stop the walking sound and drop the reference.
 *	walkSound.Value.Stop();
 *	walkSound = null;
 *
 *	// Pause all the sounds playing on the TEST channel.
 *	SoundManager.SetPausedByChannel( SoundChannel.TEST, false );
 *
 *	// LINQ query to get all BGs and fade them out.
 *	foreach ( var sound in SoundManager.ActiveSounds().Where( sound => sound.channel == SoundChannel.BG ) ) {
 *		sound.FadeOut();
 *	}
 *
 *	// Use a filter instead of a LINQ query, and fade in the BGs.
 *	foreach ( var sound in SoundManager.ActiveSounds( sound => sound.channel == SoundChannel.BG ) ) {
 *		sound.FadeIn();
 *	}
 *
 *	// Stop all sounds that are marked non-persistent. BG's are persistent by default.
 *	SoundManager.StopTransientSounds();
 *
 *	// Unload all unused clips. Generates garbage so call it during a loading screen and don't forget to collect.
 *	SoundManager.UnloadAllInactiveClips();
 *
 *	// Wipe out the inactive sound pool. Generates garbage so call it during a loading screen and don't forget to collect.
 *	SoundManager.ClearInactiveSoundPool();
 *
 *	// Change the pitch of a sound, then revert the pooled audiosource to normal afterward
 *	// This is the general way to make changes to sounds that are unusual or not supported by the SoundInterface.
 *	// With great power comes great responsibility!
 * 
 * 	var tweet = SoundManager.PlaySFX( "sfx1" );
 * 	tweet._audioSource.pitch = 0.5f;
 * 	tweet.AddOnCompletedAction( () => {
 * 		tweet._audioSource.pitch = 1;
 * 	} );
 *
 */
public class SoundManager : MonoBehaviour {
	#region static

	static SoundManager I;

	public static readonly SoundManagerConfiguration configuration = new SoundManagerConfiguration();

	static void TryCreate () {
		if ( I != null ) {
			return;
		}

		GameObject hostGameobject = new GameObject( "Sound Manager" );
		DontDestroyOnLoad( hostGameobject );

		I = hostGameobject.AddComponent<SoundManager>();
	}

	/**
	 * Unload all clips that are no longer being used.
	 * If not immediate, happens in the LateUpdate cycle following invocation. [recommended]
	 */
	public static void UnloadAllInactiveClips ( bool immediate = false ) {
		TryCreate();

		if ( !immediate ) {
			I.unloadInactiveNextCycle = true;
			return;
		}

		LinkedList<string> keysToKill = new LinkedList<string>();
		HashSet<string> liveKeys = new HashSet<string>();

		foreach ( var key in I.activePool ) {
			liveKeys.Add( key.soundName );
		}

		foreach ( var clipKey in I.clipCache.Keys ) {
			if ( !liveKeys.Contains( clipKey ) ) {
				keysToKill.AddLast( clipKey );
			}
		}

		foreach ( var soundName in keysToKill ) {
			Resources.UnloadAsset( I.clipCache[ soundName ] );
			I.clipCache.Remove( soundName );
		}
	}

	/**
	 * Wipe the object pool of empty audiosources.
	 * If not immediate, happens in the LateUpdate cycle following invocation. [recommended]
	 */
	public static void ClearInactiveSoundPool ( bool immediate = false ) {
		TryCreate();

		if ( !immediate ) {
			I.wipeInactivePoolNextCycle = true;
			return;
		}

		foreach ( var sound in I.inactivePool ) {
			Destroy( sound.audioSource.gameObject );
		}

		I.inactivePool.Clear();
	}

	/**
	 * Destroy the manager and unload all clips.
	 */
	public static void Destroy () {
		if ( I == null ) {
			return;
		}

		foreach ( var sound in I.activePool ) {
			Destroy( sound.audioSource.gameObject );
		}

		ClearInactiveSoundPool();

		foreach ( var clip in I.clipCache.Values ) {
			Resources.UnloadAsset( clip );
		}

		Destroy( I.gameObject );

		I = null;
	}

	/**
	 * Unload an AudioClip from memory and remove it from the cache.
	 */
	public static void UnloadClip ( string soundName ) {
		TryCreate();

		AudioClip clip;
		if ( I.clipCache.TryGetValue( soundName, out clip ) ) {
			I.clipCache.Remove( soundName );
			Resources.UnloadAsset( clip );
		}
	}

	/**
	 * Load a clip into the memory cache.
	 */
	public static void PreLoad ( string soundName ) {
		PreLoad( soundName, false, null );
	}

	/**
	 * Load a clip into the memory cache async.
	 */
	public static void PreLoadAsync ( string soundName ) {
		PreLoadAsync( soundName, null );
	}

	/**
	 * Load a clip into the memory cache async and perform an action on completion.
	 */
	public static void PreLoadAsync ( string soundName, Action onComplete ) {
		PreLoad( soundName, true, onComplete );
	}

	static void PreLoad ( string soundName, bool async, Action onComplete ) {
		TryCreate();

		if ( I.clipCache.ContainsKey( soundName ) ) {
			if ( onComplete != null ) {
				onComplete();
			}

			return;
		}

		if ( async ) {
			I.StartCoroutine( PreLoadRoutineAsync( soundName, onComplete ) );
			return;
		}

		I.clipCache[ soundName ] = Resources.Load<AudioClip>( configuration.directory + soundName );

		if ( onComplete != null ) {
			onComplete();
		}
	}

	static System.Collections.IEnumerator PreLoadRoutineAsync ( string soundName, Action onComplete ) {
		var request = Resources.LoadAsync<AudioClip>( configuration.directory + soundName );

		yield return request;

		I.clipCache[ soundName ] = ( AudioClip ) request.asset;

		if ( onComplete != null ) {
			onComplete();
		}
	}

	/**
	 * Get the volume of a channel.
	 */
	public static float GetVolume ( SoundChannel channel ) {
		TryCreate();

		return I.channelVolumes[ ( int ) channel ];
	}

	/**
	 * Set the volume of a channel.
	 */
	public static void SetVolume ( SoundChannel channel, float volume ) {
		TryCreate();

		I.channelVolumes[ ( int ) channel ] = volume;
	}

	/**
	 * Set pause state of one-shot player.
	 */
	public static void SetPausedOneShotPlayer ( bool paused ) {
		TryCreate();

		if ( paused ) {
			I.oneShotPlayer.Pause();
		} else {
			I.oneShotPlayer.UnPause();
		}
	}

	/**
	 * Set pause state of a certain channel.
	 */
	public static void SetPausedByChannel ( SoundChannel channel, bool paused ) {
		TryCreate();

		foreach ( var sound in ActiveSounds( sound => sound.channel == channel ) ) {
			if ( paused ) {
				sound.Pause();
			} else {
				sound.UnPause();
			}
		}
	}

	/**
	 * Hard-Stop sounds on a given channel.
	 */
	public static void StopSoundsByChannel ( SoundChannel channel ) {
		TryCreate();

		foreach ( var sound in ActiveSounds( sound => sound.channel == channel ) ) {
			sound.Stop();
		}
	}

	/**
	 * Fade-Stop sounds on a given channel.
	 */
	public static void FadeOutStopSoundsByChannel ( SoundChannel channel, float fadeSpeed = 0.5f ) {
		TryCreate();

		foreach ( var sound in ActiveSounds( sound => sound.channel == channel ) ) {
			sound.FadeOutStop( fadeSpeed );
		}
	}

	/**
	 * Hard-Stop all non-persistent sounds.
	 */
	public static void StopTransientSounds () {
		TryCreate();

		foreach ( var sound in TransientSounds() ) {
			sound.Stop();
		}
	}

	/**
	 * Fade-Stop all non-persistent sounds.
	 */
	public static void FadeOutStopTransientSounds ( float fadeSpeed = 0.5f ) {
		TryCreate();

		foreach ( var sound in TransientSounds() ) {
			sound.FadeOutStop( fadeSpeed );
		}
	}

	/**
	 * Retrieve active sounds by name.
	 */
	public static IEnumerable<SoundInterface> GetActiveSoundsByName ( string name ) {
		TryCreate();

		return ActiveSounds( sound => sound.name == name );
	}

	/**
	 * Play a 2D one-shot. The cheapest way to play a sound.
	 */
	public static void PlayOneShot ( String soundName, float volume ) {
		TryCreate();

		if ( !I.clipCache.ContainsKey( soundName ) ) {
			PreLoad( soundName );
		}

		I.oneShotPlayer.PlayOneShot( I.clipCache[ soundName ], volume );
	}

	/**
	 * Play a 2D one-shot. The cheapest way to play a sound.
	 */
	public static void PlayOneShot ( AudioClip clip, float volume ) {
		TryCreate();

		I.oneShotPlayer.PlayOneShot( clip, volume );
	}

	/**
	 * The method that builds a sound object and plays it. It is recommended that you avoid using this method directly,
	 * but it can still be useful. The functions for controlling the sound can be found in the SoundInterface that is returned.
	 */
	public static SoundInterface PlaySoundGeneric ( string soundName, SoundChannel channel ) {
		TryCreate();

		if ( !I.clipCache.ContainsKey( soundName ) ) {
			PreLoad( soundName );
		}

		SoundContainer container = null;

		if ( I.inactivePool.Count > 0 ) {
			container = I.inactivePool.First.Value;
			I.inactivePool.RemoveFirst();
		} else {
			container = BuildSoundContainer();
		}

		container.channel = channel;
		container.soundName = soundName;
		container.state = SoundMetaState.FADE_IN_ALIVE;
		container.track = null;
		container.persistent = false;
		container.fadeScalar = 0;
		container.paused = false;
		container.overrideVolume = false;
		container.onCompleted = null;

		container.audioSource.enabled = true;
		container.audioSource.volume = GetVolume( channel );
		container.audioSource.loop = false;
		container.audioSource.clip = I.clipCache[ soundName ];
		container.audioSource.spatialBlend = 0;
		container.audioSource.UnPause();
		container.audioSource.Play();

		container.audioSource.gameObject.name = soundName;

		I.activePool.AddLast( container );

		return new SoundInterface( container );
	}

	/**
	 * Get a list of non-persistent sounds.
	 */
	public static IEnumerable<SoundInterface> TransientSounds () {
		return ActiveSounds( si => !si.persistent );
	}

	/**
	 * Get a list of Persistent sounds, such as BG.
	 */
	public static IEnumerable<SoundInterface> PersistentSounds () {
		return ActiveSounds( si => si.persistent );
	}

	/**
	 * Get a list of active sounds.
	 */
	public static IEnumerable<SoundInterface> ActiveSounds () {
		return ActiveSounds( si => true );
	}

	/**
	 * Get a list of active sounds that pass a filter.
	 */
	public static IEnumerable<SoundInterface> ActiveSounds ( Func<SoundInterface, bool> filter ) {
		TryCreate();

		if ( I.activePool.Count > 0 ) {
			var current = I.activePool.First;
			do {
				var si = new SoundInterface( current.Value );
				if ( filter( si ) ) {
					yield return si;
				}
				current = current.Next;
			} while ( current != null );
		}
	}

	#region creation_methods

	/**
	 * Play a 2D SFX. Transient.
	 */
	public static SoundInterface PlaySFX ( string soundName ) {
		var sound = PlaySoundGeneric( soundName, SoundChannel.SFX );

		sound.looping = false;

		return sound;
	}

	/**
	 * Play a 3D SFX. Transient.
	 */
	public static SoundInterface PlaySFX ( string soundName, Transform location ) {
		var sound = PlaySFX( soundName );

		sound.location = location;
		sound.spatialBlend = 1;

		return sound;
	}

	/**
	 * Play a Looping 2D SFX. Transient.
	 */
	public static SoundInterface PlaySFXLoop ( string soundName ) {
		var sound = PlaySFX( soundName );

		sound.looping = true;

		return sound;
	}

	/**
	 * Play a looping 3D SFX. Transient.
	 */
	public static SoundInterface PlaySFXLoop ( string soundName, Transform location ) {
		var sound = PlaySFXLoop( soundName );

		sound.location = location;

		return sound;
	}

	/**
	 * Play a persistent background music.
	 */
	public static SoundInterface PlayBG ( string soundName ) {
		var sound = PlaySoundGeneric( soundName, SoundChannel.BG );

		sound.persistent = true;
		sound.looping = true;

		return sound;
	}

	/**
	 * Play 2D VO. Transient.
	 */
	public static SoundInterface PlayVO ( string soundName ) {
		var sound = PlaySoundGeneric( soundName, SoundChannel.VOICE );

		return sound;
	}

	/**
	 * Play 2D test sound.
	 */
	public static SoundInterface PlayTest ( string soundName, float volume ) {
		var sound = PlaySoundGeneric( soundName, SoundChannel.TEST );

		sound.volumeOverride = volume;

		return sound;
	}

	#endregion creation_methods

	static SoundContainer BuildSoundContainer () {
		GameObject g = new GameObject();
		SoundContainer container = new SoundContainer();
		container.audioSource = g.AddComponent<AudioSource>();
		g.transform.parent = I.transform;
		return container;
	}

	#endregion static

	AudioSource oneShotPlayer;
	Dictionary<string, AudioClip> clipCache;
	LinkedList<SoundContainer> activePool, inactivePool;
	float[] channelVolumes;
	bool unloadInactiveNextCycle, wipeInactivePoolNextCycle;

	void Awake () {
		I = this;

		ConfigureOneShotPlayer();

		clipCache = new Dictionary<string, AudioClip>();
		activePool = new LinkedList<SoundContainer>();
		inactivePool = new LinkedList<SoundContainer>();
		unloadInactiveNextCycle = false;
		wipeInactivePoolNextCycle = false;

		channelVolumes = new float[ Enum.GetNames( typeof( SoundChannel ) ).Length ];
		for ( int i = 0; i < channelVolumes.Length; i++ ) {
			channelVolumes[ i ] = 1;
		}
	}

	void LateUpdate () {
		if ( activePool.Count != 0 ) {
			var current = activePool.First;
			do {
				current.Value.Update();
				var tempCurrent = current;
				current = current.Next;

				if ( tempCurrent.Value.canBePooled ) {
					tempCurrent.Value.PrepareForPool();
					inactivePool.AddLast( tempCurrent.Value );

					I.activePool.Remove( tempCurrent );
				}

			} while ( current != null );
		}

		if ( I.unloadInactiveNextCycle ) {
			I.unloadInactiveNextCycle = false;
			UnloadAllInactiveClips( true );
		}

		if ( I.wipeInactivePoolNextCycle ) {
			I.wipeInactivePoolNextCycle = false;
			ClearInactiveSoundPool( true );
		}
	}

	void ConfigureOneShotPlayer () {
		oneShotPlayer = I.gameObject.AddComponent<AudioSource>();
		oneShotPlayer.dopplerLevel = 0;
		oneShotPlayer.spatialBlend = 0;
	}

	void OnLevelWasLoaded ( int level ) {
		if ( configuration.stopTransientsOnLevelLoad ) {
			StopTransientSounds();
		}
	}
}

/**
 * Customize a playing sound.
 */
public struct SoundInterface {
	readonly SoundContainer container;

	public SoundInterface ( SoundContainer container ) {
		this.container = container;
	}

	public string name {
		get {
			return container.soundName;
		}
	}

	/**
	 * Stop the sound.
	 */
	public void Stop () {
		if ( container.audioSource == null ) {
			return;
		}

		container.state = SoundMetaState.FADE_OUT_DEAD;
		container.audioSource.volume = 0;
		container.persistent = false;
		container.audioSource.Stop();
	}

	/**
	 * Fade-Stop the sound.
	 */
	public void FadeOutStop ( float fadeOutSpeed = 0.5f ) {
		container.state = SoundMetaState.FADE_OUT_DEAD;
		container.fadeScalar = Mathf.Max( 0, fadeOutSpeed );
		container.persistent = false;
	}

	/**
	 * Fade the sound to 0 volume.
	 */
	public SoundInterface FadeOut ( float fadeOutSpeed = 0.5f ) {
		container.state = SoundMetaState.FADE_OUT_ALIVE;
		container.fadeScalar = Mathf.Max( 0, fadeOutSpeed );
		return this;
	}

	/**
	 * Fade the sound to channel volume.
	 */
	public SoundInterface FadeIn ( float fadeInSpeed = 0.5f ) {
		if ( container.audioSource == null ) {
			return this;
		}

		container.audioSource.volume = 0;
		container.state = SoundMetaState.FADE_IN_ALIVE;
		container.fadeScalar = Mathf.Max( 0, fadeInSpeed );
		return this;
	}

	/**
	 * Mark the sound as persistent (true) or transient (false).
	 */
	public bool persistent {
		get {
			return container.persistent;
		}
		set {
			container.persistent = value;
		}
	}

	/**
	 * Does the sound loop?
	 */
	public bool looping {
		get {
			if ( container.audioSource == null ) {
				return false;
			}

			return container.audioSource.loop;
		}
		set {
			if ( container.audioSource == null ) {
				return;
			}

			container.audioSource.loop = value;
		}
	}

	/**
	 * Set the sound to 3D and place it at a location.
	 */
	public Transform location {
		get {
			return container.track;
		}
		set {
			container.track = value;
		}
	}

	/**
	 * Set the spatial blend of a 3D sound.
	 */
	public float spatialBlend {
		set {
			if ( container.audioSource == null ) {
				return;
			}

			container.audioSource.spatialBlend = value;
		}
		get {
			if ( container.audioSource == null ) {
				return 0;
			}

			return container.audioSource.spatialBlend;
		}
	}

	/**
	 * Pause the sound.
	 */
	public SoundInterface Pause () {
		if ( container.audioSource == null ) {
			return this;
		}

		container.audioSource.Pause();
		container.paused = true;
		return this;
	}

	/**
	 * UnPause the sound.
	 */
	public SoundInterface UnPause () {
		if ( container.audioSource == null ) {
			return this;
		}

		container.audioSource.UnPause();
		container.paused = false;
		return this;
	}

	/**
	 * Override channel volume.
	 */
	public float volumeOverride {
		set {
			container.overrideVolume = true;
			container.volumeOverride = value;
		}
	}

	/**
	 * Set the volume to channel volume.
	 */
	public SoundInterface HaltVolumeOverride () {
		container.overrideVolume = false;
		return this;
	}

	public SoundChannel channel {
		get {
			return container.channel;
		}
	}

	/**
	 * Add an action to occur when the sound is done playing.
	 */
	public SoundInterface AddOnCompletedAction ( Action action ) {
		container.onCompleted += action;
		return this;
	}

	public AudioSource _audioSource {
		get {
			return container.audioSource;
		}
	}
}

public class SoundContainer {
	public SoundChannel channel;
	public AudioSource audioSource;
	public string soundName;
	public SoundMetaState state;
	public Transform track;
	public bool persistent;
	public float fadeScalar;
	public bool paused;
	public float volumeOverride;
	public bool overrideVolume;
	public Action onCompleted;

	public void Update () {
		if ( track != null ) {
			audioSource.gameObject.transform.position = track.position;
		}

		switch ( state ) {
			case SoundMetaState.FADE_IN_ALIVE:
				{
					if ( overrideVolume ) {
						audioSource.volume = volumeOverride;
					} else {
						if ( fadeScalar != 0 ) {
							audioSource.volume = Mathf.MoveTowards( audioSource.volume, SoundManager.GetVolume( channel ), Time.deltaTime * fadeScalar );
						} else {
							audioSource.volume = SoundManager.GetVolume( channel );
						}
					}
					break;
				}
			case SoundMetaState.FADE_OUT_DEAD:
			case SoundMetaState.FADE_OUT_ALIVE:
				{
					if ( fadeScalar != 0 ) {
						audioSource.volume = Mathf.MoveTowards( audioSource.volume, 0, Time.deltaTime * fadeScalar );
					} else {
						audioSource.volume = 0;
					}

					break;
				}
			default:
				{
					break;
				}
		}

		if ( ( !audioSource.isPlaying ) && ( !paused ) ) {
			state = SoundMetaState.FADE_OUT_DEAD;
		}
	}

	public bool canBePooled {
		get {
			return ( state == SoundMetaState.FADE_OUT_DEAD ) && ( audioSource.volume == 0 );
		}
	}

	public void PrepareForPool () {
		audioSource.Stop();

		audioSource.enabled = false;
		audioSource.clip = null;
		audioSource.gameObject.name = "[ POOLED ]";

		if ( onCompleted != null ) {
			onCompleted();
		}
	}
}

public enum SoundChannel {
	SFX, BG, VOICE, TEST
}

public enum SoundMetaState {
	FADE_IN_ALIVE, FADE_OUT_ALIVE, FADE_OUT_DEAD
}

public class SoundManagerConfiguration {

	/**
	 * The directory to be prefixed to any soundName.
	 * A common choice is "Sound/"
	 */
	public string directory = "";

	/**
	 * Hard-Stop any transient sounds when a new scene is loaded.
	 */
	public bool stopTransientsOnLevelLoad = false;
}
