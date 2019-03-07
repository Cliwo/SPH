using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

public class ParticleGenerator : MonoBehaviour {
	public ParticleManager m;
	public GameObject particle;	
	public int GenerateCount;
	public float offset = 0.015f;

	// Use this for initialization
	void Start () 
	{
		GetComponent<MeshRenderer>().enabled = false;
		
	}	
}


[CustomEditor(typeof(ParticleGenerator)), CanEditMultipleObjects]
public class ParticleGeneratorEditor : Editor {
	ParticleGenerator script;
	bool isGenerated = false;
	private void OnEnable() 
	{
		script = (ParticleGenerator)target;
	}
	public override void OnInspectorGUI() {

		if(GUILayout.Button("Generate Particle"))
		{
			script.GetComponent<MeshRenderer>().enabled = false;
			Vector3 center = script.transform.position;

			HashSet<Vector3> set = new HashSet<Vector3>();	
			int countInRow = script.GenerateCount;
			float offset = script.m.h / 2;
			for(int i = 0 ; i < countInRow; i ++)
			{
				for (int j = 0 ; j< countInRow * 2; j++)
				{
					for (int k = 0; k < countInRow; k++)
					{
						Vector3 pos = new Vector3((i-countInRow/2), (j-countInRow/2), (k-countInRow/2));
						pos *= script.m.h / 2.0f;
						pos += center; 
						set.Add(pos);
					}
				}
			}
			foreach(Vector3 pos in set)
			{
				GameObject g = Instantiate(script.particle, pos, Quaternion.identity);
				g.transform.parent = script.transform;
				Particle p = g.GetComponent<Particle>();
				p.mass = 0.02f; 
				script.m.particles.Add(p);
			}
		}
		if(GUILayout.Button("Delete All"))
		{
			ClearAllParticles();
		}
		DrawDefaultInspector();
	}

	private void ClearAllParticles()
	{
		script.GetComponent<MeshRenderer>().enabled = true;
		Transform t = script.transform.childCount != 0 ? script.transform.GetChild(0) : null;
		while(t!= null)
		{
			DestroyImmediate(t.gameObject);
			t = script.transform.childCount != 0 ? script.transform.GetChild(0) : null;
		}
		script.m.particles.Clear();

	}
}
