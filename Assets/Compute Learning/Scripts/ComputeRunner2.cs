using System.Diagnostics;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public abstract class ComputeRunner2 : MonoBehaviour {
    public ComputeShader Shader;
    [SerializeField] private RawImage TargetImage;
    [SerializeField] protected Vector3Int Resolution = new Vector3Int(2048, 1024, 32);
    [SerializeField] protected Vector3Int DispatchGroupSize = new Vector3Int(8, 8, 1);
    [ReadOnly] public double DurationMs;
    [SerializeField] private Camera Source;
    [ReadOnly] public bool Active;

    protected RenderTexture RenderTexture;
    private int resolutionX;
    private int resolutionY;

    public void SetActive(bool newActive) {
        if(newActive == Active) return;
        Active = newActive;

        if (!Active && Source != null) Source.targetTexture = null;
        if (!Active) return;

        if (RenderTexture != null && Source != null) Source.targetTexture = RenderTexture;
        if (TargetImage == null)
            TargetImage = GetComponent<RawImage>();

        if (TargetImage != null)
            TargetImage.texture = RenderTexture;
    }

    private void Awake() {
        if (RenderTexture == null) MakeTexture(ref RenderTexture);

        if (Active) {
            Source.targetTexture = RenderTexture;
            if (TargetImage == null)
                TargetImage = GetComponent<RawImage>();

            if (TargetImage != null)
                TargetImage.texture = RenderTexture;
        }

        resolutionX = Resolution.x;
        resolutionY = Resolution.y;

        OnAwake();
    }

    private void Start() {
        OnStart();
    }

    protected virtual void OnAwake() {
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnCleanup() {
    }

    protected virtual void OnBeforeRender() {
    }
    
    protected virtual bool OnRender() {
        return false;
    }

    protected virtual void OnAfterRender() {
    }

    private void OnDestroy() {
        if (RenderTexture != null) RenderTexture.Release();
        OnCleanup();
    }

    private void OnPostRender() {
        if (!Active) return;
        OnBeforeRender();
        var timer = new Stopwatch();
        timer.Start();
        Render();
        DurationMs = timer.Elapsed.TotalMilliseconds;
        OnAfterRender();
    }

    protected abstract void SetParameters(int kernelID, ComputeShader shader);

    public void Render() {
        if(OnRender()) return;
        var kernelID = Shader.FindKernel("CSMain");
        Shader.SetTexture(kernelID, "Result", RenderTexture);
        Shader.SetFloat("ResolutionX", resolutionX);
        Shader.SetFloat("ResolutionY", resolutionY);
        SetParameters(kernelID, Shader);
        Shader.Dispatch(kernelID, resolutionX / DispatchGroupSize.x, resolutionY / DispatchGroupSize.y, 1);
    }

    protected void MakeTexture(ref RenderTexture texture) {
        if (texture != null) texture.Release();
        texture = new RenderTexture(Resolution.x, Resolution.y, Resolution.z, RenderTextureFormat.ARGB32);
        texture.enableRandomWrite = true;
        texture.Create();
    }

    protected void MakeTextureFromDesc(ref RenderTexture texture, RenderTextureDescriptor desc) {
        if(texture != null) texture.Release();
        texture = new RenderTexture(desc);
        texture.enableRandomWrite = true;
        texture.Create();
    }

    private void OnEnable() {
        RenderPipelineManager.endFrameRendering += RenderPipelineManager_endCameraRendering;
    }

    private void OnDisable() {
        RenderPipelineManager.endFrameRendering -= RenderPipelineManager_endCameraRendering;
    }

    private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera[] cameras) {
        OnPostRender();
    }
}