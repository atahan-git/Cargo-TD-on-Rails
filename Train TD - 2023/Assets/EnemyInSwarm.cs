using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

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
    private Vector2 teleportDamageRanges = new Vector2(0f, 0.5f);
    private Vector2 teleportChances = new Vector2(0f, 0.5f);

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
        var gunModule = GetComponentInChildren<EnemyGunModule>();

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

    private void Update() {
        if (teleportCooldown > 0) {
            teleportCooldown -= Time.deltaTime;
        }
    }


    public bool teleportInProgress = false;
    public void TookDamage(float percent) {
        if (isTeleporting && !teleportInProgress && teleportCooldown <= 0f) {
            var amount = percent.Remap(0, 1, teleportDamageRanges.x, teleportDamageRanges.y);
            amount = Mathf.Clamp01(amount);

            var chance = amount.Remap(0, 1, teleportChances.x, teleportChances.y);

            if (Random.value <= chance) {
                Teleport();
            }
        }
    }
    
    private GameObject teleportStartEffect;

    public float teleportCooldown = 2;
    [Button] 
    void Teleport() {
        teleportInProgress = true;
        teleportCooldown = 2f;

        var effectTarget = GetComponent<EnemyHealth>().aliveObject;
        teleportStartEffect = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.teleportFromEffect, effectTarget);
        
        Invoke(nameof(FinishTeleport),0.2f);
    }

    void FinishTeleport() {
        //var teleportPoint = GetRandomPointInCollider(mySwarm.GetComponent<Collider>());
        var teleportPoint = GetRandomPointNearTrain();
        
        var effectTarget = GetComponent<EnemyHealth>().aliveObject;

        if (teleportStartEffect != null) {
            teleportStartEffect.transform.SetParent(null);
        }
        
        transform.position = teleportPoint;
        
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.teleportToEffect, effectTarget);
        
        teleportInProgress = false;
    }

    Vector3 GetRandomPointNearTrain() {
        var changeSide = Random.value < 0.5f;

        if (changeSide) {
            mySwarm.activeEnemies.Remove(this);
            var potentialSwarms = mySwarm.transform.parent.GetComponentsInChildren<EnemySwarmMaker>();
            for (int i = 0; i < potentialSwarms.Length; i++) {
                if (potentialSwarms[i] != mySwarm) {
                    print($"{mySwarm.gameObject.name} - {potentialSwarms[i].gameObject.name} - {i}");
                    mySwarm = potentialSwarms[i];
                    mySwarm.activeEnemies.Add(this);
                    break;
                }
            }
        }
        
        
        var lengthRange = Train.s.GetTrainLength() + 1f;
        var widthRange = 2f;
        var minX = 0.8f;

        var isLeft = mySwarm.transform.localPosition.x > 0;

        var distance = Random.Range(-lengthRange / 2f, lengthRange / 2f);
        var newPos = PathAndTerrainGenerator.s.GetPointOnActivePath(distance);
        newPos += Quaternion.AngleAxis(90, Vector3.up) * PathAndTerrainGenerator.s.GetDirectionVectorOnActivePath(distance) * Random.Range(minX, minX + widthRange) * (isLeft? 1 : -1);

        return newPos + Vector3.up*transform.position.y;
    }
    
    Vector3 GetRandomPointInCollider(Collider collider)
    {
        Vector3 randomPoint = Vector3.zero;
        Bounds bounds = collider.bounds;

        var n = 0;
        // Iterate until a valid point is found
        while (true)
        {
            // Generate a random point within the bounds
            randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Check if the random point is inside the collider
            if (IsPointInsideCollider(collider, randomPoint))
            {
                break;
            }

            n++;
            if (n > 10) {
                break;
            }
        }

        return randomPoint;
    }

    bool IsPointInsideCollider(Collider collider, Vector3 point)
    {
        // Check if the point is inside the collider using a simple physics check
        Vector3 direction = collider.bounds.center - point;
        float distance = direction.magnitude;

        // Cast a ray from the point towards the center of the collider
        if (Physics.Raycast(point, direction, out RaycastHit hit, distance))
        {
            // Check if the raycast hit the original collider
            if (hit.collider == collider)
            {
                return true;
            }
        }

        return false;
    }
}
