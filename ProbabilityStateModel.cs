using System.Collections;
using System;

/**
 * A 1st order probabalistic model that can be used to model complex behaviours for A.I. or other processes.
 * 
 * Built for fast lookups, will store a pair of int/float entries for (states)^2.
 * Built for fast Transitions O(log2(states)). Preparing the probabilities is O(n).
 * 
 * After setting the states, don't forget to Prepare().
 * -Eric
 */
public class ProbabilityStateModel {
	public int currentState;

	readonly float[][] binarySearchPModel;
	bool validated;
	readonly int STATE_COUNT;
	readonly System.Func<float> randomNumberSource;

	/**
	 * Create a probability model
	 * UniqueStateCount: Total number of states.
	 * startState: The starting state.
	 * randomPositiveUnitNumberSource: Function that generates a random float in the range [0, ..., 1];
	 */
	public ProbabilityStateModel( int uniqueStateCount, int startState, System.Func<float> randomPositiveUnitNumberSource) {
		currentState = startState;
		STATE_COUNT = uniqueStateCount;
		randomNumberSource = randomPositiveUnitNumberSource;
		validated = false;

		binarySearchPModel = new float[ STATE_COUNT ][];
		for ( int i = 0; i < STATE_COUNT; i++ ) {
			binarySearchPModel[ i ] = new float[ STATE_COUNT ];
		}
	}

	/**
	 * Convert the model into a binary-searchable model for fast lookups.
	 * Running this will allow Transitions.
	 */
	public void Prepare() {
		if ( validated ) {
			throw new Exception( "Model has already been prepared." );
		}

		for ( int i = 0; i < STATE_COUNT; i++ ) {
			MakeRegionArraySearchable( binarySearchPModel[ i ] );
		}

		validated = true;
	}

	/**
	 * Perform a probabilistic transition.
	 * Return true if the state changed.
	 */
	public bool Transition() {
		if ( !validated ) {
			throw new Exception( "Please Prepare() model before using." );
		}

		var newState = BinarySearchRegionFromSample( binarySearchPModel[ currentState ], randomNumberSource() );

		bool isNewState = ( newState != currentState );
		currentState = newState;

		return isNewState;
	}

	/**
	 * Transition and return the current state.
	 */
	public int TransitionReturnState() {
		Transition();
		return currentState;
	}

	/**
	 * Set the transition probability. Cannot be negative.
	 * Values will be normalized later, so the value scale is not important.
	 */
	public float this[ int i, int j ] {
		set {
			if ( validated ) {
				throw new Exception( "Cannot set probabilities after calling Prepare()." );
			}

			if ( value < 0 ) {
				throw new Exception( "Probability cannot be negative." );
			}

			binarySearchPModel[ i ][ j ] = value;
		}
		get {
			if ( j == 0 ) {
				return binarySearchPModel[ i ][ j ];
			}

			return binarySearchPModel[ i ][ j ] - binarySearchPModel[ i ][ j - 1 ];
		}
	}

	/**
	 * Returns The region that a sample exists in. All regions are marked by their upper range.
	 */
	static int BinarySearchRegionFromSample( float[] A, float sample ) {
		int l = 0;
		int h = A.Length - 1;
		int m = ( h + l ) / 2;

		while ( l <= h ) {
			m = ( l + h ) / 2;
			if ( ( sample <= A[ m ] ) && ( ( m == 0 ) || ( sample > A[ m - 1 ] ) ) ) {
				break;
			} else if ( A[ m ] < sample ) {
				l = m + 1;
			} else {
				h = m - 1;
			}
		}

		if ( A[m] == A[0] ) {
			return 0;
		}

		for ( int i = m; i > 0; i-- ) {
			if ( A[i] != A[i - 1] ) {
				return i;
			}
		}

		return 0;
	}

	static void MakeRegionArraySearchable( float[] a ) {
		Normalize( a );

		for ( int i = 1; i < a.Length; i++ ) {
			a[ i ] += a[ i - 1 ];
		}
	}

	static void Normalize( float[] a ) {
		float total = 0;

		for ( int i = 0; i < a.Length; i++ ) {
			total += a[ i ];
		}

		if ( total <= 0 ) {
			throw new Exception( "Probabilities out of a given state cannot sum to 0!" );
		}

		for ( int i = 0; i < a.Length; i++ ) {
			a [ i ] /= total;
		}
	}
}
