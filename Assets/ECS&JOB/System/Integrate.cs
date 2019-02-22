using UnityEngine;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
[BurstCompile]
public struct Integrate : IJobParallelFor
{
	[ReadOnly] public NativeArray<float3> particlesForces;
	[ReadOnly] public NativeArray<float> particlesDensity;

	public NativeArray<Position> particlesPosition;
	public NativeArray<SPHVelocity> particlesVelocity;

	private const float DT = 0.001f;



	public void Execute(int index)
	{
		// Cache
		float3 velocity = particlesVelocity[index].Value;
		float3 position = particlesPosition[index].Value;

		// Process
		velocity += DT * particlesForces[index] / particlesDensity[index];
		position += DT * velocity;


		// Apply
		particlesVelocity[index] = new SPHVelocity { Value = velocity };
		particlesPosition[index] = new Position { Value = position };
	}
}

