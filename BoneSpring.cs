using UnityEngine;
using Kazoo.Physics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kazoo.Physics
{
    /// <summary>
    /// Apply a spring force to a transform. Useful for adding secondary motion to bones. Can be used with animated characters, but can be used on any transform.
    /// </summary>
    public class BoneSpring : MonoBehaviour
    {
        public SpringConfiguration configuration = new SpringConfiguration();

        [Header("Visualization")]
        public bool visualizeMaxSpringDistance = false;

        private Vector3 x, xOld, localPStart, springOrigin;
        private Quaternion localRStart;

        private void Awake()
        {
            x = configuration.simulationSpace ? configuration.simulationSpace.InverseTransformPoint(transform.position) : transform.position;
            xOld = configuration.simulationSpace ? configuration.simulationSpace.InverseTransformPoint(transform.position) : transform.position;

            if (configuration.updateType == SpringType.FixedPosition)
            {
                localPStart = transform.localPosition;
            }
            else if (configuration.updateType == SpringType.FixedRotation)
            {
                localRStart = transform.localRotation;
            }
        }

        private void FixedUpdate()
        {
            if (configuration.stepInFixedUpdate)
            {
                Step(Time.deltaTime);
            }
        }

        /// <summary>
        /// Perform a physics sim step.
        /// </summary>
        public void Step(float dt)
        {
            if (dt <= 0)
            {
                return;
            }

            Vector3 xDiff = (springOrigin - x);

            // Spring Force
            Vector3 springForce = xDiff * (configuration.springStrength * dt);
            Vector3 acceleration = (springForce + configuration.gravityForce) / configuration.mass;

            // Velocity from last frame
            Vector3 v = ((x - xOld) / dt) * (configuration.bounceStrength * dt);
            xOld = x;

            // Update Virtual Pos
            x += v + (acceleration * dt);

            // Max Distance Clamp
            if ((x - springOrigin).sqrMagnitude > (configuration.maxSpringDistance * configuration.maxSpringDistance))
            {
                x = Vector3.MoveTowards(springOrigin, x, configuration.maxSpringDistance);
            }
        }

        private void LateUpdate()
        {
            if (configuration.applyInLateUpdate)
            {
                Apply();
            }
        }

        /// <summary>
        /// Apply the position to the transform.
        /// </summary>
        public void Apply()
        {
            if (configuration.weight <= 0)
            {
                return;
            }

            if (configuration.updateType == SpringType.FixedRotation)
            {
                transform.localRotation = localRStart;
            }

            Vector3 springWorldOrigin = Vector3.zero;
            if (configuration.updateType == SpringType.AnimatedRotation || configuration.updateType == SpringType.FixedRotation)
            {
                springWorldOrigin = transform.TransformPoint(configuration.localSpringPoint);
            }
            else if (configuration.updateType == SpringType.FixedPosition && transform.parent != null)
            {
                springWorldOrigin = transform.parent.TransformPoint(localPStart);
            }
            else if (configuration.updateType == SpringType.FixedPosition)
            {
                springWorldOrigin = localPStart;
            }

            springOrigin = configuration.simulationSpace ? configuration.simulationSpace.InverseTransformPoint(springWorldOrigin) : springWorldOrigin;

            Vector3 xWorld = configuration.simulationSpace ? configuration.simulationSpace.TransformPoint(x) : x;

            // Update Bone
            if (configuration.updateType == SpringType.AnimatedRotation || configuration.updateType == SpringType.FixedRotation)
            {
                Quaternion rot = Quaternion.FromToRotation(transform.TransformDirection(configuration.localSpringPoint), xWorld - transform.position) * transform.rotation;
                rot = Quaternion.RotateTowards(transform.rotation, rot, configuration.maxRotationAngleDegrees);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, configuration.weight);
            }
            else if (configuration.updateType == SpringType.FixedPosition)
            {
                transform.position = Vector3.Lerp(transform.position, xWorld, configuration.weight);
            }
        }

        private void OnValidate()
        {
            configuration.mass = Mathf.Max(0.0001f, configuration.mass);
            configuration.maxSpringDistance = Mathf.Max(0, configuration.maxSpringDistance);

            if (configuration.updateType == SpringType.AnimatedRotation && configuration.setLocalSpringPointFromChildTransform && transform.childCount == 1)
            {
                configuration.localSpringPoint = transform.GetChild(0).localPosition;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.01f);

            if (configuration.updateType != SpringType.FixedPosition)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.TransformPoint(configuration.localSpringPoint), 0.005f);
                Gizmos.DrawLine(transform.position, transform.TransformPoint(configuration.localSpringPoint));
            }

            if (visualizeMaxSpringDistance)
            {
                Gizmos.color = Color.grey;
                Gizmos.DrawWireSphere(transform.TransformPoint(configuration.localSpringPoint), configuration.maxSpringDistance);
            }
        }

        public enum SpringType
        {
            /// <summary>
            /// The spring is attached to the animation that is applied in Update or Mecanim.
            /// </summary>
            AnimatedRotation,

            /// <summary>
            /// The spring is attached to the original local position.
            /// </summary>
            FixedPosition,

            /// <summary>
            /// The spring is attached to the original local rotation.
            /// </summary>
            FixedRotation
        }

        [System.Serializable]
        public class SpringConfiguration
        {
            public SpringType updateType = SpringType.AnimatedRotation;
            public Transform simulationSpace;

            [Header("Spring Space")]
            [Tooltip("The local vector where the spring is attached. This can effect how the spring feels.")]
            public Vector3 localSpringPoint = new Vector3(-0.1f, 0, 0);
            public bool setLocalSpringPointFromChildTransform = true;

            [Header("Simulation")]
            [Tooltip("Mass of particle. Leave this 1 unless you know what you're doing.")]
            public float mass = 1;
            [Tooltip("Distance-based spring force.")]
            public float springStrength = 1000f;
            [Range(0, 1), Tooltip("How much velocity should be retained each frame.")]
            public float bounceStrength = 0.5f;
            public Vector3 gravityForce = new Vector3(0, 0, 0);

            [Header("Constraints")]
            [Tooltip("How far off target we can go.")]
            public float maxSpringDistance = 100f;
            [Range(0, 360)]
            public float maxRotationAngleDegrees = 360f;

            [Range(0, 1)]
            public float weight = 1;

            [Header("Simulation Timing")]
            public bool applyInLateUpdate = true;
            public bool stepInFixedUpdate = true;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BoneSpring))]
