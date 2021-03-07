using System;
using Unity.Mathematics;

[Serializable]
public struct SceneMaterial {
    public float3 Albedo;
    public float3 Specular;
    public float3 Emission;
    public float Smoothness;
    public int Stride => sizeof(float) * (3 + 3 + 3 + 1);
}