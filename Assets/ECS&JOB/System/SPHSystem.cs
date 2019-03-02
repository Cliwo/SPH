using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
//https://github.com/leonardo-montes/Unity-ECS-Job-System-SPH/blob/master/Assets/Job%20System/SPHSystem.cs
public class SPHSystem : JobComponentSystem 
{
	ComponentGroup SPHCharacterGroup;
	ComponentGroup SPHColliderGroup;
	List<SPHParticle> uniqueTypes = new List<SPHParticle>(10);
	List<PreviousParticle> previousParticles = new List<PreviousParticle>();

	private static readonly int[] cellOffsetTable =
    {
        1, 1, 1, 1, 1, 0, 1, 1, -1, 1, 0, 1, 1, 0, 0, 1, 0, -1, 1, -1, 1, 1, -1, 0, 1, -1, -1,
        0, 1, 1, 0, 1, 0, 0, 1, -1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, -1, 1, 0, -1, 0, 0, -1, -1,
        -1, 1, 1, -1, 1, 0, -1, 1, -1, -1, 0, 1, -1, 0, 0, -1, 0, -1, -1, -1, 1, -1, -1, 0, -1, -1, -1
    };

	protected override void OnCreateManager()
	{
		SPHCharacterGroup = GetComponentGroup(ComponentType.ReadOnly(typeof(SPHParticle)), typeof(Position), typeof(SPHVelocity));
		//position 과 velocity는 수정해야해서 readonly 아닌듯?
		SPHColliderGroup = GetComponentGroup(ComponentType.ReadOnly(typeof(SPHCollider)));
	}
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		EntityManager.GetAllUniqueSharedComponentData(uniqueTypes);

