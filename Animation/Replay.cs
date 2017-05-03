using UnityEngine;
using System.Collections.Generic;

/**
 * Generic Module for storing replay data. Optimized for fast random seeking.
 * Attach it to each gameobject that you want to have replay fucntionality, and have
 * one script implement ReplaySupport.Replayable.
 * -Eric
 */
public class Replay : MonoBehaviour {	

	[ Tooltip( "The script that implements the interface ReplaySupport.Replayable." ) ]
	public MonoBehaviour replayableScript;
	
	[ Tooltip( "The frequency of base frame recording. Lower numbers are better for quickly performing random seeks, but it will also increase memory usage. The default is good for most cases." ) ]
	public int baseFrameTimestepSeconds = 2;
	
	[ Tooltip( "The record limit. Past this point, old replay data will be forgotten." ) ]
	public int historyLimitSeconds = 60;
	
	[ Tooltip( "The amount of frames to skip while recording. If precision is not important, consider raising this a bit to save memory." ) ]
	public int framesToSkip = 0;
	
	[ HideInInspector ]
	public bool paused;
	
	float _accumulatedTime = 0;
	float _playheadTime;
	
	LinkedList< ReplaySupport.ReplayBuffer > _rawHistory;
	LinkedListNode< ReplaySupport.ReplayBuffer >[] _baseFrames;
	
	int _earliestBaseFrameIndex;
	int _latestBaseFrameIndex;
	int _remainingFramesToBeSkipped;
	int HISTORY_BASE_FRAME_INDEX_LIMIT;
	
	void Awake() {
		Validate();
		
		this.HISTORY_BASE_FRAME_INDEX_LIMIT = ( this.historyLimitSeconds / this.baseFrameTimestepSeconds );
		
		this.paused = false;
		this._playheadTime = 0;
		
		this._rawHistory = new LinkedList< ReplaySupport.ReplayBuffer >();
		this._baseFrames = new LinkedListNode< ReplaySupport.ReplayBuffer >[ this.HISTORY_BASE_FRAME_INDEX_LIMIT ];
		
		this._earliestBaseFrameIndex = 0;
		this._latestBaseFrameIndex = -1;
		this._remainingFramesToBeSkipped = 0;
	}
	
	void Validate() {
		if ( ( this.historyLimitSeconds % this.baseFrameTimestepSeconds ) != 0 ) {
			Debug.LogError( "historyLimitSeconds must be a multiple of baseFrameTimestepSeconds. baseFrameTimestepSeconds has been set to 1." );
			this.baseFrameTimestepSeconds = 1;
		}
		
		if ( this.historyLimitSeconds < ( this.baseFrameTimestepSeconds * 2 ) ) {
			Debug.LogError( "historyLimitSeconds must be at least 2 times the length of baseFrameTimestepSeconds. baseFrameTimestepSeconds has been set to the proper number." );
			this.historyLimitSeconds = this.baseFrameTimestepSeconds * 2;
		}
	}
	
	void Start() {
		this._accumulatedTime = this.baseFrameTimestepSeconds;
		
		this.ReplayUpdate( 0 );
	}
	
	void LateUpdate() {
		if ( this.paused ) {
			return;
		}
		
		this.ReplayUpdate( Time.deltaTime );
	}
	
	void ReplayUpdate( float timeDelta ) {
		this._playheadTime += timeDelta;
		this._accumulatedTime += timeDelta;
		
		if ( ( this._remainingFramesToBeSkipped -= 1 ) >= 0 ) {
			return;
		} else {
			this._remainingFramesToBeSkipped = this.framesToSkip;
		}
		
		var recordBuffer = this.CreateRecordBufferForWriting();
		( this.replayableScript as ReplaySupport.Replayable ).RecordReplayFrame( recordBuffer );
		this.AddBufferToTimeline( recordBuffer );
	}
	
