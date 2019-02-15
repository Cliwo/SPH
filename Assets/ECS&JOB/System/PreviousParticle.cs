using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
public struct PreviousParticle {
	public NativeMultiHashMap<int, int> hashMap;
	public NativeArray<Position> particlesPosition;
	public NativeArray<SPHVelocity> particlesVelocity;
	public NativeArray<float3> particlesForces;
	public NativeArray<float> particlesPressure;
	public NativeArray<float> particlesDensity;
	public NativeArray<int> particleIndices;

	public NativeArray<int> cellOffsetTable;
	public NativeArray<SPHCollider> copyColliders;
}
