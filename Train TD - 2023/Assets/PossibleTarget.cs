using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;
using UnityEngine.UI;

public class PossibleTarget : MonoBehaviour {
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
    private void Start() {
        if (targetTransform == null) {
            var building = GetComponent<Cart>();

            if (building != null) {
                targetTransform = building.GetShootingTargetTransform();
            } else {
                targetTransform = transform;
            }
        }
        LevelReferences.s.allTargets.Add(this);
        LevelReferences.s.targetsDirty = true;

        myCart = GetComponent<Cart>();
        _outline = GetComponent<HighlightEffect>();
        isCartTarget = myCart != null;

        CacheHealths();
    }

    private void OnDestroy() {
	    LevelReferences.s.allTargets.Remove(this);
        LevelReferences.s.targetsDirty = true;
    }


    private ModuleHealth _moduleHealth;
    private EnemyHealth _enemyHealth;
    void CacheHealths() {
        _moduleHealth = GetComponent<ModuleHealth>();
        _enemyHealth = GetComponent<EnemyHealth>();
    }

    public float GetHealth() {
        if(_moduleHealth != null)
            return _moduleHealth.GetHealth();

        if (_enemyHealth != null)
            return _enemyHealth.GetHealth();

        return 0;
    }
    
    public float GetHealthPercent() {
        if(_moduleHealth != null)
            return _moduleHealth.GetHealthPercent();

        if (_enemyHealth != null)
            return _enemyHealth.GetHealthPercent();

        return 0;
    }

    Vector3 previous;
    public Vector3 velocity = Vector3.zero;

    void Update() {
        if (Time.deltaTime > 0) {
            var newVelocity = ((transform.position - previous)) / Time.deltaTime;
            newVelocity -= Train.s.GetTrainForward() * LevelReferences.s.speed;
            velocity = Vector3.Lerp(velocity, newVelocity, 1 * Time.deltaTime);
            previous = transform.position;
        }

        /*if (isCartTarget) {
            var selectedCart = PlayerWorldInteractionController.s.currentSelectedThing as Cart;
            if (myCart != selectedCart) {
                if (enemiesTargetingMe.Count > 0) {
                    var totalWidth = 0f;
                    for (int i = 0; i < enemiesTargetingMe.Count; i++) {
                        if (enemiesTargetingMe[i] != null)
                            totalWidth += enemiesTargetingMe[i].currentWidth + 0.5f;
                    }

                    _outline.highlighted = totalWidth > 0;
                    _outline.outlineColor = Color.red;
                    _outline.outlineWidth = totalWidth;
                } else {
                    _outline.highlighted = false;
                }
            }
        }*/
    }


    public List<EnemyTargetShower> enemiesTargetingMe = new List<EnemyTargetShower>();
}
