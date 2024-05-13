using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInSwarm : MonoBehaviour
{
    public Sprite enemyIcon;
    
    // 3 speed ~= regular speed
    // 0.25 speed ~= min train sped
    // 10 speed ~= max speed
    public float speed = 5;

    public bool primeEnemy = false; // set this to true if you want a particular enemy enter sound to be preferred over others
    public AudioClip[] enemyEnterSounds;
    public AudioClip[] enemyDieSounds;
    
    public bool isTeleporting = false;
    public Vector2 teleportTiming = new Vector2(10, 30);

    public bool isStealing = false;

    public bool isNuker = false;

    public float nukingTime = 20;

    [Tooltip("This gets auto set")]
    public bool isElite;

    public enum EnemyType {
        Deadly, Safe
    }

    public EnemyType myType = EnemyType.Deadly;
    
    public Sprite GetGunSprite() {
        var gunModule = GetComponentInChildren<GunModule>();

        if (gunModule != null) {
            return gunModule.gunSprite;
        } else {
            return null;
        }
    }
    
    [Header("Dynamically set:")]
    public Collider mainCollider;

    public Vector3 boilRealDelta;
    public Vector3 boidTargetDelta;

    public EnemySwarmMaker mySwarm;
    public EnemyWave myWave;
    private void Start() {
        mainCollider = GetComponent<EnemyHealth>().GetMainCollider();
    }
}
