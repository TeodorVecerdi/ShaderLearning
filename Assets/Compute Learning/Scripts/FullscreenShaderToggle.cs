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
        var shaders = toggle.GetComponentsInChildren<ComputeRunner2>();
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
            GUILayout.Label($"{shader.gameObject.name} ({shader.Shader.name})", GUILayout.ExpandWidth (false));
            GUILayout.FlexibleSpace();
            GUI.enabled = !shader.Active;
            if (GUILayout.Button("Set active", GUILayout.MinWidth(175))) {
                foreach (var shader2 in shaders) {
                    if(shader == shader2) continue;
                    shader2.SetActive(false);
                }
                shader.SetActive(true);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (showDuration) {
                GUILayout.BeginHorizontal("box");
                var durationMicro = shader.DurationMs * 1000;
                GUILayout.Label($"Duration: {durationMicro}μs ({shader.DurationMs} ms)");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("RUN", GUILayout.MinWidth(175))) {
                    shader.Render();
                }

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