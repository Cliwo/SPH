using UnityEngine;

using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
[BurstCompile]
public struct ApplyPositions : IJobParallelFor
{
	[ReadOnly] public NativeArray<Position> particlesPosition;
	[ReadOnly] public NativeArray<SPHVelocity> particlesVelocity;

	public ComponentDataArray<Position> positions;
	public ComponentDataArray<SPHVelocity> velocities;

	public void Execute(int index)
	{
		// Apply to components
		// Debug.Log(index);
		// Debug.Log("Position : " + particlesPosition[index].Value);
		// Debug.Log("Velocity : " + particlesVelocity[index]);
		
		positions[index] = new Position { Value = particlesPosition[index].Value };
		velocities[index] = particlesVelocity[index];
	}
}
