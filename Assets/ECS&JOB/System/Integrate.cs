using UnityEngine;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
[BurstCompile]
public struct Integrate : IJobParallelFor
{
	[ReadOnly] public float timeStep;
	[ReadOnly] public NativeArray<float3> particlesForces;
	[ReadOnly] public NativeArray<float> particlesDensity;

	public NativeArray<Position> particlesPosition;
	public NativeArray<SPHVelocity> particlesVelocity;

	public void Execute(int index)
	{
		// Cache
		float3 velocity = particlesVelocity[index].Value;
		float3 position = particlesPosition[index].Value;

		// Process
		velocity += timeStep * particlesForces[index] / particlesDensity[index];
		position += timeStep * velocity;


		// Apply
		particlesVelocity[index] = new SPHVelocity { Value = velocity };
		particlesPosition[index] = new Position { Value = position };
	}
}

