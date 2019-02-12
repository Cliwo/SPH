using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Particle : MonoBehaviour { //Mono일 필요가 없을 듯
	private MeshRenderer m_render;
	private static Vector3 bottomFloorNormal = new Vector3(0.0f, 1.0f, 0.0f);
	private static Vector3 rightFloorNormal = new Vector3(-1.0f, 0.0f, 0.0f);
	private static Vector3 leftFloorNormal = new Vector3(1.0f, 0.0f, 0.0f);
	private static Vector3 topFloorNormal = new Vector3(0.0f, -1.0f, 0.0f);
	private static Vector3 nearFloorNormal = new Vector3(0.0f, 0.0f, 1.0f);
	private static Vector3 farFloorNormal = new Vector3(0.0f, 0.0f, -1.0f);
	
	public const float width = 0.5f;
	public const float widthHalf = width * 0.5f;
	private static Vector3 bottomFloorPosition = new Vector3(0.0f, 0.0f, 0.0f);
	private static Vector3 topFloorPosition = new Vector3(0.0f, width, 0.0f);
	private static Vector3 rightFloorPosition = new Vector3(widthHalf, widthHalf, 0.0f);
	private static Vector3 leftFloorPosition = new Vector3(-widthHalf, widthHalf, 0.0f);
	private static Vector3 nearFloorPosition = new Vector3(0.0f, widthHalf, -widthHalf);
	private static Vector3 farFloorPosition = new Vector3(0.0f, widthHalf, widthHalf);

	public bool surfaceFlag = false;
	public float colorField;
	public float density;
	public float mass;
	public float pressure
	{
		get 
		{
			const float restDensity = 998.29f; // TODO : 이거가 올바른건가? https://scicomp.stackexchange.com/questions/14450/how-to-get-proper-parameters-of-sph-simulation
			//1.00f : kg/m^3  , 998.29 kg/m^3
			const float gasConstant = 3.0f; // TODO : https://ko.wikipedia.org/wiki/기체_상수 (혹시 문제 생기면 단위 확인 필요)
			//8.31432 : N * m / (mol * K) , 3.0 Nm/kg
			return gasConstant * (density - restDensity);
		}
	}
	public Vector3 force;
	public Vector3 velocity;
	void Awake() 
	{
		m_render = gameObject.GetComponent<MeshRenderer>();
	}
	public void AddForce(Vector3 force)
	{
		this.force += force;
	}
	public void ClearForce()
	{
		this.force = Vector3.zero;
	}

	public float alpha = 0.7f; //반발계수
	public void Apply(float deltaTime)
	{
		this.transform.position += this.velocity * deltaTime;
		this.velocity += force / this.density * deltaTime;

		if(surfaceFlag)
		{
			m_render.material.color = Color.red;
		}
		else
		{
			m_render.material.color = Color.blue;
		}

		CalcWallCollision(bottomFloorNormal, bottomFloorPosition);
		// CalcWallCollision(topFloorNormal , topFloorPosition);
		CalcWallCollision(leftFloorNormal, leftFloorPosition);
		CalcWallCollision(rightFloorNormal, rightFloorPosition);
		CalcWallCollision(nearFloorNormal, nearFloorPosition);
		CalcWallCollision(farFloorNormal, farFloorPosition);
	}

	private void CalcWallCollision(Vector3 floorNormal, Vector3 floorPosition)
	{
		if (Vector3.Dot(floorNormal, this.transform.position - floorPosition) < float.Epsilon && Vector3.Dot(this.velocity, floorNormal) < 0) //Collision
		{
			Vector3 vn = Vector3.Dot(floorNormal, this.velocity) * floorNormal;
			Vector3 vt = this.velocity - vn;
			vt *= 0.01f; 
			this.velocity = vt - alpha * vn;
			if (Vector3.Dot(this.transform.position - floorPosition, floorNormal) < 0)
				this.transform.position -= Vector3.Dot(this.transform.position - floorPosition, floorNormal) * floorNormal;
		}
		else if (Vector3.Dot(floorNormal, this.transform.position - floorPosition) < float.Epsilon) //Contact
		{
			Vector3 vn = Vector3.Dot(floorNormal, this.transform.position) * floorNormal;
			Vector3 vt = this.transform.position - vn;
			vt *= 0.01f;  
			this.velocity = vt + vn;
		}
	}

}
