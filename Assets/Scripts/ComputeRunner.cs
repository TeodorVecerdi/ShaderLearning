using UnityEngine;
using UnityEngine.UI;

public class ComputeRunner : MonoBehaviour {
    [SerializeField] private ComputeShader Shader;
    [SerializeField] private int Resolution = 512;
    [SerializeField] private RenderTexture RenderTexture;
    [SerializeField] private RawImage Renderer;

    private void Start() {
        RenderTexture = new RenderTexture(Resolution, Resolution, 24);
        RenderTexture.enableRandomWrite = true;
        RenderTexture.Create();
        Renderer.texture = RenderTexture;
        var program = Shader.FindKernel("CSMain");
        Debug.Log(program);
    }

    private void Update() {
        var program = Shader.FindKernel("CSMain");
        Shader.SetTexture(program, "Result", RenderTexture);
        Shader.SetFloat("Resolution", Resolution);
        Shader.Dispatch(program, Resolution/8, Resolution/8, 1);
    }
}
