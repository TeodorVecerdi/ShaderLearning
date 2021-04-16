using UnityEngine;

public class TimeCompute : ComputeRunner {
    [SerializeField] private float CircleRadius = 0.25f;
    [SerializeField] private float SineMultiplier = 0.5f;
    [SerializeField] private float TimeSpeed = 0.5f;
    
    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetFloat("Radius", CircleRadius);
        shader.SetFloat("SineMultiplier", SineMultiplier);
        shader.SetFloat("TimeSpeed", TimeSpeed);
        shader.SetFloat("Time", Time.time);
    }
}