using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
public struct MergeParticles : IJobNativeMultiHashMapMergedSharedKeyIndices
{
	//https://forum.unity.com/threads/how-does-ijobnativemultihashmapmergedsharedkeyindices-work.535104/
	//IJobNativeMultiHashMapMergedSharedKeyIndices 이거가 동작하는 원리를 찾는것이 매우힘들다고 한다. 위의 글이 그나마 설명해주는 글

	//결과적으로만 말하면 이 Job은 결국 파티클에 hash id를 부여하는 것.
    public NativeArray<int> particleIndices;
	public void ExecuteFirst(int index)
	{
		particleIndices[index] = index;
	}

	public void ExecuteNext(int cellIndex, int index)
	{
		particleIndices[index] = cellIndex;
	}
}
