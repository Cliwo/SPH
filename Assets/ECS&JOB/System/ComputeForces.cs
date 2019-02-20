using UnityEngine;
using System.Collections.Generic;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
[BurstCompile]
public struct ComputeForces : IJobParallelFor
{
	[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
	[ReadOnly] public NativeArray<int> cellOffsetTable;
	[ReadOnly] public NativeArray<Position> particlesPosition;
	[ReadOnly] public NativeArray<SPHVelocity> particlesVelocity;
	[ReadOnly] public NativeArray<float> particlesPressure;
	[ReadOnly] public NativeArray<float> particlesDensity;
	[ReadOnly] public SPHParticle settings;

	public NativeArray<float3> particlesForces;

	private const float PI = 3.14159274F;
	public float deltaTime;

	public void Execute(int index)
	{
		// Cache
		int particleCount = particlesPosition.Length;
		float3 position = particlesPosition[index].Value;
		float3 velocity = particlesVelocity[index].Value;
		float pressure = particlesPressure[index];
		float density = particlesDensity[index];
		float3 forcePressure = new float3(0, 0, 0);
		float3 forceViscosity = new float3(0, 0, 0);
		int i, hash, j;
		int3 gridOffset;
		int3 gridPosition = GridHash.Quantize(position, settings.Radius);
		bool found;

		// Physics
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
				if (index == j)
				{
					found = hashMap.TryGetNextValue(out j, ref iterator);
					continue;
				}

				float3 rij = particlesPosition[j].Value - position;
				float r2 = math.lengthsq(rij);
				float r = math.sqrt(r2);

				if (r < settings.SmoothingRadius)
				{
					float I_poly = pressure / (density * density);
					float J_poly = particlesPressure[j] / (particlesDensity[j] * particlesDensity[j]);

					forcePressure += -settings.mass * (I_poly+J_poly) *  SpikyGradient(settings.SmoothingRadius, r, math.normalize(rij));
					//math.normalize(rij) * (-45.0f / (PI * PowUtility.IntPow(settings.SmoothingRadius, 6))) * PowUtility.IntPow(settings.SmoothingRadius - r, 2);
					
					// forcePressure *= density;
					//2월 19일 위의 계산 에러날 경우 * density 적용하기.

					forceViscosity += settings.Viscosity * settings.mass * (particlesVelocity[j].Value - velocity) / particlesDensity[j] * ViscosityLaplacian(settings.SmoothingRadius, r);
				}

				// Next neighbor
				found = hashMap.TryGetNextValue(out j, ref iterator);
			}
		}

		// Gravity
		float3 forceGravity = new float3(0.0f, -9.81f, 0.0f) * density * settings.GravityMult;
		
		// Log
		// int delTime = (int)(deltaTime);
		// if(delTime % 2 == 0)
		// {
		// 	// string line = "Hi";
		// 	string line = FrameDebuggerUtil.EncodeInCSV(
		// 	new KeyValuePair<string,string>("Frame", delTime+""),
		// 	new KeyValuePair<string,string>("Density", density+""),
		// 	new KeyValuePair<string,string>("Pressure", forcePressure+""),
		// 	new KeyValuePair<string,string>("Viscosity", forceViscosity+""),
		// 	new KeyValuePair<string,string>("Gravity", forceGravity+"")
		// 	);
		// 	FrameDebuggerUtil.EnqueueString(line);
		// }

		// Apply
		particlesForces[index] = forcePressure + forceViscosity + forceGravity;

	}

	private float3 SpikyGradient(float h, float mag, float3 normalized)
	{
		float coef =- (45.0f / Mathf.PI / PowUtility.IntPow(h,6));
		float i = PowUtility.IntPow(h - mag, 2);
		return coef * i * normalized ;
	}

	private float ViscosityLaplacian(float h , float mag)
	{
		return 45.0f / Mathf.PI / PowUtility.IntPow(h,6) * (h - mag);
	}
}
