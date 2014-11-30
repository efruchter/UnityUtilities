using UnityEngine;
using System.Collections;

/**
 * A way to reset animations on TrailRenderers when moving them suddenly.
 * Normally a trail will streak across the space it was teleported. This script gives
 * it a single frame to reset, and then applies the proper trail time again.
 *
 * Usage (from within a script):
 * 
 * trailRenderer.Reset(trailTime, this);
 *
 * -Eric 
 */
public static class TrainRendererExtensions
{
	public static void Reset(this TrailRenderer trail, float time, MonoBehaviour instance)
	{
		instance.StartCoroutine(ResetTrail(trail, time));
	}

	static IEnumerator ResetTrail(TrailRenderer trail, float time)
	{
		trail.time = 0;
		yield return 0;
		trail.time = time;
	}
}
