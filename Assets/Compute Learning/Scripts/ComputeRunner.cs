using System;
using System.Diagnostics;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public abstract class ComputeRunner : MonoBehaviour {
    [SerializeField] private ComputeShader Shader;
    [SerializeField] protected int Resolution = 512;
    [SerializeField, ReadOnly] private double DurationMs;
    
    private RenderTexture renderTexture;
    private int resolution;

    private void Awake() {
        if(renderTexture == null) MakeTexture();
        var target = GetComponent<RawImage>();
        target.texture = renderTexture;

        resolution = Resolution;
        
        OnAwake();
    }

    private void Start() {
        OnStart();
    }

    protected virtual void OnAwake() {}
    protected virtual void OnStart() {}
    protected virtual void OnUpdate() {}
    protected virtual void OnCleanup() {}

    private void OnDestroy() {
        if(renderTexture != null) renderTexture.Release();
        OnCleanup();
    }
    

    private void Update() {
        OnUpdate();
        var timer = new Stopwatch();
        timer.Start();
        Render();
        DurationMs = timer.Elapsed.TotalMilliseconds;
    }

    protected abstract void SetParameters(int kernelID, ComputeShader shader);

    private void Render() {
        var kernelID = Shader.FindKernel("CSMain");
        Shader.SetTexture(kernelID, "Result", renderTexture);
        Shader.SetFloat("Resolution", resolution);
        SetParameters(kernelID, Shader);
        Shader.Dispatch(kernelID, resolution/8, resolution/8, 1);
    }

    private void MakeTexture() {
        if(renderTexture != null) renderTexture.Release();
        renderTexture = new RenderTexture(Resolution, Resolution, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }
}
