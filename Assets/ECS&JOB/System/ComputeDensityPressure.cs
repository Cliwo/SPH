using UnityEngine;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
public struct ComputeDensityPressure : IJobParallelFor
{
	[ReadOnly] public NativeMultiHashMap<int, int> hashMap; //왼쪽이 hash, 오른쪽이 index 
	[ReadOnly] public NativeArray<int> cellOffsetTable;
	[ReadOnly] public NativeArray<Position> particlesPosition;
	[ReadOnly] public SPHParticle settings;

	public NativeArray<float> densities;
	public NativeArray<float> pressures;

	public float gameTime;

	private const float PI = 3.14159274F;
	

	public void Execute(int index) // Non Hash
	{
		int particleCount = particlesPosition.Length;
		float3 myPos = particlesPosition[index].Value;

		float density = 0.0f;
		int count = 0;
		for(int i = 0 ; i < particlesPosition.Length; i++)
		{
			float3 diff = particlesPosition[i].Value - myPos;
			float sqrLen = math.lengthsq(diff);
			if(sqrLen < settings.SmoothingRadiusSq)
			{
				count ++;
				density += settings.mass * (315.0f / (64.0f * PI * math.pow(settings.SmoothingRadius, 9.0f))) * math.pow(settings.SmoothingRadiusSq - sqrLen, 3.0f);
				// if(index == 17 && gameTime == 0)
				// {
				// 	float den = settings.mass * (315.0f / (64.0f * PI * math.pow(settings.SmoothingRadius, 9.0f))) * math.pow(settings.SmoothingRadiusSq - sqrLen, 3.0f);
				// 	Debug.Log("Density : " + den);
				// 	Debug.Log("r2 : " + sqrLen);
				// }
			}
		}

		// int time = (int)(gameTime / 0.017);
		// if(time % 10 == 0)
		// {
		// 	string line = FrameDebuggerUtil.EncodeInCSV(
		// 		new System.Collections.Generic.KeyValuePair<string, string>("Frame" , "" + time),
		// 		new System.Collections.Generic.KeyValuePair<string,string>("Density" , "" + density),
		// 		new System.Collections.Generic.KeyValuePair<string,string>("Index" , "" + index),
		// 		new System.Collections.Generic.KeyValuePair<string,string>("Found Count" , "" + count)
		// 	);
		// 	FrameDebuggerUtil.EnqueueString(line);
		// }
		densities[index] = density;
		pressures[index] = settings.GasConstant * (density - settings.RestDensity);
	}

	// public void Execute(int index)
	// {
	// 	// Cache
	// 	int particleCount = particlesPosition.Length;
	// 	float3 position = particlesPosition[index].Value;
	// 	float density = 0.0f;
	// 	int i, hash, j;
	// 	int3 gridOffset;
	// 	int3 gridPosition = GridHash.Quantize(position, settings.Radius); //공간을 voxel로 쪼갰을 때 voxel의 position 이라고 보면 된다. 
	// 	bool found;

	// 	int count = 0;
	// 	// Find neighbors
	// 	int neighbor = cellOffsetTable.Length / 3;
	// 	for (int oi = 0; oi < neighbor; oi++)
	// 	{
	// 		i = oi * 3;
	// 		gridOffset = new int3(cellOffsetTable[i], cellOffsetTable[i + 1], cellOffsetTable[i + 2]);
	// 		hash = GridHash.Hash(gridPosition + gridOffset);
	// 		NativeMultiHashMapIterator<int> iterator;
	// 		found = hashMap.TryGetFirstValue(hash, out j, out iterator);
			
	// 		while (found)
	// 		{
	// 			// Neighbor found, get density
	// 			count ++;
	// 			float3 rij = particlesPosition[j].Value - position;
	// 			float r2 = math.lengthsq(rij);

	// 			if (r2 < settings.SmoothingRadiusSq)
	// 			{
	// 				density += settings.mass * (315.0f / (64.0f * PI * math.pow(settings.SmoothingRadius, 9.0f))) * math.pow(settings.SmoothingRadiusSq - r2, 3.0f);
	// 			}

	// 			// Next neighbor
	// 			found = hashMap.TryGetNextValue(out j, ref iterator);
	// 		}
	// 	}

	// 	// int time = (int)(gameTime / 0.017);
	// 	// if(time % 10 == 0)
	// 	// {
	// 	// 	string line = FrameDebuggerUtil.EncodeInCSV(
	// 	// 		new System.Collections.Generic.KeyValuePair<string, string>("Frame" , "" + time),
	// 	// 		new System.Collections.Generic.KeyValuePair<string,string>("Density" , "" + density),
	// 	// 		new System.Collections.Generic.KeyValuePair<string,string>("Index" , "" + index),
	// 	// 		new System.Collections.Generic.KeyValuePair<string,string>("Found Count" , "" + count)
	// 	// 	);
	// 	// 	FrameDebuggerUtil.EnqueueString(line);
	// 	// }
	// 	densities[index] = density;
	// 	pressures[index] = settings.GasConstant * (density - settings.RestDensity);
	// }
}

