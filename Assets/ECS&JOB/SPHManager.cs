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
	[SerializeField] private int inARow;
	[SerializeField] private GameObject sphParticlePrefab;
	[SerializeField] private GameObject sphColliderPrefab;


	// Use this for initialization
	void Start () {
		manager = World.Active.GetOrCreateManager<EntityManager>();

		AddCollider();
		AddParticles(amount);
	}
	
	void AddCollider()
	{
		// Find all colliders
        GameObject[] colliders = GameObject.FindGameObjectsWithTag("SPHCollider");

        // Turn them into entities
        NativeArray<Entity> entities = new NativeArray<Entity>(colliders.Length, Allocator.Temp);
        manager.Instantiate(sphColliderPrefab, entities);

        // Set data
        for (int i = 0; i < colliders.Length; i++)
        {
            manager.SetComponentData(entities[i], new SPHCollider
            {
                position = colliders[i].transform.position,
                right = colliders[i].transform.right,
                up = colliders[i].transform.up,
                scale = new float2(colliders[i].transform.localScale.x / 2f, colliders[i].transform.localScale.y / 2f)
            });
        }

        // Done
        entities.Dispose();
	}
	
	void AddParticles(int _amount)
	{
		float scaleVal = 0.015f;
		NativeArray<Entity> entities = new NativeArray<Entity>(_amount, Allocator.Temp);
        manager.Instantiate(sphParticlePrefab, entities);
        for (int i = 0; i < _amount; i++)
        {
			float3 pos = new float3(i % inARow , 
			2 + (i / inARow / inARow),
			(i / inARow) % inARow);
			pos *= scaleVal * 2.0f;
            manager.SetComponentData(entities[i], new Position { Value = pos});
			manager.SetComponentData(entities[i], new Scale { Value = new float3(scaleVal)});
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

