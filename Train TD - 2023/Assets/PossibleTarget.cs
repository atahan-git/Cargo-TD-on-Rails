using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;
using UnityEngine.UI;

public class PossibleTarget : MonoBehaviour, IActiveDuringCombat {
    [HideInInspector]
    public int myId;
    
    public enum Type {
        enemy, player
    };

    public Type myType;

    public Transform targetTransform;

    public bool avoid = false;
    public bool flying = false;

    private bool isCartTarget = false;
    private Cart myCart;
    private HighlightEffect _outline;
    private void OnEnable() {
        if (targetTransform == null) {
            var building = GetComponent<Cart>();

            if (building != null) {
                targetTransform = building.GetShootingTargetTransform();
            } else {
                targetTransform = transform;
            }
        }
        LevelReferences.allTargets.Add(this);
        LevelReferences.targetsDirty = true;

        myCart = GetComponent<Cart>();
        _outline = GetComponentInChildren<HighlightEffect>();
        isCartTarget = myCart != null;
    }

    private void OnDisable() {
	    LevelReferences.allTargets.Remove(this);
        LevelReferences.targetsDirty = true;
    }

    public float GetHealth() {
        if (myType == Type.player) {
            return GetComponent<ModuleHealth>().currentHealth;
        } else {
            return GetComponent<EnemyHealth>().currentHealth;
        }
    }

    public void ActivateForCombat() {
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }
    
    Vector3 previous;
    public Vector3 velocity = Vector3.zero;

    void Update() {
        if (Time.deltaTime > 0) {
            var newVelocity = ((transform.position - previous)) / Time.deltaTime;
            velocity = Vector3.Lerp(velocity, newVelocity, 1 * Time.deltaTime);
            previous = transform.position;
        }

        if (isCartTarget) {
            if (myCart != PlayerWorldInteractionController.s.selectedCart) {
                if (enemiesTargetingMe.Count > 0) {
                    var totalWidth = 0f;
                    for (int i = 0; i < enemiesTargetingMe.Count; i++) {
                        if (enemiesTargetingMe[i] != null)
                            totalWidth += enemiesTargetingMe[i].currentWidth + 0.5f;
                    }

                    _outline.enabled = totalWidth > 0;
                    _outline.outlineColor = Color.red;
                    _outline.outlineWidth = totalWidth;
                } else {
                    _outline.enabled = false;
                }
            }
        }
    }


    public List<EnemyTargetShower> enemiesTargetingMe = new List<EnemyTargetShower>();
}
