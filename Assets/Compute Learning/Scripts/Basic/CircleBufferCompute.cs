using NaughtyAttributes;
using UnityCommons;
using UnityEngine;

public class CircleBufferCompute : ComputeRunner {
    private struct Circle {
        public Vector2 Position;
        public float Radius;
        public Vector4 Color;
    }

    [SerializeField, MaxValue(1024), MinValue(0)] private int CircleCount = 10;
    [SerializeField] private float WorldSize = 10.0f;

    private Circle[] circles;
    private int circleCount;
    private ComputeBuffer computeBuffer;

    protected override void OnStart() {
        circleCount = CircleCount;
        circles = new Circle[circleCount];
        var maxWorldSize = Resolution / WorldSize;
        for (var i = 0; i < circleCount; i++) {
            circles[i] = new Circle {
                Position = new Vector2(Rand.Float * maxWorldSize, Rand.Float * maxWorldSize),
                Radius = Rand.Range(0.35f, 0.85f),
                Color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f)
            };
        }

        computeBuffer = new ComputeBuffer(circleCount, sizeof(float) * 7, ComputeBufferType.Structured);
        computeBuffer.SetData(circles);
    }

    protected override void OnCleanup() {
        if (computeBuffer != null) {
            computeBuffer.Release();
        }
    }

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetFloat("WorldSize", WorldSize);
        shader.SetInt("CircleCount", circleCount);
        shader.SetBuffer(kernelID, "Circles", computeBuffer);
    }
}