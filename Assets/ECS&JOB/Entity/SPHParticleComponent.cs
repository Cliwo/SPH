using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct SPHParticle : ISharedComponentData 
{
	public float Radius;
	public float SmoothingRadius;
	public float SmoothingRadiusSq;
	public float mass;
	public float RestDensity;
	public float GasConstant;
	public float Viscosity; //이게 왜 여기에 필요해? 전역아니야? 
	public float GravityMult;
	public float Drag;
	public float SurfaceTension;
	public float SurfaceThreshold;

	public float TimeStep;
}

public class SPHParticleComponent : SharedComponentDataWrapper<SPHParticle>{}