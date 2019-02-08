using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {

	private void OnTriggerStay(Collider other) 
	{
		Debug.Log(other.name);
		Particle s = other.GetComponent<Particle>();
		if(s != null)
		{
			if(s.force.y < float.Epsilon) //운동 방향을 가짜로 표현, 떨어지고 있는 경우에만 충돌
				s.AddForce(new Vector3(0.0f , s.force.y * -1.9f, 0.0f)); //마찰을 가짜로 표현
		}
	}
	private void OnTriggerEnter(Collider other) 
	{
		Debug.Log(other.name);
		Particle s = other.GetComponent<Particle>();
		if(s != null)
		{
			if(s.force.y < float.Epsilon) //운동 방향을 가짜로 표현, 떨어지고 있는 경우에만 충돌
				s.AddForce(new Vector3(0.0f , s.force.y * -1.9f, 0.0f)); //마찰을 가짜로 표현
		}
	}
}
