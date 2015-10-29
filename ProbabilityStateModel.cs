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

	readonly WeightedRegion[][] binarySearchPModel;
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

		binarySearchPModel = new WeightedRegion[ uniqueStateCount ][];
		for ( int i = 0; i < STATE_COUNT; i++ ) {
			binarySearchPModel[ i ] = new WeightedRegion[ uniqueStateCount ];
		}

		for ( int i = 0; i < STATE_COUNT; i++ ) {
			for ( int j = 0; j < STATE_COUNT; j++ ) {
				SetTransitionProbability( i, j, 0 );
			}
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
	public void SetTransitionProbability( int fromState, int toState, float prob ) {
		if ( validated ) {
			throw new Exception ( "Cannot set probabilityies after calling Prepare()." );
		}

		if ( prob < 0 ) {
			throw new Exception ( "Probability cannot be negative." );
		}


		binarySearchPModel[ fromState ][ toState ] = new WeightedRegion () {
			upperRegionValue = prob,
			trueIndex = toState
		};
	}

	/**
	 * Returns The region that a sample exists in. All regions are marked by their upper range.
	 */
	static int BinarySearchRegionFromSample( WeightedRegion[] sortedA, float sample ) {
		int l = 0;
		int h = sortedA.Length - 1;
		int m = ( l + h ) / 2;

		if ( l == h ) {
			return m;
		}

		while ( l <= h ) {
			if ( sample < sortedA[ m ].upperRegionValue ) {
				if ( ( m == 0 ) || ( (sortedA[ m ].upperRegionValue - sample ) <= ( sortedA[ m ].upperRegionValue - sortedA[ m - 1 ].upperRegionValue) ) ) {
					return sortedA[ m ].trueIndex;
				}

				h = m - 1;
			} else {
				if ( m == sortedA.Length - 1 ) {
					return sortedA[ m ].trueIndex;
				}

				l = m + 1;
			}

			m = ( l + h ) / 2;
		}

		return -1;
	}

	static void MakeRegionArraySearchable( WeightedRegion[] a ) {
		Normalize( a );

		for ( int i = 1; i < a.Length; i++ ) {
			a[ i ].upperRegionValue += a[ i - 1 ].upperRegionValue;
		}

		int lastValidIndex = 0;

		for ( int i = a.Length - 1; i > 0; i-- ) {
			if ( a[i].upperRegionValue != a [ i - 1 ].upperRegionValue ) {
				lastValidIndex = i;
				break;
			}
		}

		for ( int i = lastValidIndex + 1; i < a.Length; i++ ) {
			a[ i ].trueIndex = lastValidIndex;
		}

		for ( int i = lastValidIndex - 1; i > 0; i-- ) {
			if ( ( a[ i ].upperRegionValue == a[ i - 1 ].upperRegionValue ) || ( a[ i ].upperRegionValue != a[ i + 1 ].upperRegionValue ) ) {
				a[ i ].trueIndex = a[ i + 1 ].trueIndex;
			}
		}
	}

	static void Normalize( WeightedRegion[] A ) {
		float total = 0;

		for ( int i = 0; i < A.Length; i++ ) {
			total += A[ i ].upperRegionValue;
		}

		if ( total <= 0 ) {
			throw new Exception( "Probabilities out of a given state cannot sum to 0!" );
		}

		for ( int i = 0; i < A.Length; i++ ) {
			A [ i ].upperRegionValue /= total;
		}
	}

	public float GetTransitionProbability( int fromState, int toState ) {
		if ( toState == 0 ) {
			return binarySearchPModel[ fromState ][ toState ].upperRegionValue;
		}

		return binarySearchPModel[ fromState ] [ toState ].upperRegionValue - binarySearchPModel[ fromState ][ toState - 1 ].upperRegionValue;
	}

	struct WeightedRegion {
		public int trueIndex;
		public float upperRegionValue;
	}
}
