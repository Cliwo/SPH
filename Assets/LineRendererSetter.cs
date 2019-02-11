using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererSetter : MonoBehaviour {

	public LineRenderer lR;
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(new Vector3(0, Particle.widthHalf, 0),
			new Vector3(Particle.width,Particle.width,Particle.width));
		
	}
}

		// Vector3 BLN, BLF, BRN, BRF;
		// Vector3 TLN, TLF, TRN, TRF;

		// BLN = new Vector3(-Particle.widthHalf,0,-Particle.widthHalf);
		// BLF = new Vector3(-Particle.widthHalf,0,Particle.widthHalf);
		// BRN = new Vector3(Particle.widthHalf,0,-Particle.widthHalf);
		// BRF = new Vector3(Particle.widthHalf,0,Particle.widthHalf);

		// TLN = new Vector3(-Particle.widthHalf,Particle.width,-Particle.widthHalf);
		// TLF = new Vector3(-Particle.widthHalf,Particle.width,Particle.widthHalf);
		// TRN = new Vector3(Particle.widthHalf,Particle.width,-Particle.widthHalf);
		// TRF = new Vector3(Particle.widthHalf,Particle.width,Particle.widthHalf);
