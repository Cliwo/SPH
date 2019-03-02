using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
public class SPHManager : MonoBehaviour {
	EntityManager manager;

	[Header("Properties")]
	 public int amount;
	 public int inARow;
	public GameObject sphParticlePrefab;
	public GameObject sphColliderPrefab;


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
		NativeArray<Entity> entities = new NativeArray<Entity>(_amount, Allocator.Temp);
		SPHParticle setting = sphParticlePrefab.GetComponent<SPHParticleComponent>().Value;

        manager.Instantiate(sphParticlePrefab, entities);
        for (int i = 0; i < _amount; i++)
        {
			Position pos = new Position { Value = new float3(i % inARow + UnityEngine.Random.Range(-0.1f, 0.1f), 2 + (i / inARow / inARow) * 1.1f, (i / inARow) % inARow) + UnityEngine.Random.Range(-0.1f, 0.1f)};
			pos.Value *= setting.Radius;
            manager.SetComponentData(entities[i], pos);
			manager.SetComponentData(entities[i], new Scale{ Value = new float3(setting.Radius) });
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

[CustomEditor(typeof(SPHManager)), CanEditMultipleObjects]
public class SPHManagerEditor : Editor {
	SPHManager script;
	bool isGenerated = false;
	List<GameObject> env;
	private void OnEnable() 
	{
		env = new List<GameObject>();
		script = (SPHManager)target;
	}
	public override void OnInspectorGUI() {

		if(GUILayout.Button("Set Environments"))
		{
			const float padding = 3.0f;
			SPHParticle setting = script.sphParticlePrefab.GetComponent<SPHParticleComponent>().Value;
			float x, y;
			y = (script.amount / script.inARow / script.inARow) * setting.Radius * padding;
			x = script.inARow * setting.Radius * padding;
			
			env.ForEach((item) => {DestroyImmediate(item);});
			env.Clear();

			Vector3 upDownScale = new Vector3(x,x, 1);
			Vector3 sideScale = new Vector3(x,y, 1);

			for(int i = 0 ; i < 6; i ++)
			{
				env.Add(Instantiate<GameObject>(script.sphColliderPrefab));
			}

			env[0].transform.localScale = upDownScale;
			env[0].transform.position = new Vector3(env[0].transform.position.x, env[0].transform.position.y - y/2, env[0].transform.position.z);
			
			env[1].transform.localScale = upDownScale;
			env[1].transform.position = new Vector3(env[1].transform.position.x, env[1].transform.position.y + y/2, env[1].transform.position.z);
			env[1].transform.rotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
			
			env[2].transform.localScale = sideScale;
			env[3].transform.localScale = sideScale;
			env[4].transform.localScale = sideScale;
			env[5].transform.localScale = sideScale;

			env[2].transform.Translate(new Vector3(-x/2.0f,0,0));
			env[2].transform.rotation = Quaternion.Euler(0.0f, -90.0f, 0.0f);
			env[3].transform.Translate(new Vector3(+x/2.0f,0,0));
			env[3].transform.rotation = Quaternion.Euler(0.0f, +90.0f, 0.0f);
			env[4].transform.rotation = Quaternion.Euler(0.0f, -180.0f, 0.0f);
			env[4].transform.Translate(new Vector3(0,0,+x/2.0f));
			env[5].transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			env[5].transform.Translate(new Vector3(0,0,+x/2.0f));
			
		}
		DrawDefaultInspector();
	}
}