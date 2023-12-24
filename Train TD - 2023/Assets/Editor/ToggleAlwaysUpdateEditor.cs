using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ToggleAlwaysUpdateEditor
{
    [MenuItem("Tools/EnableAlwaysUpdateEditor")]
    static void EnableAlwaysUpdateEditor() {
        foreach (SceneView sceneView in SceneView.sceneViews) {
            sceneView.sceneViewState.alwaysRefresh = true;
        }
    }

    [MenuItem("Tools/DisableAlwaysUpdateEditor")]
    static void DisableAlwaysUpdateEditor() {
        foreach (SceneView sceneView in SceneView.sceneViews) {
            sceneView.sceneViewState.alwaysRefresh = false;
        }
    }
}
