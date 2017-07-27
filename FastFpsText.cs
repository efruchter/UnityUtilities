using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Custom
{
	[RequireComponent(typeof(Text))]
	public class FastFpsText : MonoBehaviour
	{
		public float updateRate = 1f;
		public RoundingBehaviour roundingBehaviour = RoundingBehaviour.Ceiling;

		private readonly string[] fpsCache = new string[141];

		private Text text;
		private float updateAccumulator;
		private long framesElapsed;

		private void Awake()
		{
			for (int i = 0; i < fpsCache.Length; i++)
			{
				fpsCache [i] = i.ToString ();
			}

			text = GetComponent<Text> ();
			updateAccumulator = 0;
			framesElapsed = 0;
		}

		protected void LateUpdate()
		{
			float correctedUpdateRate = Mathf.Max (updateRate, Time.deltaTime);
			
			if (correctedUpdateRate <= 0)
			{
				WriteFps (0);
			}
			
			updateAccumulator += Time.deltaTime;
			framesElapsed++;

			if (updateAccumulator >= correctedUpdateRate)
			{
				WriteFps (framesElapsed / updateAccumulator);
				updateAccumulator -= correctedUpdateRate;
				framesElapsed = 0;
			}
		}

		protected void OnValidate()
		{
			updateRate = Mathf.Max (1f / 100f, updateRate);
		}

		private void WriteFps(float fpsActual)
		{
			int fpsVirtual;
			switch (roundingBehaviour)
			{
			case RoundingBehaviour.Ceiling:
				fpsVirtual = Mathf.CeilToInt (fpsActual);
				break;
			case RoundingBehaviour.Floor:
				fpsVirtual = Mathf.FloorToInt (fpsActual);
				break;
			default:
				//fallthrough
			case RoundingBehaviour.Round:
				fpsVirtual = Mathf.RoundToInt (fpsActual);
				break;
			}

			int fpsCacheIndex = Mathf.Clamp (fpsVirtual, 0, fpsCache.Length - 1);
			string fpsRef = fpsCache [fpsCacheIndex];
			text.text = fpsRef;
		}

		public enum RoundingBehaviour
		{
			Round, Ceiling, Floor
		}
	}
}
