using Unity.Mathematics;
using UnityEngine;

public class RotationSpring : MonoBehaviour
{
    public float springK = 10f;
    public float speed = 5f;
    public float fps = 60;

    float4 _vel;
    float4 _targetPose;
    float _cooldown = 0;

    private void Awake()
    {
        _targetPose = v4(transform.localRotation);
    }

    void Update()
    {
        _cooldown += Time.deltaTime;

        float stepSize = 1f / fps;

        if (_cooldown >= stepSize)
        {
            _cooldown -= stepSize;

            float dt = stepSize * speed;

            float4 pos = v4(transform.localRotation); 
            float4 springForce = springK * (_targetPose - pos);

            _vel = math.normalizesafe(_vel + (springForce * dt));
            pos += (_vel * dt) + (springForce * (0.5f * dt * dt));

            transform.localRotation = math.quaternion(pos);
        }
    }

    static float4 v4(quaternion rot)
    {
        return rot.value;
    }
}
