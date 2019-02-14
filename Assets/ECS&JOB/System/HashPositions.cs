using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
[BurstCompile]
public struct HashPositions : IJobParallelFor 
{
     [ReadOnly] public float cellRadius;

	public NativeArray<Position> positions;
	public NativeMultiHashMap<int, int>.Concurrent hashMap;

	public void Execute(int index)
	{
		float3 position = positions[index].Value;

		int hash = GridHash.Hash(position, cellRadius); 
		hashMap.Add(hash, index);

		positions[index] = new Position { Value = position };
	}
}
