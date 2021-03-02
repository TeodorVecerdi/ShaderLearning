using System.Diagnostics;
using NaughtyAttributes;
using UnityCommons;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GenerateCirclesCompute : ComputeRunner {
    private struct Circle {
        public Vector2 Position;
        public float Radius;
        public Vector4 Color;
    }

    private struct BlittableCircle {
        public float X;
        public float Y;
        public float Radius;
        public float R;
        public float G;
        public float B;
        public float A;
    }

    [SerializeField, MaxValue(1024), MinValue(0)] private int CircleCount = 64;
    [SerializeField] private float WorldSize = 10.0f;
    [SerializeField] private float PositionRange = 8.0f;
    [SerializeField] private ComputeShader GeneratorShader;
    [SerializeField, MinValue(1)] private int GenerateIterations = 1;

    private Circle[] circles;
    private int circleCount;
    private ComputeBuffer computeBuffer;

    protected override void OnStart() {
        circleCount = CircleCount;
        var oldIterations = GenerateIterations;
        GenerateIterations = 1;
        GenerateCircles();
        GenerateIterations = oldIterations;
    }

    protected override void OnCleanup() {
        if(computeBuffer != null) computeBuffer.Release();
    }

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetFloat("WorldSize", WorldSize);
        shader.SetInt("CircleCount", circleCount);
        shader.SetBuffer(kernelID, "Circles", computeBuffer);
    }

    [Button]
    private void GenerateCircles() {
        var generateCount = circleCount;
        var blittableCircles = new BlittableCircle[generateCount];
        for (var i = 0; i < generateCount; i++) {
            blittableCircles[i] = new BlittableCircle();
        }

        var generateBuffer = new ComputeBuffer(generateCount, sizeof(float) * 7, ComputeBufferType.Structured);
        generateBuffer.SetData(blittableCircles);
        
        GeneratorShader.SetFloat("Resolution", Resolution);
        GeneratorShader.SetInt("CircleCount", generateCount);
        GeneratorShader.SetFloat("PositionRange", PositionRange);
        GeneratorShader.SetFloat("WorldSize", WorldSize);
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < GenerateIterations; i++) {
            GeneratorShader.SetVector("Seed", new Vector2(Rand.Float * 1000, Rand.Float * 1000));
            GeneratorShader.SetBuffer(0, "Circles", generateBuffer);
            GeneratorShader.Dispatch(0, generateCount / 10, 1, 1);
        }
        Debug.Log($"{sw.Elapsed.TotalMilliseconds}ms");

        generateBuffer.GetData(blittableCircles);
        generateBuffer.Release();

        if (circles == null) circles = new Circle[circleCount];
        for (var i = 0; i < generateCount; i++) {
            circles[i] = new Circle {
                Position = new Vector2(blittableCircles[i].X, blittableCircles[i].Y),
                Radius = blittableCircles[i].Radius,
                Color = new Vector4(blittableCircles[i].R, blittableCircles[i].G, blittableCircles[i].B, blittableCircles[i].A)
            };
        }

        UpdateRenderBuffer();
    }

    private void UpdateRenderBuffer() {
        if(computeBuffer != null) computeBuffer.Release();
        computeBuffer = new ComputeBuffer(circleCount, sizeof(float) * 7, ComputeBufferType.Structured);
        computeBuffer.SetData(circles);
    }
}