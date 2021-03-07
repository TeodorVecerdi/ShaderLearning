using UnityEngine;

public class CircleCompute : ComputeRunner {
    [SerializeField] private float CircleRadius = 0.25f;
    
    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetFloat("Radius", CircleRadius);
    }
}