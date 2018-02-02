using System;
using UnityEngine;

/// <summary>
/// Copies transform position/orientation from one reference frame to another.
/// </summary>
public class PuppetVrRig : MonoBehaviour
{
	[ SerializeField, Tooltip( "The Rig that you want the puppet to imitate." ) ]
	private RigProfile driver;

	[ SerializeField, Tooltip( "The puppet's rig." ) ]
	private RigProfile driven;

	[ Serializable ]
	public struct RigProfile
	{
		public Transform Area, Head, LeftHand, RightHand;
	}
	
	#region Unity

	void LateUpdate()
	{
		TransferPose();
	}

	#endregion

	private void TransferPose()
	{
		if ( driver.Area && driven.Area )
		{
			// Create the mappings from coordinate space of DRIVER to coordinate space of DRIVEN
			var tMat = driver.Area.localToWorldMatrix * driven.Area.localToWorldMatrix;
			var rQuat = Quaternion.Inverse( driver.Area.rotation ) * driven.Area.rotation;
			
			if ( driver.Head && driven.Head )
			{
				driven.Head.localPosition = tMat * driver.Head.localPosition;
				driven.Head.localRotation = rQuat * driver.Head.localRotation;
			}
			
			if ( driver.LeftHand && driven.LeftHand )
			{
				driven.LeftHand.localPosition = tMat * driver.LeftHand.localPosition;
				driven.LeftHand.localRotation = rQuat * driver.LeftHand.localRotation;
			}
			
			if ( driver.RightHand && driven.RightHand )
			{
				driven.RightHand.localPosition = tMat * driver.RightHand.localPosition;
				driven.RightHand.localRotation = rQuat * driver.RightHand.localRotation;
			}
		}
	}
}