	void AddBufferToTimeline( ReplaySupport.ReplayBuffer replayBuffer, bool forceAddbaseFrame = false ) {
		replayBuffer.time = this._playheadTime;
		this._rawHistory.AddLast( replayBuffer );
		
		if ( forceAddbaseFrame || ( this._accumulatedTime >= this.baseFrameTimestepSeconds ) ) {
			this._accumulatedTime -= this.baseFrameTimestepSeconds;
			this.ShiftHistorybaseFrameIndexIfNeeded();
			this._latestBaseFrameIndex = this.WrappedBaseFrameIndex( this._latestBaseFrameIndex + 1 );
			this._baseFrames[ this._latestBaseFrameIndex ] = this._rawHistory.Last;
		}
	}
	
	int WrappedBaseFrameIndex( int index ) {
		return index % this.HISTORY_BASE_FRAME_INDEX_LIMIT;
	}
	
	void ShiftHistorybaseFrameIndexIfNeeded() {
		if ( this._latestBaseFrameIndex == this.WrappedBaseFrameIndex( ( this._earliestBaseFrameIndex + this.HISTORY_BASE_FRAME_INDEX_LIMIT - 1 ) ) ) {
			LinkedListNode< ReplaySupport.ReplayBuffer > nextIndex = this._baseFrames[ this.WrappedBaseFrameIndex( this._earliestBaseFrameIndex + 1 ) ];
			while ( this._rawHistory.First != nextIndex ) {
				this._rawHistory.RemoveFirst();
			}
			this._earliestBaseFrameIndex = this.WrappedBaseFrameIndex( this._earliestBaseFrameIndex + 1 );
		}
	}
	
	public float oldestTime {
		get {
			return this._baseFrames[ this._earliestBaseFrameIndex ].Value.time;
		}
	}
	
	public float playheadTime {
		get {
			return this._playheadTime;
		}
	}
	
	public void SetPlayheadTime( float time ) {
		if ( time < this.oldestTime || time > this.playheadTime) {
			throw new UnityException( "New Playhead time must be within recording range" );
		}
		
		this._playheadTime = time;
		this._latestBaseFrameIndex = this.GetFlooredBaseFrameIndex( time );
		
		LinkedListNode< ReplaySupport.ReplayBuffer > current = this._rawHistory.Last;
		while ( current.Value.time > time ) {
			var temp = current;
			current = current.Previous;
			this._rawHistory.Remove( temp );
		}
		
		if ( time > 0 ) {
			this._accumulatedTime = time - this.GetFlooredBaseFrame( time ).Value.time;
		} else {
			this._accumulatedTime = this.baseFrameTimestepSeconds;
		}
		
		ReplayUpdate ( 0 );
	}
	
	int GetFlooredBaseFrameIndex( float time ) {
		return this.WrappedBaseFrameIndex( this._earliestBaseFrameIndex + Mathf.FloorToInt( ( time - oldestTime ) / (float) this.baseFrameTimestepSeconds) );
	}
	
	int GetCielingBaseFrameIndex( float time ) {
		return this.WrappedBaseFrameIndex( this._earliestBaseFrameIndex + Mathf.CeilToInt( ( time - oldestTime ) / (float) this.baseFrameTimestepSeconds) );
	}
	
	LinkedListNode< ReplaySupport.ReplayBuffer > GetFlooredBaseFrame(float time) {
		return this._baseFrames[ this.GetFlooredBaseFrameIndex( time ) ];
	}
	
	LinkedListNode< ReplaySupport.ReplayBuffer > GetCielingBaseFrame(float time) {
		return this._baseFrames[ this.GetCielingBaseFrameIndex( time ) ];
	}
	
	ReplaySupport.ReplayBuffer CreateRecordBufferForWriting() {
		var buffer = new ReplaySupport.ReplayBuffer();
		return buffer._InitWrite( ( this.replayableScript as ReplaySupport.Replayable ).GetFieldCountPerFrame() );;
	}
	
