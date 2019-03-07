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

		int hash = GridHash.Hash(position, cellRadius); //cellRadius에 기반한 position을 mapping시킨 hash를 가져온다.
		hashMap.Add(hash, index); //hash와 particle의 index (id?)를 저장한다.

		positions[index] = new Position { Value = position }; // 왜 이걸 저장해? 원본이랑 같은데? 그냥 규칙인가 중요하지 않은 듯.
	}
}
