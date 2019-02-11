using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGenerator : MonoBehaviour {
	public ParticleManager m;
	public GameObject particle;	
	public int GenerateCount;
	

	// Use this for initialization
	void Start () 
	{
		GetComponent<MeshRenderer>().enabled = false;
		Vector3 center = transform.position;
		Vector3 extent = transform.localScale * 0.1f;

		HashSet<Vector3> set = new HashSet<Vector3>();	
		for(int i = 0 ; i < 5; i ++)
		{
			for (int j = 0 ; j< 5; j++)
			{
				for (int k = 0; k < 5; k++)
				{
					Vector3 pos = new Vector3(i * 0.015f, j * 0.015f, k * 0.015f);
					pos += center;
					set.Add(pos);
				}
			}
		}
		foreach(Vector3 pos in set)
		{
			GameObject g = Instantiate(particle, pos, Quaternion.identity);
			g.transform.parent = transform;
			Particle p = g.GetComponent<Particle>();
			p.mass = 0.02f; 
			m.particles.Add(p);
		}
	}
	
	
}
