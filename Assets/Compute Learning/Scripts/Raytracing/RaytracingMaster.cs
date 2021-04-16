using System;
using System.Collections.Generic;
using System.Diagnostics;
using NaughtyAttributes;
using TMPro;
using UnityCommons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class RaytracingMaster : MonoBehaviour {
    [SerializeField] private ComputeShader RaytracingShader;
    [SerializeField] private BakedType<Vector3Int> Resolution = new Vector3Int(2048, 1024, 32);
    [SerializeField] private int Samples = 256;
    [SerializeField, Required] private Camera Camera;
    [SerializeField, Required] private RawImage Display;
    [Space, SerializeField] private Texture SkyboxTexture;
    [SerializeField, Required] private Light DirectionalLight;
    [Space(10), SerializeField, Expandable, Required] private Scene Scene;
    [Header("Random Settings")]
    [SerializeField] private int Seed = 0;
    [Space, SerializeField] private Vector2Int MaterialCountMinMax = new Vector2Int(5, 20);
    [SerializeField] private float MetallicChance = 0.5f;
    [SerializeField] private Vector2 SpecularRange = new Vector2(0, 0.002f);
    [SerializeField] private Vector2Int SphereCountMinMax = new Vector2Int(10, 30);
    [SerializeField] private Vector2 SphereRadiusMinMax = new Vector2(3, 8);
    [SerializeField] private float SpherePositionRadius = 100.0f;
    [SerializeField] private float SpherePositionHeight = 10f;
    
    [Space(20), ReadOnly, SerializeField] private string Duration;
    private double duration;

    private RenderTexture targetTexture;
    private RenderTexture displayTexture;
    private RenderTexture convergeTexture;
    private ComputeBuffer dummyRenderCheckBuffer;
    private readonly int[] renderCheckBufferData = {0};

    private uint currentSample;
    private Material antiAliasingMaterial;

    private float oldFieldOfView;

    private readonly Stopwatch timer = new Stopwatch();
    private bool lastFrameReady = true;
    
    private static readonly int antiAliasingSampleID = Shader.PropertyToID("_Sample");
    private List<Transform> resetTransforms = new List<Transform>();
    
    private ComputeBuffer materialComputeBuffer;
    private ComputeBuffer sphereComputeBuffer;

    private void Awake() {
        Resolution.Bake();
        InitRenderTexture();
        MakeBuffer(ref dummyRenderCheckBuffer, 1, sizeof(int), renderCheckBufferData);
        Display.texture = displayTexture;

        oldFieldOfView = Camera.fieldOfView;
        resetTransforms.Add(Camera.transform);
        resetTransforms.Add(DirectionalLight.transform);
    }

    private void Update() {
        
        foreach (var resetTransform in resetTransforms) {
            if (!resetTransform.hasChanged) continue;
            
            resetTransform.hasChanged = false;
            currentSample = 0;
        }

        if (Math.Abs(oldFieldOfView - Camera.fieldOfView) > 0.01f) {
            oldFieldOfView = Camera.fieldOfView;
            currentSample = 0;
        }

        if (currentSample > Samples) currentSample = (uint)Samples;
    }

    private void Run() {
        if (!lastFrameReady) return;
        lastFrameReady = false;

        duration = timer.Elapsed.TotalMilliseconds;
        Duration = $"{duration:F4}ms";
        timer.Restart();

        Render();
    }

    private void Render() {
        SetShaderParameters();
        RaytracingShader.SetTexture(0, "Result", targetTexture);
        var threadGroupsX = Mathf.CeilToInt(Resolution.Value.x / 32.0f);
        var threadGroupsY = Mathf.CeilToInt(Resolution.Value.y / 16.0f);
        RaytracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        OnRender();
    }

    private void SetShaderParameters() {
        RaytracingShader.SetBuffer(0, "RenderCheckBuffer", dummyRenderCheckBuffer);
        RaytracingShader.SetMatrix("CameraToWorld", Camera.cameraToWorldMatrix);
        RaytracingShader.SetMatrix("CameraInverseProjection", Camera.projectionMatrix.inverse);
        RaytracingShader.SetTexture(0, "SkyboxTexture", SkyboxTexture);
        RaytracingShader.SetVector("PixelOffset", new Vector2(Rand.Float, Rand.Float));
        RaytracingShader.SetFloat("Sample", currentSample);
        var direction = DirectionalLight.transform.forward;
        RaytracingShader.SetVector("DirectionalLight", new Vector4(direction.x, direction.y, direction.z, DirectionalLight.intensity));

        RaytracingShader.SetBuffer(0, "CurrentScene.Materials", materialComputeBuffer);
        RaytracingShader.SetBuffer(0, "CurrentScene.Spheres", sphereComputeBuffer);
    }

    private void OnRender() {
        AsyncGPUReadback.Request(dummyRenderCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyRenderCheckBuffer)</b> has error");
            lastFrameReady = true;

            if (antiAliasingMaterial == null) antiAliasingMaterial = new Material(Shader.Find("Raytracing/Average"));
            antiAliasingMaterial.SetFloat(antiAliasingSampleID, currentSample);
            currentSample++;
            
            Graphics.Blit(targetTexture, displayTexture);
            // Graphics.Blit(convergeTexture, displayTexture);
        });
    }

    private void InitRenderTexture() {
        var copy = Resolution.Value;
        copy.z = 0;
        MakeTexture(ref targetTexture, copy, RenderTextureFormat.ARGBFloat);
        MakeTexture(ref convergeTexture, copy, RenderTextureFormat.ARGBFloat);
        MakeTexture(ref displayTexture, copy, RenderTextureFormat.ARGBFloat);
    }

    private void MakeTexture(ref RenderTexture texture, Vector3Int textureResolution, RenderTextureFormat format = RenderTextureFormat.ARGB32,
                             RenderTextureReadWrite readWrite = RenderTextureReadWrite.Linear) {
        if (texture != null) texture.Release();
        texture = new RenderTexture(textureResolution.x, textureResolution.y, textureResolution.z, format, readWrite) {enableRandomWrite = true};
        texture.Create();
    }

    private void MakeBuffer(ref ComputeBuffer computeBuffer, int count, int stride, Array data) {
        if (computeBuffer != null) computeBuffer.Release();
        computeBuffer = new ComputeBuffer(count, stride);
        computeBuffer.SetData(data);
    }

    private void GenerateMaterials() {
        Scene.Materials.Clear();

        var materialCount = Rand.Range(MaterialCountMinMax);
        for (var i = 0; i < materialCount; i++) {
            var material = new SceneMaterial();
            var color = new Vector3(Rand.Float, Rand.Float, Rand.Float);
            var isMetallic = Rand.Chance(MetallicChance);
            var specular = Rand.Range(SpecularRange);
            if (isMetallic) {
                material.Albedo = color * specular;
                material.Specular = color;
            } else {
                material.Albedo = color;
                material.Specular = Vector3.zero;
            }
            Scene.Materials.Add(material);
        }

        if (materialComputeBuffer != null) materialComputeBuffer.Release();
        MakeBuffer(ref materialComputeBuffer, materialCount, Scene.Materials[0].Stride, Scene.Materials.ToArray());
    }

    private void GenerateSpheres() {
        Scene.Spheres.Clear();

        var sphereCount = Rand.Range(SphereCountMinMax);
        for (var i = 0; i < sphereCount; i++) {
            var sphere = new SceneSphere {
                MaterialIndex = (uint) Rand.Range(Scene.Materials.Count),
                Radius = Rand.Range(SphereRadiusMinMax),
                Position = Rand.InsideUnitCircleVec3 * SpherePositionRadius + Vector3.up * Rand.Float * SpherePositionHeight
            };
            Scene.Spheres.Add(sphere);
        }
        
        if (sphereComputeBuffer != null) sphereComputeBuffer.Release();
        MakeBuffer(ref sphereComputeBuffer, sphereCount, Scene.Spheres[0].Stride, Scene.Spheres.ToArray());
    }

    private void OnDestroy() {
        if (targetTexture != null) targetTexture.Release();
        if (displayTexture != null) displayTexture.Release();
        if (convergeTexture != null) convergeTexture.Release();
        if (dummyRenderCheckBuffer != null) dummyRenderCheckBuffer.Release();
    }

    private void OnEnable() {
        RenderPipelineManager.endFrameRendering += RenderPipelineManager_endFrameRendering;
        Rand.PushState(Seed);
        GenerateMaterials();
        GenerateSpheres();
        Rand.PopState();
        currentSample = 0;
    }

    private void OnDisable() {
        RenderPipelineManager.endFrameRendering -= RenderPipelineManager_endFrameRendering;
        if(materialComputeBuffer != null) materialComputeBuffer.Release();
        if(sphereComputeBuffer != null) sphereComputeBuffer.Release();
    }

    private void RenderPipelineManager_endFrameRendering(ScriptableRenderContext context, Camera[] cameras) {
        Run();
    }
}