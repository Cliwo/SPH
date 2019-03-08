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

	public float gameTime;

	private const float PI = 3.14159274F;


	public void Execute(int index) // Non Hash
	{
		float3 myPos = particlesPosition[index].Value;
		float3 myVel = particlesVelocity[index].Value;
		float myPressure = particlesPressure[index];
		float myDensity = particlesDensity[index];

		float3 force_pressure = new float3(0);
		float3 force_viscosity = new float3(0);
		float3 force_surface = new float3(0);

		float3 n = new float3(0);
		float laplacian = 0.0f;
		
		for(int i = 0 ; i < particlesPosition.Length; i++)
		{
			if(i == index)
			{
				continue;
			}
			
			float3 diff = particlesPosition[i].Value - myPos;
			float sqrLen = math.lengthsq(diff);
			float len = math.length(diff);
			if(len < settings.SmoothingRadius)
			{
				// force_pressure += -math.normalize(diff) * settings.mass * (2.0f * myPressure) / (2.0f * myDensity) * (-45.0f / (PI * math.pow(settings.SmoothingRadius, 6.0f))) * math.pow(settings.SmoothingRadius - len, 2.0f);
				force_pressure +=   ((myPressure) / (myDensity * myDensity) + (particlesPressure[i]) / (particlesDensity[i] * particlesDensity[i])) * 
									math.normalize(diff) * (-45.0f / (PI * math.pow(settings.SmoothingRadius, 6.0f))) * math.pow(settings.SmoothingRadius - len, 2.0f);
				// force_viscosity += settings.Viscosity * settings.mass * (particlesVelocity[i].Value - myVel) / myDensity * (45.0f / (PI * math.pow(settings.SmoothingRadius, 6.0f))) * (settings.SmoothingRadius - len);
				force_viscosity += settings.Viscosity * settings.mass * 
									(particlesVelocity[i].Value - myVel) / particlesDensity[i] * 
									(45.0f / (PI * math.pow(settings.SmoothingRadius, 6.0f))) * (settings.SmoothingRadius - len);
				// float j_term = settings.mass / myDensity;
				float j_term = settings.mass / particlesDensity[i];
				n += j_term * Poly6Gradient(settings.SmoothingRadius, diff, sqrLen);
				laplacian += j_term * Poly6Laplacian(settings.SmoothingRadius, sqrLen);
			}
		}

		// Gravity
		float3 force_gravity = new float3(0.0f, -9.81f, 0.0f) * myDensity * settings.GravityMult;

		//Surface
		float mag = math.length(n);
		if(mag > settings.SurfaceThreshold)
		{
			float kappa = -laplacian / mag;
			force_surface = settings.SurfaceTension * kappa * n;
		}

		force_pressure *= -settings.mass * myDensity;

		// int time = (int)(gameTime / 0.017);
		// if(time % 10 == 0)
		// {
		// 	string debugT = FrameDebuggerUtil.EncodeInCSV(
		// 	new KeyValuePair<string, string>("Frame", ""+time),
		// 	new KeyValuePair<string, string>("Index", ""+index),
		// 	new KeyValuePair<string, string>("pressure",""+ToStringFloat3(force_pressure)),
		// 	new KeyValuePair<string, string>("viscosity",""+ToStringFloat3(force_viscosity)),
		// 	new KeyValuePair<string, string>("surface",""+ToStringFloat3(force_surface)),
		// 	new KeyValuePair<string, string>("Density", ""+myDensity)
		// 	);
		// 	FrameDebuggerUtil.EnqueueString(debugT);
		// }

		particlesForces[index] = force_pressure + force_viscosity + force_surface + force_gravity;
	}

	// public void Execute(int index)
	// {
	// 	// Cache
	// 	int particleCount = particlesPosition.Length;
	// 	float3 position = particlesPosition[index].Value;
	// 	float3 velocity = particlesVelocity[index].Value;
	// 	float pressure = particlesPressure[index];
	// 	float density = particlesDensity[index];
	// 	float3 forcePressure = new float3(0, 0, 0);
	// 	float3 forceViscosity = new float3(0, 0, 0);
	// 	int i, hash, j;
	// 	int3 gridOffset;
	// 	int3 gridPosition = GridHash.Quantize(position, settings.Radius);
	// 	bool found;

	// 	// Surface
	// 	float3 n = new float3(0,0,0);
	// 	float laplacian = 0.0f;
	// 	float3 forceSurface = new float3(0,0,0);
		
	// 	// Physics
	// 	// Find neighbors
	// 	int neighbor = cellOffsetTable.Length/3;
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
	// 			if (index == j)
	// 			{
	// 				found = hashMap.TryGetNextValue(out j, ref iterator);
	// 				continue;
	// 			}

	// 			float3 rij = particlesPosition[j].Value - position;
	// 			float r2 = math.lengthsq(rij);
	// 			float r = math.sqrt(r2);
				
	// 			if (r < settings.SmoothingRadius)
	// 			{
	// 				forcePressure += -math.normalize(rij) * settings.mass * (2.0f * pressure) / (2.0f * density) * (-45.0f / (PI * math.pow(settings.SmoothingRadius, 6.0f))) * math.pow(settings.SmoothingRadius - r, 2.0f);

	// 				forceViscosity += settings.Viscosity * settings.mass * (particlesVelocity[j].Value - velocity) / density * (45.0f / (PI * math.pow(settings.SmoothingRadius, 6.0f))) * (settings.SmoothingRadius - r);

	// 				float j_term = settings.mass / density;
	// 				n += j_term * Poly6Gradient(settings.SmoothingRadius, rij, r2);
	// 				laplacian += j_term * Poly6Laplacian(settings.SmoothingRadius, r2);
	// 			}

	// 			// Next neighbor
	// 			found = hashMap.TryGetNextValue(out j, ref iterator);
	// 		}
	// 	}

	// 	// Gravity
	// 	float3 forceGravity = new float3(0.0f, -9.81f, 0.0f) * density * settings.GravityMult;

	// 	//Surface
	// 	float mag = math.length(n);
	// 	if(mag > settings.SurfaceThreshold)
	// 	{
	// 		float kappa = -laplacian / mag;
	// 		forceSurface = settings.SurfaceTension * kappa * n;
	// 	}
		

	// 	// int time = (int)(gameTime / 0.017);
	// 	// if(time % 10 == 0)
	// 	// {
	// 	// 	string debugT = FrameDebuggerUtil.EncodeInCSV(
	// 	// 	new KeyValuePair<string, string>("Frame", ""+time),
	// 	// 	new KeyValuePair<string, string>("pressure",""+ToStringFloat3(forcePressure)),
	// 	// 	new KeyValuePair<string, string>("viscosity",""+ToStringFloat3(forceViscosity)),
	// 	// 	new KeyValuePair<string, string>("surface",""+ToStringFloat3(forceSurface)),
	// 	// 	new KeyValuePair<string, string>("Density", ""+density)
	// 	// 	);
	// 	// 	FrameDebuggerUtil.EnqueueString(debugT);
	// 	// }
		

	// 	// Apply
	// 	particlesForces[index] = forcePressure + forceViscosity + forceSurface + forceGravity;
	// 	// particlesForces[index] = forceSurface + forceViscosity;
	// 	// particlesForces[index] = forcePressure;
	// }	

	private float3 Poly6Gradient(float h, float3 pos, float sqr)
	{
		float coef = - 945.0f / (32.0f * PI * math.pow(h,9));
		float3 result  =  coef * pos * math.pow((h*h - sqr), 2);
		return result;
	}
	private float Poly6Laplacian(float h, float sqr)
	{
		float result = -945.0f / (32.0f * PI * math.pow(h,9)) * (h*h - sqr) 
				* (3.0f * h*h - 7.0f * sqr);
		return result;
	}

	private string ToStringFloat3(float3 val)
	{
		return val.x + " " + val.y + " " + val.z;
	}
}