[CanEditMultipleObjects]
public class BoneSpringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var spring = target as BoneSpring;
        if (spring.configuration.updateType == BoneSpring.SpringType.FixedPosition)
        {
            EditorGUILayout.HelpBox("FixedPosition: The transform will translate only. It's initial position on Awake is assumed to be it's goal position.", MessageType.Info);
        }
        else if (spring.configuration.updateType == BoneSpring.SpringType.FixedRotation)
        {
            EditorGUILayout.HelpBox("FixedRotation: The transform will rotate only. It's initial rotation on Awake is assumed to be it's goal rotation.", MessageType.Info);
        }
        else if (spring.configuration.updateType == BoneSpring.SpringType.AnimatedRotation)
        {
            EditorGUILayout.HelpBox("FixedRotation: The transform will rotate only. It's goal rotation is assumed to be set every frame by Animator or Ik.", MessageType.Info);
        }

        base.OnInspectorGUI();

        if (spring.configuration.updateType != BoneSpring.SpringType.FixedPosition)
        {
            if (spring.configuration.localSpringPoint.sqrMagnitude <= 0)
            {
                EditorGUILayout.HelpBox("For rotating springs, you must set a non-zero-length 'Local Spring Point'.", MessageType.Warning);
            }
        }
    }
}
#endif
