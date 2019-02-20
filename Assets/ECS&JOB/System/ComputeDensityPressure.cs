using UnityEngine;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

using System.Collections.Generic;
[BurstCompile]
public struct ComputeDensityPressure : IJobParallelFor
{
	[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
	[ReadOnly] public NativeArray<int> cellOffsetTable;
	[ReadOnly] public NativeArray<Position> particlesPosition;
	[ReadOnly] public SPHParticle settings;

	public NativeArray<float> densities;
	public NativeArray<float> pressures;
	public float deltaTime;

	private const float PI = 3.14159274F;
	private const float GAS_CONST = 3.0f;

	

	public void Execute(int index)
	{
		int particleCount = particlesPosition.Length;
		float3 position = particlesPosition[index].Value;
		float density = 0.0f;
		int count = 0;
		int i, hash, j;
		int3 gridOffset;
		int3 gridPosition = GridHash.Quantize(position, settings.Radius);
		bool found;

		for (int oi = 0; oi < 27; oi++)
		{
			i = oi * 3;
			gridOffset = new int3(cellOffsetTable[i], cellOffsetTable[i + 1], cellOffsetTable[i + 2]);
			hash = GridHash.Hash(gridPosition + gridOffset);
			NativeMultiHashMapIterator<int> iterator;
			found = hashMap.TryGetFirstValue(hash, out j, out iterator);
			while (found)
			{
				float3 rij = particlesPosition[j].Value - position;
				float r2 = math.lengthsq(rij);

				if (r2 < settings.SmoothingRadiusSq)
				{
					count ++;
					density += settings.mass * Poly6(settings.SmoothingRadius, r2);
					// int delTim = (int)(deltaTime);
					// if(delTim % 2 == 0)
					// {
					// 	string line = FrameDebuggerUtil.EncodeInCSV(
					// 	new KeyValuePair<string,string>("Index", index+""),
					// 	new KeyValuePair<string,string>("distance", rij+""),
					// 	new KeyValuePair<string,string>("density", density+"")
					// 	);
					// 	FrameDebuggerUtil.EnqueueString(line);
					// }
				}

				found = hashMap.TryGetNextValue(out j, ref iterator);
			}
		}
		// int delTime = (int)(deltaTime);
		// if(delTime % 2 == 0)
		// {
		// 	// string line = "Hi";
		// 	string line = FrameDebuggerUtil.EncodeInCSV(
		// 	new KeyValuePair<string,string>("Index", index+""),
		// 	new KeyValuePair<string,string>("Frame", delTime+""),
		// 	new KeyValuePair<string,string>("Total Density", density+""),
		// 	new KeyValuePair<string,string>("Influence", count+"")
		// 	);
		// 	FrameDebuggerUtil.EnqueueString(line);
		// }
		densities[index] = density;
		pressures[index] = GAS_CONST * (density - settings.RestDensity);
	}

	private float Poly6(float h, float sqr)
	{
		float coef = 315.0f / (64.0f * PI * PowUtility.IntPow(h,9));
		float result = coef * PowUtility.IntPow((h*h - sqr),3);
		return result;
	}
}

