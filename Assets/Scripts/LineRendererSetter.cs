using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererSetter : MonoBehaviour {
	void OnDrawGizmos() {

		Gizmos.DrawWireCube(new Vector3(0, Particle.widthHalf, 0),
			new Vector3(Particle.width,Particle.width,Particle.width));
		
	}
}
