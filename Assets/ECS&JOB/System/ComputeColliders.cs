using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

using UnityEngine;

[BurstCompile]
public struct ComputeColliders : IJobParallelFor
{
	[ReadOnly] public SPHParticle settings;
	[ReadOnly] public NativeArray<SPHCollider> copyColliders;

	public NativeArray<Position> particlesPosition;
	public NativeArray<SPHVelocity> particlesVelocity;

	private const float BOUND_DAMPING = -0.5f;



	private static bool Intersect(SPHCollider collider, float3 position, float radius, out float3 penetrationNormal, out float3 penetrationPosition, out float penetrationLength)
	{	
		float3 colliderProjection = collider.position - position;

		penetrationNormal = math.cross(collider.right, collider.up);
		penetrationLength = math.abs(math.dot(colliderProjection, penetrationNormal)) - (radius / 2.0f);
		penetrationPosition = collider.position - colliderProjection;

		return penetrationLength < 0.0f
			&& math.abs(math.dot(colliderProjection, collider.right)) < collider.scale.x
			&& math.abs(math.dot(colliderProjection, collider.up)) < collider.scale.y;
	}



	private static Vector3 DampVelocity(SPHCollider collider, float3 velocity, float3 penetrationNormal, float drag)
	{
		float3 newVelocity = math.dot(velocity, penetrationNormal) * penetrationNormal * BOUND_DAMPING
							+ math.dot(velocity, collider.right) * collider.right * drag
							+ math.dot(velocity, collider.up) * collider.up * drag;
		newVelocity = math.dot(newVelocity, new float3(0, 0, 1)) * new float3(0, 0, 1)
					+ math.dot(newVelocity, new float3(1, 0, 0)) * new float3(1, 0, 0)
					+ math.dot(newVelocity, new float3(0, 1, 0)) * new float3(0, 1, 0);
		return newVelocity;
	}



	public void Execute(int index)
	{
		// Cache
		int colliderCount = copyColliders.Length;
		float3 position = particlesPosition[index].Value;
		float3 velocity = particlesVelocity[index].Value;

		// Process
		for (int i = 0; i < colliderCount; i++)
		{
			float3 penetrationNormal;
			float3 penetrationPosition;
			float penetrationLength;
			if (Intersect(copyColliders[i], position, settings.Radius, out penetrationNormal, out penetrationPosition, out penetrationLength))
			{
				velocity = DampVelocity(copyColliders[i], velocity, penetrationNormal, 1.0f - settings.Drag);
				position = penetrationPosition - penetrationNormal * math.abs(penetrationLength);
			}
		}

		// Apply
		particlesVelocity[index] = new SPHVelocity { Value = velocity };
		particlesPosition[index] = new Position { Value = position };
	}
}
