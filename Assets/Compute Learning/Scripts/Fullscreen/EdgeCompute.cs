using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;

public class EdgeCompute : ComputeRunner3 {
    [Space] public ComputeShader CopyShader; 
    public Color EdgeColor;
    [MinValue(0)] public float LookupDistance = 3.0f;
    [MinValue(0)] public float MinDistance = 0.25f;

    private RenderTexture edgeTexture;
    private ComputeBuffer dummyCopyCheckBuffer;
    private readonly int[] copyCheckBufferData = {0};

    protected override void OnAwake() {
        MakeTextureFromDescriptor(ref edgeTexture, WorkingTexture.descriptor);
        MakeBuffer(ref dummyCopyCheckBuffer, 1, sizeof(int), copyCheckBufferData);
    }

    protected override void OnFreeResources() {
        if(edgeTexture != null) edgeTexture.Release();
        if(dummyCopyCheckBuffer != null) dummyCopyCheckBuffer.Release();
    }

    protected override void OnAfterRender(Action afterRenderComplete) {
        var kernelID = CopyShader.FindKernel("CSMain");
        CopyShader.SetTexture(kernelID, "Src", edgeTexture);
        CopyShader.SetTexture(kernelID, "Dst", WorkingTexture);
        CopyShader.SetBuffer(kernelID, "CopyCheckBuffer", dummyCopyCheckBuffer);
        CopyShader.Dispatch(kernelID, resolution.x / 16, resolution.y / 16, 1);

        AsyncGPUReadback.Request(dummyCopyCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyCopyCheckBuffer)</b> has error.");
            afterRenderComplete();
        });
    }

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetVector("Color", EdgeColor);
        shader.SetInt("Width", resolution.x);
        shader.SetInt("Height", resolution.y);
        shader.SetFloat("LookupDistance", LookupDistance);
        shader.SetFloat("MinDistance", MinDistance);
        shader.SetTexture(kernelID, "EdgeTexture", edgeTexture);
    }
}