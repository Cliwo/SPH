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
		while(set.Count < GenerateCount)
		{
			float x = Random.Range(center.x - extent.x , center.x + extent.x);
			float y = Random.Range(center.y - extent.y , center.y + extent.y);
			float z = Random.Range(center.z - extent.z , center.z + extent.z);

			set.Add(new Vector3(x, y, z));
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
