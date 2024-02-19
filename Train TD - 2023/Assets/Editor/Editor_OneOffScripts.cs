using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEditor;

public class Editor_OneOffScripts : EditorWindow
{
    
    [MenuItem("Tools/Editor One Off Scripts")]
    static void Init()
    {
        Editor_OneOffScripts window = (Editor_OneOffScripts)EditorWindow.GetWindow(typeof(Editor_OneOffScripts));
        window.Show();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Add and move Enemy in Swarm"))
        {
            AddAndMoveComponent();
        }
        
        if (GUILayout.Button("Unpack Prefab"))
        {
            UnpackPrefab();
        }
    }

    void AddAndMoveComponent() {

        GameObject enemy = null;
        GameObject swarmMaker = null;
        // Iterate through selected prefabs
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject.GetComponent<EnemyHealth>()) {
                enemy = selectedObject;
            }

            if (selectedObject.GetComponent<EnemySwarmMaker>()) {
                swarmMaker = selectedObject;
            }
        }

        if (enemy == null || swarmMaker == null) {
            return;
        }

        EnemyInSwarm enemyInSwarm = enemy.GetComponent<EnemyInSwarm>();

        if (enemyInSwarm == null) {
            enemyInSwarm = enemy.AddComponent<EnemyInSwarm>();
        }

        // Move the new component to the top of the components list
        Component[] components = enemy.GetComponents<Component>();

        // Find the index of the new component
        int newIndex = Array.IndexOf(components, enemyInSwarm);

        for (int i = 0; i < newIndex; i++) {
            UnityEditorInternal.ComponentUtility.MoveComponentUp(enemyInSwarm);
        }

        EnemySwarmMaker enemySwarmMaker = swarmMaker.GetComponent<EnemySwarmMaker>();

        /*
        enemyInSwarm.enemyIcon = enemySwarmMaker.enemyIcon;
        enemyInSwarm.speed = enemySwarmMaker.speed;
        enemyInSwarm.enemyEnterSounds = enemySwarmMaker.enemyEnterSounds;
        enemyInSwarm.enemyDieSounds = enemySwarmMaker.enemyDieSounds;
        enemyInSwarm.isTeleporting = enemySwarmMaker.isTeleporting;
        enemyInSwarm.teleportTiming = enemySwarmMaker.teleportTiming;
        enemyInSwarm.isStealing = enemySwarmMaker.isStealing;
        enemyInSwarm.isNuker = enemySwarmMaker.isNuker;
        enemyInSwarm.nukingTime = enemySwarmMaker.nukingTime;*/
        
        Debug.Log("success!");

    }
    
    
    void UnpackPrefab() {

        GameObject myCart = null;
        // Iterate through selected prefabs
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject.GetComponent<Cart>()) {
                myCart = selectedObject;
            }
        }

        if (myCart == null) {
            return;
        }
        
        PrefabUtility.UnpackPrefabInstance(myCart, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
        
        Debug.Log("success!");
    }
}
