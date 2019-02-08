using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ParticleManager : MonoBehaviour {

	public int Steps;
	public Vector3 gravity = new Vector3(0.0f, -9.8f, 0f);
	[HideInInspector]
	public List<Particle> particles;
	const float smoothingLevel = 0.457f;
	const float TensionThreshold = 7.065f;
	float h { get {return smoothingLevel;}}
	void Update () 
	{
		for(int i = 0 ; i< Steps; i++)
		{
			particles.ForEach((p) => p.ClearForce());
			particles.ForEach((p) => UpdateDensity(p)); //중요. 매번 처음에 density 업데이트를 해야함.
			particles.ForEach((p) => UpdateColorField(p));

			// particles.ForEach((p) => p.AddForce(CalcPressure(p)));
			// particles.ForEach((p) => p.AddForce(CalcViscosity(p)));
			particles.ForEach((p) => p.AddForce(CalcSurfaceTension(p, TensionThreshold)));
			particles.ForEach((p) => p.AddForce(p.mass * gravity)); //Gravity

			particles.ForEach((p) => p.Apply(Time.deltaTime / Steps));
		}
	}
	
	void UpdateDensity(Particle p)
	{
		//TODO : 업데이트시 가장 좋은건 kernel의 0조건을 먼저 판단하는 것임! (계산 처음부터 안하게) 시간나서 최적화 할 때 고려해보기
		p.density = particles.Sum((j) => j.mass * SmoothKernel_Poly6(p.transform.position - j.transform.position, h));
	}
	void UpdateColorField(Particle p) //항상 Density update이후에 호출되어야한다.
	{
		p.colorField = particles.Sum((j) => j.mass / j.density * SmoothKernel_Poly6(p.transform.position - j.transform.position, h));
	}

	Vector3 CalcPressure(Particle p)
	{
		Vector3 force = Vector3.zero;
		float pressure_I = p.pressure;
		foreach(Particle j in particles)
		{
			float pressure_J = j.pressure; 
			force += -0.5f * j.mass / j.density * ( pressure_I + pressure_J ) * SmoothKernel_Spiky_Gradient(p.transform.position - j.transform.position , h);
		}
		return force;
	}

	Vector3 CalcViscosity(Particle p)
	{
		const float viscosityCoefficient = 3.5f; //0.79722f 0도, mPa /s 단위 (밀리파스칼, s는 뭔지 모르겠)
		//3.5f Ns/m^2
		Vector3 force = Vector3.zero;
		foreach(Particle j in particles)
		{
			force += j.mass / (j.density) * (j.velocity - p.velocity) * SmoothKernel_Viscosity_Laplacian(p.transform.position - j.transform.position, h);
		}
		force *= viscosityCoefficient;
		return force;
	}

	Vector3 CalcSurfaceTension(Particle p, float threshold) // TODO : 최적화 고려해보기 
	{
		const float sigma = 0.07197f; // 0.0719-> N/m 단위  71.97mN/m (밀리뉴턴, 미터 단위) (25도에서)) 
		Vector3 n = Vector3.zero;		
		foreach(Particle j in particles)
		{
			Vector3 result = j.mass / j.density * SmoothKernel_Poly6_Gradient(p.transform.position - j.transform.position, h);
			n += result;
		}
		float magnitude = n.magnitude;
		if(magnitude < threshold)
		{
			return Vector3.zero; //n의 magnitude가 너무 작음, 힘을 계산하지 않는다.
		}

		float laplacian = 0.0f;
		foreach(Particle j in particles)
		{
			laplacian = j.mass / j.density * SmoothKernel_Poly6_Laplacian(p.transform.position - j.transform.position, h);
		}
		float kappa = -laplacian / magnitude;
		return (sigma * kappa * n);
	}

	float SmoothKernel_Poly6(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			float result = 315 / (64 * Mathf.PI * Mathf.Pow(h,9.0f)) * Mathf.Pow((h*h - position.sqrMagnitude),3.0f);
			return result;
		}
		return 0.0f;
	}
	Vector3 SmoothKernel_Poly6_Gradient(Vector3 position, float h)
	{
		if(position.sqrMagnitude <= h * h)
		{
			return - 945 / (32*Mathf.PI*Mathf.Pow(h,9.0f)) * position * Mathf.Pow((h*h - position.sqrMagnitude), 2.0f);
		}
		return Vector3.zero;
	}

	float SmoothKernel_Poly6_Laplacian(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			return 945 / (32 * Mathf.PI * Mathf.Pow(h, 9.0f)) *(-1 * Mathf.Pow((h*h - position.sqrMagnitude),2.0f) + 
			4 * position.magnitude*(h*h - position.sqrMagnitude));
		}
		return 0.0f;
	}

	
	Vector3 SmoothKernel_Spiky_Gradient(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			return - (45 / Mathf.PI / Mathf.Pow(h,6.0f) * Mathf.Pow(h - position.magnitude, 2.0f) * position.normalized);
			//이거 position이 아니라 normailzed position 인것 같음 !!!! := 맞는듯! normalized 로 쓰는 다른 코드도 발견
			/*
			https://www8.cs.umu.se/kurser/TDBD24/VT06/lectures/sphsurvivalkit.pdf 
			여기서는 마치 그냥 r벡터를 사용하는 것 같이 나오는데 실제로 수식 돌려서 계산해 보면 
			각 x,y,z항이 r을 분모로 갖는다. 즉, 최종적으로 r의 nomalized를 곱해야한다는 것 아닐까? 
			 */
		}
		return Vector3.zero; 
	} 

	float SmoothKernel_Viscosity_Laplacian(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			return 45 / Mathf.PI / Mathf.Pow(h,6.0f) * (h - position.magnitude);
		}
		return 0.0f;
	}
}
