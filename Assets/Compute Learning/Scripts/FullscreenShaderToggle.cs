using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FullscreenShaderToggle : MonoBehaviour {
}

[CustomEditor(typeof(FullscreenShaderToggle))]
public class FullscreenShaderToggleEditor : Editor {
    private FullscreenShaderToggle toggle;
    private Vector2 scrollPosition;
    private int childCount;
    private static bool showDuration = true;
    private static bool autoRefresh;
    private readonly Color backgroundColor = new Color(0.09f, 0.09f, 0.09f);
    
    private void OnEnable() {
        toggle = target as FullscreenShaderToggle;
    }

    public override void OnInspectorGUI() {
        var shaders = toggle.GetComponentsInChildren<ComputeRunner3>();
        if(shaders.Length != childCount) scrollPosition = Vector2.zero;
        childCount = shaders.Length;

        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh");
        showDuration = GUILayout.Toggle(showDuration, "Show Duration");
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();
        var defaultColor = GUI.backgroundColor;
        foreach (var shader in shaders) {
            GUI.backgroundColor = backgroundColor;
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = defaultColor;
            if (shader.Active) GUI.backgroundColor = Color.green;
            
            GUILayout.BeginHorizontal("box");
            GUI.backgroundColor = defaultColor;
            GUILayout.Label($"{shader.gameObject.name} ({shader.ComputeShader.name})", GUILayout.ExpandWidth (false));
            GUILayout.FlexibleSpace();
            GUI.enabled = !shader.Active;
            if (GUILayout.Button("Set active", GUILayout.MinWidth(175))) {
                foreach (var shader2 in shaders) {
                    if(shader == shader2) continue;
                    shader2.SetShaderActive(false);
                }
                shader.SetShaderActive(true);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (showDuration) {
                GUILayout.BeginHorizontal("box");
                var durationMicro = shader.RenderTime * 1000;
                GUILayout.Label($"Duration: {durationMicro:F4}μs ({shader.RenderTime:F6} ms)");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        GUI.backgroundColor = defaultColor;
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        
        if(autoRefresh) EditorUtility.SetDirty(toggle);
    }
}