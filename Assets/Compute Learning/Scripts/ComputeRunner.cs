using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public abstract class ComputeRunner : MonoBehaviour {
    [SerializeField] private ComputeShader Shader;
    [SerializeField] private int Resolution = 512;
    private RenderTexture renderTexture;

    private void Awake() {
        if(renderTexture == null) MakeTexture();
        var target = GetComponent<RawImage>();
        target.texture = renderTexture;
        
        OnAwake();
    }

    private void Start() {
        OnStart();
    }

    protected virtual void OnAwake() {}
    protected virtual void OnStart() {}
    protected virtual void OnUpdate() {}

    private void OnDestroy() {
        if(renderTexture != null) renderTexture.Release();
    }

    private void Update() {
        OnUpdate();
        Render();
    }

    protected abstract void SetParameters(int kernelID, ComputeShader shader);

    private void Render() {
        var kernelID = Shader.FindKernel("CSMain");
        Shader.SetTexture(kernelID, "Result", renderTexture);
        Shader.SetFloat("Resolution", Resolution);
        SetParameters(kernelID, Shader);
        Shader.Dispatch(kernelID, Resolution/8, Resolution/8, 1);
    }

    private void MakeTexture() {
        if(renderTexture != null) renderTexture.Release();
        renderTexture = new RenderTexture(Resolution, Resolution, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }
}
