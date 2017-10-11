using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Assertions;

namespace kazoo
{
	public static class TweenRoutines
	{
		public static void TweenFloat(this MonoBehaviour behaviour, float startValue, float endValue, float time, Action<float> onUpdate)
		{
			if (onUpdate == null)
			{
				return;
			}

			if (time <= 0)
			{
				onUpdate(endValue);
			}
			else
			{
				behaviour.StartCoroutine(TweenFloat(startValue, endValue, time, onUpdate));
			}
		}

		private static IEnumerator TweenFloat(float startValue, float endValue, float time, Action<float> onUpdate)
		{
			Assert.IsTrue(time > 0);
			Assert.IsNotNull(onUpdate);
			float t = 0;
			do
			{
				onUpdate(Mathf.Lerp(startValue, endValue, t / time));
				yield return null;
				t += Time.deltaTime;
			} while (t < time);
			onUpdate(endValue);
		}

		public static void SinBounceFloat(this MonoBehaviour behaviour, float startValue, float endValue, float time, Action<float> onUpdate)
		{
			SinBounceFloat(behaviour, startValue, endValue, time, 20, .75f, onUpdate);
		}

		public static void SinBounceFloat(this MonoBehaviour behaviour, float startValue, float endValue, float time, float bounceFrequency, float bounceAmplitude, Action<float> onUpdate)
		{
			if (onUpdate == null)
			{
				return;
			}

			if (time <= 0)
			{
				onUpdate(endValue);
			}
			else
			{
				behaviour.StartCoroutine(SinBounceFloat(startValue, endValue, time, bounceFrequency, bounceAmplitude, onUpdate));
			}
		}

		private static IEnumerator SinBounceFloat(float startValue, float endValue, float time, float bounceFrequency, float bounceAmplitude, Action<float> onUpdate)
		{
			Assert.IsTrue(time > 0);
			Assert.IsNotNull(onUpdate);
			float t = 0;
			do
			{
				float x = t / time;
				// Dampened sin wave bounce.
				float tValue = ((Mathf.Sin((x * x * 10f) + (-Mathf.PI / 2f)) + 1f) * (3.0f / 4.0f)) * (1f - (x * x)) + x;
				onUpdate(Mathf.LerpUnclamped(startValue, endValue, tValue));
				yield return null;
				t += Time.deltaTime;
			} while (t < time);
			onUpdate(endValue);
		}
	}
}
