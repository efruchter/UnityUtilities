using System.Collections.Generic;
using UnityEngine;

namespace Kazoo.Animation
{
    /// <summary>
    /// Programatically control eye movement and gaze. Can track a list of targets and switch between them. Can apply noise to eye movements.
    /// </summary>
    public class EyeMovement : MonoBehaviour
    {
        [Tooltip("The transform on the face that points forward. Used to acquire targets.")]
        public Transform frontOfFace;
        [Header("References")]
        [Tooltip("Eyeball objects. Their default positions are set based on starting rotation.")]
        public Transform[] eyeballs = new Transform[0];
        [Tooltip("Potential Targets to scan for")]

        [Header("Targeting")]
        public List<Transform> targets = new List<Transform>(0);
        [Tooltip("The current gaze target, if there is one.")]
        public Transform currentGazeTarget;

        [Header("Configure Eyes")]
        public RandomSphereNoise noise = new RandomSphereNoise();
        public float targetLerpSpeed = 5f;
        public float limitInDegrees = 90f;

        [Header("Configure Blink Trigger")]
        [Tooltip("What change in degrees per second should trigger a blink.")]
        public float degreesPerSecondToTriggerBlink = 90f;
        [Tooltip("How responsive should the angular speed detection be?")]
        public float blinkDetectionResponsiveness = 10f;

        /// <summary>
        /// This callback is fired while the face is rotating beyind a set speed.
        /// A blink can be triggered from this for a more natural appearance.
        /// </summary>
        public System.Action onFaceTurnSpeedExceedsThreshold;

        private Quaternion[] localEyeCenterRot;
        private Quaternion[] localEyeOldRot;
        private Quaternion oldForwardtransformRotation = Quaternion.identity;
        private float angularSpeed = 0;
        private int targetSliceIndex = 0;

        private void Awake()
        {
            FixEyeTransforms();
            localEyeCenterRot = new Quaternion[eyeballs.Length];
            localEyeOldRot = new Quaternion[eyeballs.Length];
            for (int i = 0; i < eyeballs.Length; i++)
            {
                localEyeCenterRot[i] = eyeballs[i].localRotation;
                localEyeOldRot[i] = eyeballs[i].localRotation;
            }
        }

        private void Update()
        {
            if (!frontOfFace)
                return;

            if (onFaceTurnSpeedExceedsThreshold != null)
            {
                float newAngularSpeed = Quaternion.Angle(oldForwardtransformRotation, frontOfFace.rotation) / Time.deltaTime;
                angularSpeed = Mathf.Lerp(angularSpeed, newAngularSpeed, Time.deltaTime * blinkDetectionResponsiveness);
                oldForwardtransformRotation = frontOfFace.rotation;
                if (angularSpeed > degreesPerSecondToTriggerBlink)
                {
                    onFaceTurnSpeedExceedsThreshold();
                }
            }

            if (eyeballs.Length == 0)
                return;

            if (targets.Count > 0)
            {
                targetSliceIndex = (targetSliceIndex + 1) % targets.Count;
                Transform potentialTarget = targets[targetSliceIndex];

                bool targetIsValid = potentialTarget != null
                                    && potentialTarget != currentGazeTarget;

                if (targetIsValid)
                {
                    if (currentGazeTarget == null)
                    {
                        currentGazeTarget = potentialTarget;
                    }
                    else
                    {
                        Quaternion currentTargetRot = Quaternion.LookRotation(currentGazeTarget.position - frontOfFace.position);
                        Quaternion newTargetRot = Quaternion.LookRotation(potentialTarget.position - frontOfFace.position);
                        if (Quaternion.Angle(frontOfFace.rotation, newTargetRot) < Quaternion.Angle(frontOfFace.rotation, currentTargetRot))
                        {
                            currentGazeTarget = potentialTarget;
                        }
                    }
                }
            }

            noise.UpdateNoise();

            if (currentGazeTarget == null)
            {
                for (int i = 0; i < eyeballs.Length; i++)
                {
                    eyeballs[i].localRotation = localEyeCenterRot[i] * noise.noiseValue;
                }
            }
            else
            {
                for (int i = 0; i < eyeballs.Length; i++)
                {
                    Transform eye = eyeballs[i];
                    Quaternion parentalRot = (eye.parent ? eye.parent.rotation : Quaternion.identity);

                    //Get rotation within eye limits that has us look at target
                    Quaternion targetWorld = Quaternion.RotateTowards(
                        parentalRot * localEyeCenterRot[i],
                        Quaternion.LookRotation(currentGazeTarget.position - eye.position),
                        limitInDegrees);

                    //lerp between old local and new local
                    eye.localRotation = Quaternion.Slerp(
                        localEyeOldRot[i],
                        Quaternion.Inverse(parentalRot) * targetWorld,
                        Time.deltaTime * targetLerpSpeed);

                    //save new local
                    localEyeOldRot[i] = eye.localRotation;

                    //Apply noise
                    eye.localRotation *= noise.noiseValue;
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.Lerp(Color.grey, Color.blue, Mathf.PingPong(Time.time / 4f, 1));
            if (currentGazeTarget != null)
            {
                for (int i = 0; i < eyeballs.Length; i++)
                {
                    Gizmos.DrawLine(eyeballs[i].position, currentGazeTarget.position);
                }
                Gizmos.DrawWireSphere(currentGazeTarget.position, 0.04f);
            }

            if (frontOfFace != null)
            {
                Gizmos.DrawLine(frontOfFace.position, frontOfFace.position + frontOfFace.forward * 1.5f);
            }

            for (int i = 0; i < eyeballs.Length; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(eyeballs[i].position, eyeballs[i].position + eyeballs[i].forward * 0.3f);
            }
        }

        private void OnValidate()
        {
            FixEyeTransforms();
        }

        private void FixEyeTransforms()
        {
            for (int i = 0; i < eyeballs.Length; i++)
            {
                eyeballs[i].LookAt(eyeballs[i].position + eyeballs[i].forward);
            }
        }
    }

    /// <summary>
    /// Generate a continuous random rotation within rotation limits.
    /// </summary>
    [System.Serializable]
    public class RandomSphereNoise
    {
        [Tooltip("How many degrees can we move per second, at most.")]
        public float intensity = 100f;
        [Tooltip("What is the limit to how many degrees from (0, 0, 0) we can be.")]
        public float radiusInDegree = 5;
        [Tooltip("How much the noise effects the end rotation.")]
        public float lerpSpeed = 0.5f;

        public Quaternion noiseValue { private set; get; }
        private float xSim = 0, ySim = 0, zSim = 0;

        public void UpdateNoise()
        {
            //Move randomly around a cube, then map it to a rotation
            xSim = Mathf.Clamp(xSim + Random.Range(-intensity, intensity) * Time.deltaTime, -radiusInDegree, radiusInDegree);
            ySim = Mathf.Clamp(ySim + Random.Range(-intensity, intensity) * Time.deltaTime, -radiusInDegree, radiusInDegree);
            zSim = Mathf.Clamp(zSim + Random.Range(-intensity, intensity) * Time.deltaTime, -radiusInDegree, radiusInDegree);

            noiseValue = Quaternion.Slerp(noiseValue, Quaternion.Euler(xSim, ySim, zSim), Time.deltaTime * lerpSpeed);
        }
    }
}
