using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MouseLightingCompute : ComputeRunner {
    private RectTransform rectTransform;
    private Vector2 positionScreenSpaceNormalized;
    private Vector2 sizeScreenSpaceNormalized;

    protected override void OnAwake() {
        rectTransform = GetComponent<RectTransform>();
    }

    protected override void OnUpdate() {
        var screenSize = new Vector2(Screen.width, Screen.height);
        var positionScreenSpace = Utils.RectTransformToScreenSpace(rectTransform);
        positionScreenSpaceNormalized = new Vector2(positionScreenSpace.x / screenSize.x, positionScreenSpace.y / screenSize.y);
        sizeScreenSpaceNormalized = new Vector2(positionScreenSpace.width / screenSize.x, positionScreenSpace.height / screenSize.y);
    }

    private Vector2 GetMouseNormalized() {
        var screenSize = new Vector2(Screen.width, Screen.height);
        var mousePosition = Input.mousePosition;
        return new Vector2(mousePosition.x / screenSize.x, mousePosition.y / screenSize.y);
    }

    protected override void SetParameters(int kernelID, ComputeShader shader) {
        var mouseNormalized = GetMouseNormalized();
        var positionNormalized = new Vector4(mouseNormalized.x, mouseNormalized.y, positionScreenSpaceNormalized.x, positionScreenSpaceNormalized.y);
        shader.SetVector("PositionNormalized", positionNormalized);
        shader.SetVector("SizeNormalized", sizeScreenSpaceNormalized);
    }
}