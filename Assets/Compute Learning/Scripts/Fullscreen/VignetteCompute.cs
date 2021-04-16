using System;
using UnityEngine;
using UnityEngine.Rendering;

public class VignetteCompute : ComputeRunner3 {
    [Space] public ComputeShader CopyShader;
    public float Intensity;
    public float Radius;
    public Color Color;

    private RenderTexture vignetteTexture;
    private ComputeBuffer dummyCopyCheckBuffer;
    private readonly int[] copyCheckBufferData = {0};

    protected override void OnAwake() {
        MakeTextureFromDescriptor(ref vignetteTexture, WorkingTexture.descriptor);
        MakeBuffer(ref dummyCopyCheckBuffer, 1, sizeof(int), copyCheckBufferData);
    }

    protected override void OnFreeResources() {
        if (vignetteTexture != null) vignetteTexture.Release();
        if (dummyCopyCheckBuffer != null) dummyCopyCheckBuffer.Release();
    }
    
    protected override void OnAfterRender(Action afterRenderComplete) {
        var kernelID = CopyShader.FindKernel("CSMain");
        CopyShader.SetTexture(kernelID, "Src", vignetteTexture);
        CopyShader.SetTexture(kernelID, "Dst", WorkingTexture);
        CopyShader.SetBuffer(kernelID, "CopyCheckBuffer", dummyCopyCheckBuffer);
        CopyShader.Dispatch(kernelID, resolution.x / 16, resolution.y / 16, 1);

        AsyncGPUReadback.Request(dummyCopyCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyCopyCheckBuffer)</b> has error.");
            afterRenderComplete();
        });
    }
    
    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetVector("Color", Color);
        shader.SetInt("Width", resolution.x);
        shader.SetInt("Height", resolution.y);
        shader.SetFloat("Intensity", Intensity);
        shader.SetFloat("Radius", Radius);
        shader.SetTexture(kernelID, "VignetteTexture", vignetteTexture);
    }
}