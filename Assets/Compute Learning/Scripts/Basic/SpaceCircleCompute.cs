using UnityEngine;

public class SpaceCircleCompute : ComputeRunner {
    [SerializeField] private Vector2 CirclePosition;
    [SerializeField] private float CircleRadius = 0.25f;
    [SerializeField] private float WorldSize = 10.0f;
    [SerializeField] private Transform Reference;
    
    protected override void SetParameters(int kernelID, ComputeShader shader) {
        if (Reference != null) CirclePosition = Reference.position;
        shader.SetVector("Position", CirclePosition);
        shader.SetFloat("Radius", CircleRadius);
        shader.SetFloat("WorldSize", WorldSize);
    }
}