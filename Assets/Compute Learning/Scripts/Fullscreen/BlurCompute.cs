using NaughtyAttributes;
using UnityEngine;

public class BlurCompute : ComputeRunner2 {
    [Space(20)] public ComputeShader CopyShader;
    public ComputeShader DownSampleShader;
    public ComputeShader UpSampleShader;
    [OnValueChanged("KernelDirty")] public int KernelSize = 5;
    [OnValueChanged("KernelDirty")] public float BlurAmount = 2;
    // [Range(1, 4), OnValueChanged("DownSampleDirty")] public int DownSamplingPower = 3;

    private RenderTexture blurTexture;
    private RenderTexture downSampleTexture;
    private float[] kernel;
    private ComputeBuffer kernelBuffer;
    private bool kernelDirty = true;
    private float sigma;

    protected override void OnAwake() {
        if (blurTexture == null) MakeTextureFromDesc(ref blurTexture, RenderTexture.descriptor);
    }

    protected override void OnCleanup() {
        if (blurTexture != null) blurTexture.Release();
        if (downSampleTexture != null) downSampleTexture.Release();
        if (kernelBuffer != null) kernelBuffer.Release();
    }

    protected override void OnBeforeRender() {
        if (kernelDirty) {
            kernelDirty = false;
            GenerateKernel();
            GenerateKernelBuffer();
        }
    }

    protected override void OnAfterRender() {
        var kernelID = CopyShader.FindKernel("CSMain");
        CopyShader.SetTexture(kernelID, "Src", blurTexture);
        CopyShader.SetTexture(kernelID, "Dst", RenderTexture);
        CopyShader.Dispatch(kernelID, Resolution.x / 8, Resolution.y / 8, 1);
    }
    

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        shader.SetInt("Width", Resolution.x);
        shader.SetInt("Height", Resolution.y);
        shader.SetInt("KernelSize", KernelSize);
        shader.SetBuffer(kernelID, "Kernel", kernelBuffer);
        shader.SetTexture(kernelID, "Blur", blurTexture);
    }

    /*
    private void DownSample() {
        if(downSampleTexture == null) DownSampleDirty();
        var kernelID = DownSampleShader.FindKernel("CSMain");
        var downSampling = 1 << DownSamplingPower;
        var resolutionX = Resolution.x / downSampling;
        var resolutionY = Resolution.y / downSampling;
        DownSampleShader.SetInt("DownSampling", downSampling);
        DownSampleShader.SetTexture(kernelID, "Src", RenderTexture);
        DownSampleShader.SetTexture(kernelID, "Dst", downSampleTexture);
        DownSampleShader.Dispatch(kernelID, resolutionX / 8, resolutionY / 8, 1);
    }

    private void UpSample() {
        var kernelID = UpSampleShader.FindKernel("CSMain");
        var downSampling = 1 << DownSamplingPower;
        var resolutionX = Resolution.x / downSampling;
        var resolutionY = Resolution.y / downSampling;
        UpSampleShader.SetInt("UpSampling", downSampling);
        UpSampleShader.SetTexture(kernelID, "Src", downSampleTexture);
        UpSampleShader.SetTexture(kernelID, "Dst", blurTexture);
        UpSampleShader.Dispatch(kernelID, resolutionX / 8, resolutionY / 8, 1);
    }
    */

    private void GenerateKernelBuffer() {
        if (kernelBuffer != null) kernelBuffer.Release();
        kernelBuffer = new ComputeBuffer(KernelSize * KernelSize, sizeof(float));
        kernelBuffer.SetData(kernel);
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

    /*private void DownSampleDirty() {
        if (downSampleTexture != null) downSampleTexture.Release();
        if (blurDownSampleTexture != null) blurDownSampleTexture.Release();
        
        var downsampling = 1 << DownSamplingPower;
        downSampleTexture = new RenderTexture(Resolution.x / downsampling, Resolution.y / downsampling, 32);
        downSampleTexture.enableRandomWrite = true;
        downSampleTexture.Create();
        blurDownSampleTexture = new RenderTexture(Resolution.x / downsampling, Resolution.y / downsampling, 32);
        blurDownSampleTexture.enableRandomWrite = true;
        blurDownSampleTexture.Create();
    }*/
}