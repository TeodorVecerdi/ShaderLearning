using System;
using System.Diagnostics;
using NaughtyAttributes;
using UnityCommons;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class RaytracingMaster : MonoBehaviour {
    [SerializeField] private ComputeShader RaytracingShader;
    [SerializeField] private BakedType<Vector3Int> Resolution = new Vector3Int(2048, 1024, 32);
    [SerializeField, Required] private Camera Camera;
    [SerializeField, Required] private RawImage Display;
    [Space, SerializeField] private Texture SkyboxTexture;
    [Space(20), ReadOnly, SerializeField] private string Duration;

    private RenderTexture targetTexture;
    private RenderTexture displayTexture;
    private ComputeBuffer dummyRenderCheckBuffer;
    private readonly int[] renderCheckBufferData = {0};

    private uint currentSample;
    private Material antiAliasingMaterial;

    private float oldFieldOfView;

    private readonly Stopwatch timer = new Stopwatch();
    private bool lastFrameReady = true;
    
    private static readonly int antiAliasingSampleID = Shader.PropertyToID("_Sample");

    private void Awake() {
        Resolution.Bake();
        InitRenderTexture();
        MakeBuffer(ref dummyRenderCheckBuffer, 1, sizeof(int), renderCheckBufferData);
        Display.texture = displayTexture;

        oldFieldOfView = Camera.fieldOfView;
    }

    private void Update() {
        if (Camera.transform.hasChanged) {
            Camera.transform.hasChanged = false;
            currentSample = 0;
        }

        if (Math.Abs(oldFieldOfView - Camera.fieldOfView) > 0.01f) {
            oldFieldOfView = Camera.fieldOfView;
            currentSample = 0;
        }
    }

    private void Run() {
        if (!lastFrameReady) return;
        lastFrameReady = false;

        var duration = timer.Elapsed.TotalMilliseconds;
        Duration = $"{duration:F4}ms";
        timer.Restart();

        Render();
    }

    private void Render() {
        SetShaderParameters();
        RaytracingShader.SetTexture(0, "Result", targetTexture);
        var threadGroupsX = Mathf.CeilToInt(Resolution.Value.x / 8.0f);
        var threadGroupsY = Mathf.CeilToInt(Resolution.Value.y / 8.0f);
        RaytracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        OnRender();
    }

    private void SetShaderParameters() {
        RaytracingShader.SetBuffer(0, "RenderCheckBuffer", dummyRenderCheckBuffer);
        RaytracingShader.SetMatrix("CameraToWorld", Camera.cameraToWorldMatrix);
        RaytracingShader.SetMatrix("CameraInverseProjection", Camera.projectionMatrix.inverse);
        RaytracingShader.SetTexture(0, "SkyboxTexture", SkyboxTexture);
        RaytracingShader.SetVector("PixelOffset", new Vector2(Rand.Float, Rand.Float));
    }

    private void OnRender() {
        AsyncGPUReadback.Request(dummyRenderCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyRenderCheckBuffer)</b> has error");
            lastFrameReady = true;

            if (antiAliasingMaterial == null) antiAliasingMaterial = new Material(Shader.Find("Hidden/RaytracingAA"));
            antiAliasingMaterial.SetFloat(antiAliasingSampleID, currentSample);
            currentSample++;
            
            Graphics.Blit(targetTexture, displayTexture, antiAliasingMaterial);
        });
    }

    private void InitRenderTexture() {
        var copy = Resolution.Value;
        copy.z = 0;
        MakeTexture(ref targetTexture, copy, RenderTextureFormat.ARGBFloat);
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

    private void OnDestroy() {
        if (targetTexture != null) targetTexture.Release();
        if (displayTexture != null) displayTexture.Release();
        if (dummyRenderCheckBuffer != null) dummyRenderCheckBuffer.Release();
    }

    private void OnEnable() {
        RenderPipelineManager.endFrameRendering += RenderPipelineManager_endFrameRendering;
    }

    private void OnDisable() {
        RenderPipelineManager.endFrameRendering -= RenderPipelineManager_endFrameRendering;
    }

    private void RenderPipelineManager_endFrameRendering(ScriptableRenderContext context, Camera[] cameras) {
        Run();
    }
}