		ComponentDataArray<SPHCollider> colliders = SPHColliderGroup.GetComponentDataArray<SPHCollider>();
		int colliderCount = colliders.Length;
		//collider 부분 안함. 
		for (int typeIndex = 1; typeIndex < uniqueTypes.Count; typeIndex++)
		{
			/* Part 1 : 데이터 캐싱 */
			//데이터를 캐싱한다.
			//Get the current chunk setting
			SPHParticle settings = uniqueTypes[typeIndex];
			SPHCharacterGroup.SetFilter(settings); //Filter가 뭔데? 

			// Cache the data
			ComponentDataArray<Position> positions = SPHCharacterGroup.GetComponentDataArray<Position>();
			ComponentDataArray<SPHVelocity> velocities = SPHCharacterGroup.GetComponentDataArray<SPHVelocity>();

			int cacheIndex = typeIndex - 1;
			int particleCount = positions.Length;

			NativeMultiHashMap<int, int> hashMap = new NativeMultiHashMap<int, int>(particleCount, Allocator.TempJob);
			// 성능향상을 위한 HashMap

			NativeArray<Position> particlesPosition = new NativeArray<Position>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<SPHVelocity> particlesVelocity = new NativeArray<SPHVelocity>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<float3> particlesForces = new NativeArray<float3>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<float> particlesPressure = new NativeArray<float>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<float> particlesDensity = new NativeArray<float>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<int> particleIndices = new NativeArray<int>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			NativeArray<int> cellOffsetTableNative = new NativeArray<int>(cellOffsetTable, Allocator.TempJob);
			NativeArray<SPHCollider> copyColliders = new NativeArray<SPHCollider>(colliderCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			/* Part 2 : PreviousParticle 생성 */
			PreviousParticle nextParticles = new PreviousParticle
			{
				hashMap = hashMap,
				particlesPosition = particlesPosition,
				particlesVelocity = particlesVelocity,
				particlesForces = particlesForces,
				particlesPressure = particlesPressure,
				particlesDensity = particlesDensity,
				particleIndices = particleIndices,
				cellOffsetTable = cellOffsetTableNative,
				copyColliders = copyColliders
			};

			if (cacheIndex > previousParticles.Count - 1)
			{
				previousParticles.Add(nextParticles);
			}
			else
			{
				previousParticles[cacheIndex].hashMap.Dispose();
				previousParticles[cacheIndex].particlesPosition.Dispose();
				previousParticles[cacheIndex].particlesVelocity.Dispose();
				previousParticles[cacheIndex].particlesForces.Dispose();
				previousParticles[cacheIndex].particlesPressure.Dispose();
				previousParticles[cacheIndex].particlesDensity.Dispose();
				previousParticles[cacheIndex].particleIndices.Dispose();
				previousParticles[cacheIndex].cellOffsetTable.Dispose();
				previousParticles[cacheIndex].copyColliders.Dispose();
			}
			// 의문 : 지금 생성하는 방식이 미리 메모리 할당을 다하고 (new)에서, 조건을 보고 Count가 Index를 넘어서면 미리 생성한걸 해제하는 것 같은데 
			// 그렇게 안하고 조건을 미리보고 조건에 맞으면 생성하면 안됨?? 코드가 더럽지 않음 이러면?
			previousParticles[cacheIndex] = nextParticles;


			/* Part 3 : Job 생성 (?) */
			// Copy the component data to native arrays
			CopyComponentData<Position> particlesPositionJob = new CopyComponentData<Position> { Source = positions, Results = particlesPosition };
			JobHandle particlesPositionJobHandle = particlesPositionJob.Schedule(particleCount, 64, inputDeps);

			CopyComponentData<SPHVelocity> particlesVelocityJob = new CopyComponentData<SPHVelocity> { Source = velocities, Results = particlesVelocity };
			JobHandle particlesVelocityJobHandle = particlesVelocityJob.Schedule(particleCount, 64, inputDeps);

			CopyComponentData<SPHCollider> copyCollidersJob = new CopyComponentData<SPHCollider> { Source = colliders, Results = copyColliders };
			JobHandle copyCollidersJobHandle = copyCollidersJob.Schedule(colliderCount, 64, inputDeps);

			MemsetNativeArray<float> particlesPressureJob = new MemsetNativeArray<float> { Source = particlesPressure, Value = 0.0f };
			JobHandle particlesPressureJobHandle = particlesPressureJob.Schedule(particleCount, 64, inputDeps);

			MemsetNativeArray<float> particlesDensityJob = new MemsetNativeArray<float> { Source = particlesDensity, Value = 0.0f };
			JobHandle particlesDensityJobHandle = particlesDensityJob.Schedule(particleCount, 64, inputDeps);

			MemsetNativeArray<int> particleIndicesJob = new MemsetNativeArray<int> { Source = particleIndices, Value = 0 };
			JobHandle particleIndicesJobHandle = particleIndicesJob.Schedule(particleCount, 64, inputDeps);

			MemsetNativeArray<float3> particlesForcesJob = new MemsetNativeArray<float3> { Source = particlesForces, Value = new float3(0, 0, 0) };
			JobHandle particlesForcesJobHandle = particlesForcesJob.Schedule(particleCount, 64, inputDeps);


			/* Part 4 : Hash를 이용한 Optimization, Optimization을 하기위해 필요한 Job 세팅하기 */
			// Put positions into a hashMap
			HashPositions hashPositionsJob = new HashPositions
			{
				positions = particlesPosition,
				hashMap = hashMap.ToConcurrent(),
				cellRadius = settings.Radius
			};
			JobHandle hashPositionsJobHandle = hashPositionsJob.Schedule(particleCount, 64, particlesPositionJobHandle);

			JobHandle mergedPositionIndicesJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, particleIndicesJobHandle);

			MergeParticles mergeParticlesJob = new MergeParticles
			{
				particleIndices = particleIndices
			};
			JobHandle mergeParticlesJobHandle = mergeParticlesJob.Schedule(hashMap, 64, mergedPositionIndicesJobHandle);

			JobHandle mergedMergedParticlesDensityPressure = JobHandle.CombineDependencies(mergeParticlesJobHandle, particlesPressureJobHandle, particlesDensityJobHandle);

			/* Part 5 : 드디어 SPH를 해결하는 파트 */
			// Compute density pressure
            ComputeDensityPressure computeDensityPressureJob = new ComputeDensityPressure
            {
                particlesPosition = particlesPosition,
                densities = particlesDensity,
                pressures = particlesPressure,
                hashMap = hashMap,
                cellOffsetTable = cellOffsetTableNative,
                settings = settings
            };
            JobHandle computeDensityPressureJobHandle = computeDensityPressureJob.Schedule(particleCount, 64, mergedMergedParticlesDensityPressure);

            JobHandle mergeComputeDensityPressureVelocityForces = JobHandle.CombineDependencies(computeDensityPressureJobHandle, particlesForcesJobHandle, particlesVelocityJobHandle);

            // Compute forces
            ComputeForces computeForcesJob = new ComputeForces
            {
                particlesPosition = particlesPosition,
                particlesVelocity = particlesVelocity,
                particlesForces = particlesForces,
                particlesPressure = particlesPressure,
                particlesDensity = particlesDensity,
                cellOffsetTable = cellOffsetTableNative,
                hashMap = hashMap,
                settings = settings
            };
            JobHandle computeForcesJobHandle = computeForcesJob.Schedule(particleCount, 64, mergeComputeDensityPressureVelocityForces);

            // Integrate
            Integrate integrateJob = new Integrate
            {
				timeStep = settings.TimeStep,
                particlesPosition = particlesPosition,
                particlesVelocity = particlesVelocity,
                particlesDensity = particlesDensity,
                particlesForces = particlesForces
            };
            JobHandle integrateJobHandle = integrateJob.Schedule(particleCount, 64, computeForcesJobHandle);

			/* Part 6 : Collider 와 Apply Positions 파트, 나는 Collider 부분은 제외해버림  */
			JobHandle mergedIntegrateCollider = JobHandle.CombineDependencies(integrateJobHandle, copyCollidersJobHandle);

			// Compute Colliders
			ComputeColliders computeCollidersJob = new ComputeColliders
			{
				particlesPosition = particlesPosition,
				particlesVelocity = particlesVelocity,
				copyColliders = copyColliders,
				settings = settings
			};
			JobHandle computeCollidersJobHandle = computeCollidersJob.Schedule(particleCount, 64, mergedIntegrateCollider);

			// Apply positions
			ApplyPositions applyPositionsJob = new ApplyPositions
			{
				particlesPosition = particlesPosition,
				particlesVelocity = particlesVelocity,
				positions = positions,
				velocities = velocities
			};
			JobHandle applyPositionsJobHandle = applyPositionsJob.Schedule(particleCount, 64, computeCollidersJobHandle);
			// JobHandle applyPositionsJobHandle = applyPositionsJob.Schedule(particleCount, 64, integrateJobHandle);
			// 이 부분 주의! : 내가 Collider가 없으니까 dependency가 없다고 판단하고 computeCollidersJobHandle 부분을 임의로 날려버렸음. 혹시 에러나면 주의

			inputDeps = applyPositionsJobHandle;
		}

		uniqueTypes.Clear();
		return inputDeps;
	}


	protected override void OnStopRunning()
	{
		for (int i = 0; i < previousParticles.Count; i++)
		{
			previousParticles[i].hashMap.Dispose();
			previousParticles[i].particlesPosition.Dispose();
			previousParticles[i].particlesVelocity.Dispose();
			previousParticles[i].particlesForces.Dispose();
			previousParticles[i].particlesPressure.Dispose();
			previousParticles[i].particlesDensity.Dispose();
			previousParticles[i].particleIndices.Dispose();
			previousParticles[i].cellOffsetTable.Dispose();
			previousParticles[i].copyColliders.Dispose();
		}

		previousParticles.Clear();
	}
}
