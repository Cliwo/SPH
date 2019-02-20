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

		//Surface
		float3 n = new float3(0, 0, 0);
		float3 forceSurface = new float3(0,0,0);
		float laplacian = 0.0f;
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

					forcePressure += (I_poly+J_poly) *  SpikyGradient(settings.SmoothingRadius, r, math.normalize(rij));
					//math.normalize(rij) * (-45.0f / (PI * PowUtility.IntPow(settings.SmoothingRadius, 6))) * PowUtility.IntPow(settings.SmoothingRadius - r, 2);
					forceViscosity += settings.mass * (particlesVelocity[j].Value - velocity) / particlesDensity[j] * ViscosityLaplacian(settings.SmoothingRadius, r);

					
					n += settings.mass / particlesDensity[j] * Poly6Gradient(settings.SmoothingRadius, rij, r2);
					laplacian += settings.mass / particlesDensity[j] 
								* Poly6Laplacian(settings.SmoothingRadius, r2);
					
					
				}

				// Next neighbor
				found = hashMap.TryGetNextValue(out j, ref iterator);
			}
		}

		// Gravity
		float3 forceGravity = new float3(0.0f, -9.81f, 0.0f) * density * settings.GravityMult;
		
		//Surface
		const float sigma = 0.07197f; // 0.0719-> N/m 단위  71.97mN/m (밀리뉴턴, 미터 단위) (25도에서)) 
		const float threshold = 7.065f;
		float magnitude = math.length(n);
		if(magnitude >= threshold)
		{
			float kappa = -laplacian / magnitude;
			forceSurface = (sigma * kappa * n);
		}

		forceViscosity *= settings.Viscosity;
		forcePressure *= -settings.mass * density;
		
		// Log
		int delTime = (int)(deltaTime);
		if(delTime % 2 == 0)
		{
			// string line = "Hi";
			string line = FrameDebuggerUtil.EncodeInCSV(
			new KeyValuePair<string,string>("Frame", delTime+""),
			new KeyValuePair<string,string>("Density", density+""),
			new KeyValuePair<string,string>("Pressure", forcePressure+"")
			);
			FrameDebuggerUtil.EnqueueString(line);
		}

		// Apply
		particlesForces[index] =  forcePressure;
		// particlesForces[index] = forceSurface + forcePressure + forceViscosity + forceGravity;
	}
	private float3 Poly6Gradient(float h, float3 position, float sqr)
	{
		float coef = - 945.0f / (32*Mathf.PI*PowUtility.IntPow(h,9));
		Vector3 result  =  coef * position * PowUtility.IntPow((h*h - sqr), 2);
		return result;
	}
	private float Poly6Laplacian(float h, float sqr)
	{
		float result = -945.0f / (32.0f * Mathf.PI * PowUtility.IntPow(h,9)) * (h*h - sqr) 
			* (3.0f * h*h - 7.0f * sqr);
		return result;
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