	public float GetBlendedFloat( float time, int indexInBuffer) {
		if ( time <= this.oldestTime ) {
			return this._rawHistory.First.Value.frameDataRaw[ indexInBuffer ].f;
		}
		
		if ( time >= this.playheadTime ) {
			return this._rawHistory.Last.Value.frameDataRaw[ indexInBuffer ].f;
		}
		
		LinkedListNode< ReplaySupport.ReplayBuffer > current = this.GetFlooredBaseFrame( time );
		while ( ( current.Value.time < time ) && ( current.Next != null ) ) {
			current = current.Next;
		}
		
		if ( current.Value.time >  time ) {
			current = current.Previous;
		}
		
		if ( current.Next == null ) {
			return current.Value.frameDataRaw[ indexInBuffer ].f;
		}
		
		float v1 = current.Value.frameDataRaw[ indexInBuffer ].f;
		float v2 = current.Next.Value.frameDataRaw[ indexInBuffer ].f;
		float t = ( time - current.Value.time ) / ( current.Next.Value.time - current.Value.time );
		return Mathf.Lerp( v1, v2, t );
	}
	
	public ReplaySupport.ReplayBuffer GetNearestReplayFrame( float time ) {
		if ( time <= this.oldestTime ) {
			return this._rawHistory.First.Value._InitRead();
		}
		
		if ( time >= this.playheadTime ) {
			return this._rawHistory.Last.Value._InitRead();
		}
		
		LinkedListNode< ReplaySupport.ReplayBuffer > current = this.GetFlooredBaseFrame( time );
		while ( ( current.Value.time < time ) && ( current.Next != null ) ) {
			current = current.Next;
		}
		
		if ( current.Value.time >  time ) {
			current = current.Previous;
		}
		
		if ( current.Next == null ) {
			current.Value._InitRead();
			return current.Value._InitRead();
		}
		
		if ( ( time - current.Value.time ) < ( current.Next.Value.time - time ) ) {
			return current.Value._InitRead();
		} else {
			return current.Next.Value._InitRead();
		}
	}
}

namespace ReplaySupport {

	public interface Replayable {
		int GetFieldCountPerFrame();
		void RecordReplayFrame( ReplaySupport.ReplayBuffer buffer );
	}
	
	public class ReplayBuffer {
	
		ReplaySupport.ReplayFrame[] _frameData;
		int _currIndex;
		
		public float time;
		
		public ReplaySupport.ReplayBuffer _InitWrite( int dataLength ) {
			this._frameData = new ReplayFrame[ dataLength ];
			this._currIndex = 0;
			return this;
		}
		
		public ReplaySupport.ReplayBuffer _InitRead() {
			this._currIndex = 0;
			return this;
		}
		
		public ReplayBuffer WriteInt( int i ) {
			this._frameData[ this._currIndex++ ] = new ReplayFrame(){ i = i };
			return this;
		}
		
		public ReplayBuffer WriteFloat( float f ) {
			this._frameData[ this._currIndex++ ] = new ReplayFrame(){ f = f };
			return this;
		}
		
		public ReplayBuffer WriteBool( bool b ) {
			this.WriteInt( b ? 1 : 0 );
			return this;
		}
		
		public int ReadInt() {
			return this._frameData[ this._currIndex++ ].i;
		}
		
		public float ReadFloat() {
			return this._frameData[ this._currIndex++ ].f;
		}
		
		public bool ReadBool() {
			return this.ReadInt() > 0;
		}
		
		public ReplaySupport.ReplayFrame[] frameDataRaw {
			get {
				return this._frameData;
			}
		}
		
		public ReplaySupport.ReplayFrame this[ int frameIndex ] {
			get {
				return this._frameData[ frameIndex ];
			}
		}
	}
	
	public struct ReplayFrame {
	
		public float f;
		public int i;
	}
}
