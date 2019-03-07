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

	// private static readonly int[] cellOffsetTable =
    // {
    //     1, 1, 1, 1, 1, 0, 1, 1, -1, 1, 0, 1, 1, 0, 0, 1, 0, -1, 1, -1, 1, 1, -1, 0, 1, -1, -1,
    //     0, 1, 1, 0, 1, 0, 0, 1, -1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, -1, 1, 0, -1, 0, 0, -1, -1,
    //     -1, 1, 1, -1, 1, 0, -1, 1, -1, -1, 0, 1, -1, 0, 0, -1, 0, -1, -1, -1, 1, -1, -1, 0, -1, -1, -1
    // }; 
	public static int[] cellOffsetTable;
	
	protected override void OnCreateManager()
	{
		List<int> offsetList = new List<int>();
		int wide = 3;
		for(int i = -wide ; i <= wide; i++)
		{
			for(int j = -wide ; j <= wide ; j++)
			{
				for(int k = -wide ; k <= wide ; k++)
				{
					offsetList.Add(i); offsetList.Add(j); offsetList.Add(k);
				}
			}
		}
		cellOffsetTable = offsetList.ToArray();

		SPHCharacterGroup = GetComponentGroup(ComponentType.ReadOnly(typeof(SPHParticle)), typeof(Position), typeof(SPHVelocity));
		//position 과 velocity는 수정해야해서 readonly 아닌듯?
		SPHColliderGroup = GetComponentGroup(ComponentType.ReadOnly(typeof(SPHCollider)));
	}
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		EntityManager.GetAllUniqueSharedComponentData(uniqueTypes);
		//Unique한 Shared Data이다. 우리가 쓰는 Data중에 Shared Data는 SPHParticle (settings) 밖에 없다 
		ComponentDataArray<SPHCollider> colliders = SPHColliderGroup.GetComponentDataArray<SPHCollider>();
		int colliderCount = colliders.Length;
		//collider 부분 안함. 
		for (int typeIndex = 1; typeIndex < uniqueTypes.Count; typeIndex++)
		{
			/* Part 1 : 데이터 캐싱 */
			//데이터를 캐싱한다.
			//Get the current chunk setting
			SPHParticle settings = uniqueTypes[typeIndex];
			SPHCharacterGroup.SetFilter(settings); //Filter가 뭔데? , 아 SPHParticle은 제외하고 다른 Component를 보겠다는건가? 

			// Cache the data
			ComponentDataArray<Position> positions = SPHCharacterGroup.GetComponentDataArray<Position>(); //현재 모든 파티클의 Position
			ComponentDataArray<SPHVelocity> velocities = SPHCharacterGroup.GetComponentDataArray<SPHVelocity>(); //현재 모든 파티클의 Velocity

			int cacheIndex = typeIndex - 1;
			int particleCount = positions.Length;

			NativeMultiHashMap<int, int> hashMap = new NativeMultiHashMap<int, int>(particleCount, Allocator.TempJob); //Hash
			// 성능향상을 위한 HashMap

			// PreviousParticle을 채우기 위해 미리 생성한 데이터 공간이다. 생성하면서 data를 채워주지 않기 떄문에 메모리만 할당된 상태이다. 
			NativeArray<Position> particlesPosition = new NativeArray<Position>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<SPHVelocity> particlesVelocity = new NativeArray<SPHVelocity>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<float3> particlesForces = new NativeArray<float3>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<float> particlesPressure = new NativeArray<float>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<float> particlesDensity = new NativeArray<float>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			NativeArray<int> particleIndices = new NativeArray<int>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			NativeArray<int> cellOffsetTableNative = new NativeArray<int>(cellOffsetTable, Allocator.TempJob);
			NativeArray<SPHCollider> copyColliders = new NativeArray<SPHCollider>(colliderCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			/* Part 2 : PreviousParticle 생성 */
			PreviousParticle nextParticles = new PreviousParticle //PreviousParticle 은 그냥 일반적인 struct이다. Component가 아님. 
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

			if (cacheIndex > previousParticles.Count - 1) //cacheIndex는 현재 확인하고 있는 UniqueSharedComponent의 index - 1값이다. 즉 0부터 1씩 증가한다. 
			{
				previousParticles.Add(nextParticles); 
			}
			else //파티클을 previousParticles에 삽입하지 않은 경우
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
			previousParticles[cacheIndex] = nextParticles;


			/* Part 3 : Job 생성 (?) */
			// Copy the component data to native arrays
			// Position 과 Velocity는 이전결과에서 수정하는 것이라서 복사하고, Pressure, Density, Indices, Force는 새로 계산할것이기 떄문에 초기화만 해준다. 
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
			//Hash를 하기 위해선 position 정보가 필요하다, position 정보와 sync를 맞춘다. 

			JobHandle mergedPositionIndicesJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, particleIndicesJobHandle);
			//Hash하고나서 index를 쓸 거라서 index와 sync를 맞춘다. 

			MergeParticles mergeParticlesJob = new MergeParticles
			{
				particleIndices = particleIndices
			};
			JobHandle mergeParticlesJobHandle = mergeParticlesJob.Schedule(hashMap, 64, mergedPositionIndicesJobHandle);
			/// ![확실하진않음] : Hash에서 index와 hash를 연결했다, 이 때 index는 Particle과는 mapping되어 있지 않은 그저 실행 순서 index였나보다. 위의 MergeParticles를 통해서
			/// particle이 각자의 index를 갖게된다. 이 떄문에 이미 index와 연결된 Hash가 particle과 연결된다. 

			//이제 hash가 끝났으므로 Density, Pressure, Force를 계산할 시간. 따라서 sync를 맞춘다. 
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
                settings = settings,
				gameTime = Time.time
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
                settings = settings,
				gameTime = Time.time
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
