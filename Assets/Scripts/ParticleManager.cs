using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ParticleManager : MonoBehaviour {
	public bool ApplyPressure = true;
	public bool ApplyViscosity = true;
	public bool ApplySurfaceTension = true;
	public bool ApplyGravity = true;

	public float PressureCoef = 0.000008f;
	public float ViscosityCoef = 0.0000001f;
	public float SurfaceTensionCoef = 0.0000001f;
	public float GravityCoef = 10.0f;
	public float Steps;
	[HideInInspector]
	public Vector3 gravity = new Vector3(0.0f, -9.8f, 0f);
	[HideInInspector]
	public List<Particle> particles;
	const float smoothingLevel = 0.0457f;
	const float TensionThreshold = 7.065f;
	public float h { get {return smoothingLevel;}}

	void FixedUpdate() 
	{
		particles.ForEach((p) => p.ClearAcceleration());
		particles.ForEach((p) => UpdateDensity(p)); //중요. 매번 처음에 density 업데이트를 해야함.

		if(ApplyPressure)
			particles.ForEach((p) => p.f_pressure = (CalcPressure(p) * PressureCoef));
		if(ApplyViscosity)
			particles.ForEach((p) => p.f_viscosity = (CalcViscosity(p) * ViscosityCoef));
		if(ApplySurfaceTension)
			particles.ForEach((p) => p.f_surface = (CalcSurfaceTension(p, TensionThreshold) * SurfaceTensionCoef));
		if(ApplyGravity)
			particles.ForEach((p) => p.AddAcceleration(gravity * p.density * GravityCoef)); //Gravity
		
		int frame = (int)(Time.time / 0.017f);
		particles.ForEach((p) => p.Apply(Steps, frame));
	}
	void UpdateDensity(Particle p)
	{
		//TODO : 업데이트시 가장 좋은건 kernel의 0조건을 먼저 판단하는 것임! (계산 처음부터 안하게) 시간나서 최적화 할 때 고려해보기
		p.density = particles.Sum((j) => j.mass * SmoothKernel_Poly6(p.transform.position - j.transform.position, h));
	}
	void UpdateColorField(Particle p) //항상 Density update이후에 호출되어야한다.
	{
		//2.12 쓸모 없는 놈인듯? colorFieldNormal 계산시 굳이 필요하지 않음. 
		p.colorField = particles.Sum((j) => j.mass / j.density * SmoothKernel_Poly6(p.transform.position - j.transform.position, h));
	}

	Vector3 CalcPressure(Particle p)
	{
		Vector3 force = Vector3.zero;
		float pressure_I = p.pressure;
		foreach(Particle j in particles)
		{
			if((p.transform.position - j.transform.position).sqrMagnitude < float.Epsilon)
				continue;
			float pressure_J = j.pressure; 
			float i_poly = (pressure_I / (p.density * p.density));
			float j_poly = pressure_J/ (j.density * j.density);
			Vector3 grad = SmoothKernel_Spiky_Gradient(p.transform.position - j.transform.position , h);
			//force += -0.5f * j.mass / j.density * ( pressure_I + pressure_J ) * SmoothKernel_Spiky_Gradient(p.transform.position - j.transform.position , h);
			force += (i_poly + j_poly) * grad;
		}
		force *= - p.mass * p.density; // 2.12 density 를 왜 곱해?? 논문식은 이렇지 않음
		return force;
	}

	Vector3 CalcViscosity(Particle p)
	{
		const float viscosityCoefficient = 3.5f; //0.79722f 0도, mPa /s 단위 (밀리파스칼, s는 뭔지 모르겠)
		//3.5f Ns/m^2
		Vector3 force = Vector3.zero;
		foreach(Particle j in particles)
		{
			if((p.transform.position - j.transform.position).sqrMagnitude < float.Epsilon)
				continue;
			force += j.mass / (j.density) * (j.velocity - p.velocity) * SmoothKernel_Viscosity_Laplacian(p.transform.position - j.transform.position, h);
		}
		force *= viscosityCoefficient;
		return force;
	}

	Vector3 CalcSurfaceTension(Particle p, float threshold) 
	{
		const float sigma = 0.07197f; // 0.0719-> N/m 단위  71.97mN/m (밀리뉴턴, 미터 단위) (25도에서)) 
		Vector3 n = Vector3.zero;		
		foreach(Particle j in particles)
		{
			n += j.mass / j.density * SmoothKernel_Poly6_Gradient(p.transform.position - j.transform.position, h);
		}
		float magnitude = n.magnitude;
		if(magnitude < threshold)
		{
			p.surfaceFlag = false;
			return Vector3.zero; //n의 magnitude가 너무 작음, 힘을 계산하지 않는다.
		}
		p.surfaceFlag = true;
		float laplacian = 0.0f;
		foreach(Particle j in particles)
		{
			laplacian += j.mass / j.density * SmoothKernel_Poly6_Laplacian(p.transform.position - j.transform.position, h);
		}
		float kappa = -laplacian / magnitude;
		Vector3 surfaceF = (sigma * kappa * n);
		return surfaceF;
	}

	float SmoothKernel_Poly6(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			double expo = IntPow(h,9);
			double d_coef = 315.0 / (64.0 * Mathf.PI * IntPow(h,9)) ;
			float coef = 315.0f / (64.0f * Mathf.PI * IntPow(h,9)) ;
			float result = coef * IntPow((h*h - position.sqrMagnitude),3);
			return result;
		}
		return 0.0f;
	}
	Vector3 SmoothKernel_Poly6_Gradient(Vector3 position, float h)
	{
		if(position.sqrMagnitude <= h * h)
		{
			float coef = - 945.0f / (32*Mathf.PI*IntPow(h,9));
			Vector3 result  =  coef * position * IntPow((h*h - position.sqrMagnitude), 2);
			return result;
		}
		return Vector3.zero;
	}

	float SmoothKernel_Poly6_Laplacian(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			// return 945 / (32 * Mathf.PI * IntPow(h, 9.0f)) *(-1 * IntPow((h*h - position.sqrMagnitude),2.0f) + 
			// 4 * position.magnitude*(h*h - position.sqrMagnitude));
			float result = -945.0f / (32.0f * Mathf.PI * IntPow(h,9)) * (h*h - position.sqrMagnitude) 
				* (3.0f * h*h - 7.0f * position.sqrMagnitude);
			return result;
		}
		return 0.0f;
	}

	
	Vector3 SmoothKernel_Spiky_Gradient(Vector3 position, float h)
	{
		if(position.sqrMagnitude < h * h)
		{
			float coef =- (45.0f / Mathf.PI / IntPow(h,6));
			float i = IntPow(h - position.magnitude, 2);
			return coef * i * position.normalized ;
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
			return 45.0f / Mathf.PI / IntPow(h,6) * (h - position.magnitude);
		}
		return 0.0f;
	}

	private float IntPow(float number, int p)
	{
		float origin = number;
		for(int i = 1 ; i < p; i++)
			number *= origin;
		return number;
	}
}
