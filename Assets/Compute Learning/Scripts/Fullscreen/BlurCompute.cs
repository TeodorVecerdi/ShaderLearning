using System;
using NaughtyAttributes;
using UnityEngine;

public class BlurCompute : ComputeRunner3 {
    [Space]
    [OnValueChanged("KernelDirty")] public int KernelSize = 5;
    [OnValueChanged("KernelDirty")] public float BlurAmount = 2;
    [Range(0, 5)] public int DownSamplingPower = 3;

    private RenderTexture blurTexture;
    [SerializeField] private float[] kernel;
    private ComputeBuffer kernelBuffer;
    private bool kernelDirty = true;
    private float sigma;

    protected override void OnAwake() {
        var downSampleLevel = 1 << DownSamplingPower;
        var downSampleResolution = resolution / downSampleLevel;
        downSampleResolution.z = 32;
        MakeTexture(ref blurTexture, downSampleResolution, RenderTextureFormat.Default);
        blurTexture.filterMode = FilterMode.Bilinear;
    }

    protected override void OnFreeResources() {
        if (blurTexture != null) blurTexture.Release();
        if (kernelBuffer != null) kernelBuffer.Release();
    }

    protected override Vector3Int GetDispatchSize() {
        var downSampleLevel = 1 << DownSamplingPower;
        var downSampleResolution = resolution / downSampleLevel;
        downSampleResolution.x /= threadGroupSize.x;
        downSampleResolution.y /= threadGroupSize.y;
        downSampleResolution.z = 1;
        return downSampleResolution;
    }
    
    protected override void OnBeforeRender(Action beforeRenderComplete) {
        if (kernelDirty) {
            kernelDirty = false;
            ParametersDirty = true;
            GenerateKernel();
            MakeBuffer(ref kernelBuffer, KernelSize * KernelSize, sizeof(float), kernel);
        }
        beforeRenderComplete();
    }

    protected override void OnAfterRender(Action afterRenderComplete) {
        var downSampleLevel = 1 << DownSamplingPower;
        var downSampleScale = Vector2.one * downSampleLevel;
        Graphics.Blit(blurTexture, WorkingTexture);
        afterRenderComplete();
    }

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        var downSampleLevel = 1 << DownSamplingPower;
        var downSampleResolution = resolution / downSampleLevel;
        shader.SetInt("Width", downSampleResolution.x);
        shader.SetInt("Height", downSampleResolution.y);
        shader.SetInt("KernelSize", KernelSize);
        shader.SetTexture(kernelID, "Blur", blurTexture);
        shader.SetBuffer(kernelID, "Kernel", kernelBuffer);
    }

    protected override void SetParametersOnce(int kernelID, ComputeShader shader) {
    }

    private float GetSigma() {
        return BlurAmount / Mathf.Sqrt(8.0f * Mathf.Log(2));
    }

    private void GenerateKernel() {
        Debug.Log("Generated kernel");
        sigma = GetSigma();
        kernel = new float[KernelSize * KernelSize];
        var invsqrt = (float) (1.0 / (2f * Mathf.PI * sigma * sigma));
        var sum = 0.0f;
        for (var y = 0; y < KernelSize; y++) {
            for (var x = 0; x < KernelSize; x++) {
                var yc = y - (KernelSize - 1) / 2;
                var xc = x - (KernelSize - 1) / 2;

                var g = Mathf.Exp(-(yc * yc + xc * xc) / (2 * sigma * sigma)) * invsqrt;
                sum += g;
                kernel[y * KernelSize + x] = g;
            }
        }

        for (var y = 0; y < KernelSize; y++) {
            for (var x = 0; x < KernelSize; x++) {
                kernel[y * KernelSize + x] /= sum;
            }
        }
    }

    private void KernelDirty() {
        kernelDirty = true;
    }
}