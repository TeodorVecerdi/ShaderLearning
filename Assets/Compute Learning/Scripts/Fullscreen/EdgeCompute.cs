using NaughtyAttributes;
using UnityEngine;

public class EdgeCompute : ComputeRunner2 {
    [Space] public ComputeShader CopyShader; 
    public Color EdgeColor;
    [MinValue(0)] public float LookupDistance = 3.0f;
    [MinValue(0)] public float MinDistance = 0.25f;

    private RenderTexture edgeTexture;

    protected override void OnAwake() {
        if (edgeTexture == null) MakeTextureFromDesc(ref edgeTexture, RenderTexture.descriptor);
    }

    protected override void OnCleanup() {
        if(edgeTexture != null) edgeTexture.Release();
    }

    protected override void OnAfterRender() {
        var kernelID = CopyShader.FindKernel("CSMain");
        CopyShader.SetTexture(kernelID, "Src", edgeTexture);
        CopyShader.SetTexture(kernelID, "Dst", RenderTexture);
        CopyShader.Dispatch(kernelID, Resolution.x / 8, Resolution.y / 8, 1);
    }

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetVector("Color", EdgeColor);
        shader.SetInt("Width", Resolution.x);
        shader.SetInt("Height", Resolution.y);
        shader.SetFloat("LookupDistance", LookupDistance);
        shader.SetFloat("MinDistance", MinDistance);
        shader.SetTexture(kernelID, "EdgeTexture", edgeTexture);
    }
}