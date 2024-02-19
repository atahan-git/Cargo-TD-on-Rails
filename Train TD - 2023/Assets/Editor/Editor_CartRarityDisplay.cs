using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class Editor_CartRarityDisplay {
    /*static Editor_CartRarityDisplay() {
        EditorApplication.projectWindowItemOnGUI -= ProjectWindowItemOnGUICallback;
        EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUICallback;
    }

    static void ProjectWindowItemOnGUICallback(string guid, Rect selectionRect) {
        // If the width is too wide, we don't need to put the icon.
        if (selectionRect.width / selectionRect.height >= 2) {
            return;
        }

        string fileName = AssetDatabase.GUIDToAssetPath(guid);

        // If we're a directory, ignore it.
        if (fileName.LastIndexOf('.') == -1) {
            return;
        }

        // If we're not a prefab, ignore it
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(fileName);
        if (go == null) {
            return;
        }

        // If we're not a variant, ignore it.
        if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.Variant) {
            return;
        }
        
        
        GUIContent icon;
        var cart = go.GetComponent<Cart>();
        var artifact = go.GetComponent<Artifact>();
        UpgradesController.CartRarity myRarity = UpgradesController.CartRarity.common;
        if (cart != null) {
            myRarity = cart.myRarity;
        }else if (artifact != null) {
            myRarity = artifact.myRarity;
        } else {
            return;
        }
        
        switch (myRarity) {
            case UpgradesController.CartRarity.special:
                icon = EditorGUIUtility.IconContent("sv_icon_dot3_pix16_gizmo");
                    
                break;
            case UpgradesController.CartRarity.boss:
                icon = EditorGUIUtility.IconContent("sv_icon_dot6_pix16_gizmo");
                    
                break;
            case UpgradesController.CartRarity.epic:
                icon = EditorGUIUtility.IconContent("sv_icon_dot5_pix16_gizmo");
                
                break;
            case UpgradesController.CartRarity.rare:
                icon = EditorGUIUtility.IconContent("sv_icon_dot1_pix16_gizmo");
                
                break;
            case UpgradesController.CartRarity.common:
            default:
                
                icon = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo");
                break;
        }


        

        

        GUI.Label(GetRect(true, selectionRect), icon);

        var needsToBeBought = false;
        var cost = 0;
        if (cart != null) {
            needsToBeBought = cart.needsToBeBought;
            cost = cart.buyCost;
        }

        if (artifact != null) {
            needsToBeBought = artifact.needsToBeBought;
            cost = artifact.buyCost;
        }
        
        if (needsToBeBought) {
            icon = EditorGUIUtility.IconContent("sv_icon_dot10_pix16_gizmo");
            if (cost > 50) {
                icon = EditorGUIUtility.IconContent("sv_icon_dot12_pix16_gizmo");
            }
            if (cost > 500) {
                icon = EditorGUIUtility.IconContent("sv_icon_dot13_pix16_gizmo");
            }
                
            GUI.Label(GetRect(false, selectionRect), icon);
        }
    }

   static Rect GetRect(bool isOnRight, Rect selectionRect) {
        Vector2 selectionPosition = selectionRect.position;
        Vector2 selectionSize = selectionRect.size;

        float xPosition;
        if (isOnRight) {
            // The x position should be a little left of the right edge.
            xPosition = selectionPosition.x + selectionSize.x - selectionSize.x / 3.5f;
        } else {
            xPosition = selectionPosition.x ;
        }

        // The y position should be a little above of the bottom edge.
        float yPosition = selectionPosition.y + selectionSize.y - selectionSize.y / 2;

        Vector2 position = new Vector2(xPosition, yPosition);

        Rect newRect = selectionRect;
        newRect.position = position;

        // This is a nice number to reduce the size of the rect by to make the icon smaller
        newRect.size /= 3.5f;

        return newRect;
    }*/
}