using System.Collections;
using System;

/**
 * A 1st order probabalistic model that can be used to model complex behaviours for A.I. or other processes.
 * 
 * Built for fast lookups, stores states^2 floats.
 * Built for fast Transitions O(log2(states)). Preparing the probabilities is O(n).
 * 
 * After setting the states, don't forget to Prepare().
 * -Eric
 */
public class ProbabilityStateModel {
	public readonly int STATE_COUNT;
	readonly float[][] binarySearchPModel;
	bool validated;

	/**
	 * Create a probability model
	 * UniqueStateCount: Total number of states.
	 * startState: The starting state.
	 * randomPositiveUnitNumberSource: Function that generates a random float in the range [0, ..., 1];
	 */
	public ProbabilityStateModel ( int uniqueStateCount ) {
		STATE_COUNT = uniqueStateCount;
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
	public void Prepare () {
		if ( validated ) {
			throw new Exception( "Model has already been prepared." );
		}

		for ( int i = 0; i < STATE_COUNT; i++ ) {
			WeightedRandom.MakeWeightArraySearchable( binarySearchPModel[ i ] );
		}

		validated = true;
	}

	/**
	 * Perform a probabilistic transition.
	 * Return the new state.
	 */
	public int Transition ( int currentState ) {
		if ( !validated ) {
			throw new Exception( "Please Prepare() model before using." );
		}

		return WeightedRandom.BinarySearchWeightArrayFromSample( binarySearchPModel[ currentState ], UnityEngine.Random.value );
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
}

/**
 * Build a weighted random data structure. Designed for fast sampling.
 */
public class WeightedRandom {
	float[] stateWeights;

	/**
	 * Create the data structure.
	 * stateWeights: the weight of each state.
	 */
	public WeightedRandom ( float[] stateWeights ) {
		this.stateWeights = new float[ stateWeights.Length ];

		for ( int i = 0; i < stateWeights.Length; i++ ) {
			this.stateWeights[ i ] = stateWeights[ i ];
		}

		MakeWeightArraySearchable( this.stateWeights );
	}

	/**
	 * Sample a state and return. Sampling is O( log2(n) ).
	 */
	public int Sample () {
		return BinarySearchWeightArrayFromSample( this.stateWeights, UnityEngine.Random.value );
	}

	/**
	 * Returns The region that a sample exists in. All regions are marked by their upper range.
	 */
	public static int BinarySearchWeightArrayFromSample ( float[] stateWeights, float sample ) {
		int l = 0;
		int h = stateWeights.Length - 1;
		int m = ( h + l ) / 2;

		while ( l <= h ) {
			m = ( l + h ) / 2;
			if ( ( sample <= stateWeights[ m ] ) && ( ( m == 0 ) || ( sample > stateWeights[ m - 1 ] ) ) ) {
				break;
			} else if ( stateWeights[ m ] < sample ) {
				l = m + 1;
			} else {
				h = m - 1;
			}
		}

		if ( stateWeights[ m ] == stateWeights[ 0 ] ) {
			return 0;
		}

		for ( int i = m; i > 0; i-- ) {
			if ( stateWeights[ i ] != stateWeights[ i - 1 ] ) {
				return i;
			}
		}

		return 0;
	}

	public static void MakeWeightArraySearchable ( float[] a ) {
		Normalize( a );

		for ( int i = 1; i < a.Length; i++ ) {
			a[ i ] += a[ i - 1 ];
		}
	}

	public static void Normalize ( float[] a ) {
		float total = 0;

		for ( int i = 0; i < a.Length; i++ ) {
			total += a[ i ];
		}

		if ( total <= 0 ) {
			throw new Exception( "Probabilities out of a given state cannot sum to 0!" );
		}

		for ( int i = 0; i < a.Length; i++ ) {
			a[ i ] /= total;
		}
	}
}
