using System;
using System.Diagnostics;
using NaughtyAttributes;
using UnityCommons;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class LifeCompute : MonoBehaviour {
    [SerializeField] private BakedType<Vector3Int> Resolution = new Vector3Int(2048, 1024, 32);
    [SerializeField] private BakedType<Vector2Int> ThreadGroupSize = new Vector2Int(8, 8);
    [SerializeField] private BakedType<int> CellSize = 8;
    [Space(4)]
    [SerializeField, Required] private ComputeShader DisplayShader;
    [SerializeField, Required] private ComputeShader GameShader;
    [SerializeField, Required] private ComputeShader InitializeShader;
    [SerializeField, Required] private RawImage Display;
    [Space(4)]
    [SerializeField] private float StepTime = 0.5f;
    [SerializeField, DisableIf("RandomSeed")] private float Seed = 0;
    [SerializeField] private bool RandomSeed = false;
    [Space(4)]
    [SerializeField] private Color GridColor = Color.black;
    [SerializeField] private Color CellColor = Color.white;
    [SerializeField] private Color GridLineColor = Color.gray;
    [Space(20), SerializeField, ReadOnly] private string Duration;
    private BakedType<Vector3Int> gameResolution;
    
    private RenderTexture workingGameTexture;
    private RenderTexture gameTexture;
    private RenderTexture workingTexture;
    private RenderTexture displayTexture;
    private ComputeBuffer dummyRenderCheckBuffer;
    private ComputeBuffer dummyStepCheckBuffer;
    private readonly int[] renderCheckBufferData = {0};
    private readonly int[] stepCheckBufferData = {0};

    private bool lastFrameReady = true;
    private bool stepReady = false;
    private readonly Stopwatch timer = new Stopwatch();
    private double duration = 1e-6;
    private float stepTimer = 0.0f;

    private void Awake() {
        Resolution.Bake();
        ThreadGroupSize.Bake();
        CellSize.Bake();

        MakeBuffer(ref dummyRenderCheckBuffer, 1, sizeof(int), renderCheckBufferData);
        MakeBuffer(ref dummyStepCheckBuffer, 1, sizeof(int), stepCheckBufferData);
        MakeTexture(ref workingTexture, Resolution);
        MakeTexture(ref displayTexture, Resolution);

        gameResolution = Resolution.Value / CellSize.Value;
        gameResolution.Bake();

        MakeTexture(ref workingGameTexture, gameResolution);
        MakeTexture(ref gameTexture, gameResolution);
        Display.texture = displayTexture;
    }

    private void Start() {
        if (RandomSeed) Seed = Rand.Float * 1e2f;
        
        InitializeShader.SetTexture(0, "Game", workingGameTexture);
        InitializeShader.SetBuffer(0, "StepCheckBuffer", dummyStepCheckBuffer);
        InitializeShader.SetFloat("Seed", Seed);
        var threadGroupsX = Mathf.CeilToInt(gameResolution.Value.x / (float)ThreadGroupSize.Value.x);
        var threadGroupsY = Mathf.CeilToInt(gameResolution.Value.y / (float)ThreadGroupSize.Value.y);
        InitializeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        AsyncGPUReadback.Request(dummyStepCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyStepCheckBuffer)</b> has error");
            Graphics.Blit(workingGameTexture, gameTexture);
            stepReady = true;
        });
    }

    private void OnDestroy() {
        if (dummyRenderCheckBuffer != null) dummyRenderCheckBuffer.Release();
        if (dummyStepCheckBuffer != null) dummyStepCheckBuffer.Release();
        if (workingTexture != null) workingTexture.Release();
        if (displayTexture != null) displayTexture.Release();
        if (workingGameTexture != null) workingGameTexture.Release();
        if (gameTexture != null) gameTexture.Release();
    }

    private void Update() {
        if(!stepReady) return;
        stepTimer += Time.deltaTime;
        if (stepTimer > StepTime) {
            stepTimer -= StepTime;
            stepReady = false;
            Step();
        }
    }

    private void Step() {
        GameShader.SetTexture(0, "Game", gameTexture);
        GameShader.SetTexture(0, "WorkingGame", workingGameTexture);
        GameShader.SetBuffer(0, "StepCheckBuffer", dummyStepCheckBuffer);
        var threadGroupsX = Mathf.CeilToInt(gameResolution.Value.x / (float)ThreadGroupSize.Value.x);
        var threadGroupsY = Mathf.CeilToInt(gameResolution.Value.y / (float)ThreadGroupSize.Value.y);
        GameShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        OnStep();
    }

    private void OnStep() {
        AsyncGPUReadback.Request(dummyStepCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyStepCheckBuffer)</b> has error");
            Graphics.Blit(workingGameTexture, gameTexture);
            stepReady = true;
        });
    }

    private void Render() {
        DisplayShader.SetInt("CellSize", CellSize);
        DisplayShader.SetVector("GridColor", new Vector3(GridColor.r, GridColor.g, GridColor.b));
        DisplayShader.SetVector("CellColor", new Vector3(CellColor.r, CellColor.g, CellColor.b));
        DisplayShader.SetVector("GridLineColor", new Vector3(GridLineColor.r, GridLineColor.g, GridLineColor.b));
        
        DisplayShader.SetTexture(0, "Game", gameTexture);
        DisplayShader.SetTexture(0, "Result", workingTexture);
        DisplayShader.SetBuffer(0, "RenderCheckBuffer", dummyRenderCheckBuffer);
        var threadGroupsX = Mathf.CeilToInt(gameResolution.Value.x / (float)ThreadGroupSize.Value.x);
        var threadGroupsY = Mathf.CeilToInt(gameResolution.Value.y / (float)ThreadGroupSize.Value.y);
        DisplayShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        OnRender();
    }

    private void OnRender() {
        AsyncGPUReadback.Request(dummyRenderCheckBuffer, request => {
            if (request.hasError) Debug.Log("<b>AsyncGPUReadback.Request(dummyRenderCheckBuffer)</b> has error");
            lastFrameReady = true;
            Graphics.Blit(workingTexture, displayTexture);
        });
    }

    private void Run() {
        if (!lastFrameReady) return;
        lastFrameReady = false;
        duration = timer.Elapsed.TotalMilliseconds;
        Duration = $"{duration:F4}ms ({(1000.0 / duration):F1} fps)";
        timer.Restart();

        Render();
    }

    private void MakeTexture(ref RenderTexture texture, Vector3Int textureResolution, RenderTextureFormat format = RenderTextureFormat.ARGB32, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Linear) {
        if (texture != null) texture.Release();
        texture = new RenderTexture(textureResolution.x, textureResolution.y, textureResolution.z, format) {enableRandomWrite = true};
        texture.Create();
    }
    
    private void MakeBuffer(ref ComputeBuffer computeBuffer, int count, int stride, Array data) {
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