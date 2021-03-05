using System;
using System.Diagnostics;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public abstract class ComputeRunner3 : MonoBehaviour {
    [ReadOnly] public double RenderTime = 0.0;
    [ReadOnly] public bool Active;
    [Space]
    [SerializeField] protected RawImage TargetDisplay;
    [SerializeField, Required] protected Camera SourceRenderer;
    [Space]
    [SerializeField]
    public ComputeShader ComputeShader;
    [SerializeField] private Vector3Int Resolution = new Vector3Int(2048, 1024, 32);
    [SerializeField] private Vector2Int ThreadGroupSize = new Vector2Int(8, 8);
    private RenderTexture sourceTexture;
    private RenderTexture displayTexture;
    protected RenderTexture WorkingTexture;
    protected bool ParametersDirty;

    private ComputeBuffer dummyRenderCheckBuffer;
    private readonly int[] renderCheckBufferData = {0};
    
    private bool lastFrameReady;
    private Stopwatch timer = new Stopwatch();

    protected Vector3Int resolution;
    protected Vector2Int threadGroupSize;
    
    private void Awake() {
        // Bake settings so you can't change them during runtime
        resolution = Resolution;
        threadGroupSize = ThreadGroupSize;
        
        ParametersDirty = true;
        MakeBuffer(ref dummyRenderCheckBuffer, 1, sizeof(int), renderCheckBufferData);
        MakeTexture(ref sourceTexture);
        MakeTexture(ref displayTexture);
        MakeTexture(ref WorkingTexture);

        if(Active) SetShaderActiveImpl();
        
        OnAwake();
    }
    
    private void Start() {
        OnStart();
    }

    private void OnDestroy() {
        if (sourceTexture != null) sourceTexture.Release();
        if (displayTexture != null) displayTexture.Release();
        if (WorkingTexture != null) WorkingTexture.Release();
        if(dummyRenderCheckBuffer != null) dummyRenderCheckBuffer.Release();
        
        OnFreeResources();
    }

    public void SetShaderActive(bool active) {
        if (active == Active) return;
        Active = active;
        SetShaderActiveImpl();
    }

    protected virtual void OnEnableShader() { }
    protected virtual void OnDisableShader() { }
    protected virtual void OnAwake() { }
    protected virtual void OnStart() { }
    protected virtual void OnFreeResources() { }
    protected virtual void SetParameters(int kernelID, ComputeShader shader) { }
    protected virtual void SetParametersOnce(int kernelID, ComputeShader shader) { }

    protected virtual Vector3Int GetDispatchSize() {
        return new Vector3Int(Mathf.CeilToInt((float)resolution.x / threadGroupSize.x), Mathf.CeilToInt((float)resolution.y / threadGroupSize.y), 1);
    }

    protected virtual void OnBeforeRender(Action beforeRenderComplete) {
        beforeRenderComplete();
    }

    protected virtual void OnRender(Action renderComplete) {
        AsyncGPUReadback.Request(dummyRenderCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyRenderCheckBuffer)</b> has error");
            renderComplete();
        });
    }

    protected virtual void OnAfterRender(Action afterRenderComplete) {
        afterRenderComplete();
    }

    private void RunBeforeRender() {
        OnBeforeRender(BeforeRenderComplete);
    }
    
    private void RunRender() {
        var kernelID = ComputeShader.FindKernel("CSMain");
        ComputeShader.SetTexture(kernelID, "Result", WorkingTexture);
        ComputeShader.SetInt("ResolutionX", resolution.x);
        ComputeShader.SetInt("ResolutionY", resolution.y);
        SetParameters(kernelID, ComputeShader);
        if (ParametersDirty) {
            ParametersDirty = false;
            ComputeShader.SetBuffer(kernelID, "RenderCheckBuffer", dummyRenderCheckBuffer);
            SetParametersOnce(kernelID, ComputeShader);
        }

        var dispatchSize = GetDispatchSize();
        ComputeShader.Dispatch(kernelID, dispatchSize.x, dispatchSize.y, dispatchSize.z);
        OnRender(RenderComplete);
    }

    private void RunAfterRender() {
        OnAfterRender(AfterRenderComplete);
    }
    
    private void Run() {
        if (!Active || !lastFrameReady) return;
        lastFrameReady = false;
        Graphics.Blit(sourceTexture, WorkingTexture);
        RunBeforeRender();
    }

    private void BeforeRenderComplete() {
        timer.Restart();
        RunRender();
    }

    private void RenderComplete() {
        RenderTime = timer.Elapsed.TotalMilliseconds;
        if (!Active) {
            AfterRenderComplete();
            return;
        }
        RunAfterRender();
    }

    private void AfterRenderComplete() {
        Graphics.Blit(WorkingTexture, displayTexture);
        lastFrameReady = true;
    }
    
    private void SetShaderActiveImpl() {
        if (!Active) {
            if (SourceRenderer != null) SourceRenderer.targetTexture = null;
            OnDisableShader();
            return;
        }
        
        lastFrameReady = true;
        if (sourceTexture != null) {
            if (SourceRenderer != null) SourceRenderer.targetTexture = sourceTexture;
            if (TargetDisplay == null) TargetDisplay = GetComponent<RawImage>();
            if (TargetDisplay != null) TargetDisplay.texture = displayTexture;
        }
        OnEnableShader();
    }
    
    protected void MakeTexture(ref RenderTexture texture) {
        MakeTexture(ref texture, resolution, RenderTextureFormat.ARGB32);
    }
    
    protected void MakeTexture(ref RenderTexture texture, Vector3Int textureResolution, RenderTextureFormat format) {
        if (texture != null) texture.Release();
        texture = new RenderTexture(textureResolution.x, textureResolution.y, textureResolution.z, format) {enableRandomWrite = true};
        texture.Create();
    }

    protected void MakeTextureFromDescriptor(ref RenderTexture texture, RenderTextureDescriptor desc) {
        if(texture != null) texture.Release();
        texture = new RenderTexture(desc) {enableRandomWrite = true};
        texture.Create();
    }
    
    protected void MakeBuffer(ref ComputeBuffer computeBuffer, int count, int stride, Array data) {
        if (computeBuffer != null) computeBuffer.Release();
        computeBuffer = new ComputeBuffer(count, stride);
        computeBuffer.SetData(data);
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