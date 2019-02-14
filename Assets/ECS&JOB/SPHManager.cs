using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
public class SPHManager : MonoBehaviour {
	EntityManager manager;

	[Header("Properties")]
	[SerializeField] private int amount;
	[SerializeField] private GameObject sphParticlePrefab;


	// Use this for initialization
	void Start () {
		manager = World.Active.GetOrCreateManager<EntityManager>();

		AddCollider();
		AddParticles(amount);
	}
	
	void AddCollider()
	{
		// 벽을 추가한다. 나는 벽이 코드내에서 작동하므로 필요 없을 듯
	}
	
	void AddParticles(int _amount)
	{
		NativeArray<Entity> entities = new NativeArray<Entity>(_amount, Allocator.Temp);
        manager.Instantiate(sphParticlePrefab, entities);
		int inARow = 8;
        for (int i = 0; i < _amount; i++)
        {
            manager.SetComponentData(entities[i], new Position { Value = new float3(i % inARow + UnityEngine.Random.Range(-0.1f, 0.1f), 2 + (i / inARow / inARow) * 1.1f, (i / inARow) % inARow) + UnityEngine.Random.Range(-0.1f, 0.1f) });
        }

        entities.Dispose();
	}
	void AddParticles()
	{
		Particle[] Particle_Mono = GetComponents<Particle>();
		NativeArray<Entity> entities = new NativeArray<Entity>(Particle_Mono.Length, Allocator.Temp);

		for(int i = 0 ; i < Particle_Mono.Length; i++)
		{
			//TODO : 아래 수정점 있을 수 있음! 미리 만들어진 Particle 들을 모아오고, position 설정 잘 되는지 확인
			manager.SetComponentData(entities[i], new Position {Value = 
			new float3 (Particle_Mono[i].transform.position.x ,
			Particle_Mono[i].transform.position.y,
			Particle_Mono[i].transform.position.z)});
		}

		entities.Dispose();
	}
}

