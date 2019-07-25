using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public class RotationSpring : MonoBehaviour
{
    public float springK = 10f;
    public float speed = 5f;
    public float fps = 60;
    public bool zAxisOnly;

    float4 _vel;
    float4 _targetPose;
    float _cooldown = 0;

    private void Awake()
    {
        SetTargetPose(transform.localRotation);
        ClearVelocity();
    }

    public void SetTargetPose(quaternion localRot)
    {
        _targetPose = AsFloat4(localRot);
    }

    public void ClearVelocity()
    {
        _vel = AsFloat4(quaternion.identity);
    }

    public void Update()
    {
        TickPhysics(Time.deltaTime);
    }

    public void TickPhysics(in float tickTime)
    {
        _cooldown += tickTime;

        float stepSize = 1f / fps;

        while (_cooldown >= stepSize && stepSize > 0)
        {
            _cooldown -= stepSize;

            float dt = stepSize * speed;

            float4 pos = AsFloat4(transform.localRotation);
            float4 springForce = springK * (_targetPose - pos);

            _vel = _vel + (springForce * dt);

            if (zAxisOnly)
                _vel.xy = math.float2(0);

            _vel = math.normalizesafe(_vel);

            pos += (_vel * dt) + (springForce * (0.5f * dt * dt));

            if (zAxisOnly)
                pos.xy = math.float2(0);

            transform.localRotation = math.quaternion(pos);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float4 AsFloat4(in quaternion rot)
    {
        return rot.value;
    }
}
