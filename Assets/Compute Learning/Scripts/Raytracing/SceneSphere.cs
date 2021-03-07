using System;
using Unity.Mathematics;

[Serializable]
public struct SceneSphere {
    public float3 Position;
    public float Radius;
    public uint MaterialIndex;
    public int Stride => sizeof(float) * (3 + 1) + sizeof(uint) * 1;
}