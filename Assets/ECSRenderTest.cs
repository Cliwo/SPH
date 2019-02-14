using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Transforms;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Rendering;

public static class ECSRenderTest {

	// Use this for initialization
	// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Start () 
	{
		Debug.Log("Start");
		EntityManager manager = World.Active.GetOrCreateManager<EntityManager>();
		EntityArchetype sampleTypes = manager.CreateArchetype(
			typeof(Unity.Transforms.Position)

		);
		GameObject prefab = GameObject.FindObjectOfType<MeshInstanceRendererComponent>().gameObject;
		Mesh mesh_m = prefab.GetComponent<MeshInstanceRendererComponent>().Value.mesh;
		Material material_m = prefab.GetComponent<MeshInstanceRendererComponent>().Value.material;
		
		MeshInstanceRenderer renderer = new MeshInstanceRenderer {
				mesh = mesh_m,
				material = material_m,
				subMesh = 0 ,
				castShadows = ShadowCastingMode.Off,
				receiveShadows = false
			};

		for(int i = 0 ; i< 3; i ++)
		{
			var sampleEntity = manager.CreateEntity(sampleTypes);
			manager.SetComponentData(sampleEntity, new Unity.Transforms.Position { Value = new Unity.Mathematics.float3(0, i, 0)});
			manager.AddSharedComponentData<MeshInstanceRenderer>(sampleEntity, renderer);
		}
		
	}
	
	
}
