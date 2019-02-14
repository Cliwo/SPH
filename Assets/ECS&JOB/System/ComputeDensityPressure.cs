using UnityEngine;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public struct ComputeDensityPressure : IJobParallelFor
{
	[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
	[ReadOnly] public NativeArray<int> cellOffsetTable;
	[ReadOnly] public NativeArray<Position> particlesPosition;
	[ReadOnly] public SPHParticle settings;

	public NativeArray<float> densities;
	public NativeArray<float> pressures;

	private const float PI = 3.14159274F;
	private const float GAS_CONST = 2000.0f;

	

	public void Execute(int index)
	{
		// Cache
		int particleCount = particlesPosition.Length;
		float3 position = particlesPosition[index].Value;
		float density = 0.0f;
		int i, hash, j;
		int3 gridOffset;
		int3 gridPosition = GridHash.Quantize(position, settings.Radius);
		bool found;

		// Find neighbors
		for (int oi = 0; oi < 27; oi++)
		{
			i = oi * 3;
			gridOffset = new int3(cellOffsetTable[i], cellOffsetTable[i + 1], cellOffsetTable[i + 2]);
			hash = GridHash.Hash(gridPosition + gridOffset);
			NativeMultiHashMapIterator<int> iterator;
			found = hashMap.TryGetFirstValue(hash, out j, out iterator);
			while (found)
			{
				// Neighbor found, get density
				float3 rij = particlesPosition[j].Value - position;
				float r2 = math.lengthsq(rij);

				if (r2 < settings.SmoothingRadiusSq)
				{
					density += settings.mass * (315.0f / (64.0f * PI * math.pow(settings.SmoothingRadius, 9.0f))) * math.pow(settings.SmoothingRadiusSq - r2, 3.0f);
				}

				// Next neighbor
				found = hashMap.TryGetNextValue(out j, ref iterator);
			}
		}
		// Debug.Log("Den : " + density);
		// Apply density and compute/apply pressure
		densities[index] = density;
		pressures[index] = GAS_CONST * (density - settings.RestDensity);
	}
}

