using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemySwarmMaker : MonoBehaviour {
    private void Start() {
        GetComponent<Collider>().isTrigger = true;
    }

    public List<EnemyInSwarm> activeEnemies = new List<EnemyInSwarm>();

    public void EnemySpawn(EnemyHealth enemyHealth) {
        var enemyInSwarm = enemyHealth.GetComponent<EnemyInSwarm>();
        activeEnemies.Add(enemyInSwarm);
        enemyInSwarm.mySwarm = this;
        enemyInSwarm.myWave = GetComponentInParent<EnemyWave>();
        enemyHealth.transform.SetParent(transform.parent);
        //enemyHealth.transform.position += Random.onUnitSphere;
    }

    public void EnemyDeath(EnemyHealth enemyHealth, bool playDeathSounds = true) {
        activeEnemies.Remove(enemyHealth.GetComponent<EnemyInSwarm>());

        var enemyInSwarm = enemyHealth.GetComponent<EnemyInSwarm>();

        if (playDeathSounds) {
            if(enemyInSwarm.enemyDieSounds.Length > 0)
                SoundscapeController.s.PlayEnemyDie(enemyInSwarm.enemyDieSounds[Random.Range(0,enemyInSwarm.enemyDieSounds.Length)]);
        }

        if (activeEnemies.Count <= 0) {
            //Destroy(GetComponentInParent<EnemyWave>().gameObject);
            GetComponentInParent<EnemyWave>().CheckDestroySelf();
            Destroy(gameObject);
        }
    }

    public bool unsquished = false;
    public Vector3 swarmCenter;
    public Vector3 swarmAverageVelocity;

    [Button]
    public Vector3 GetPositionInCollider() {
        var collider = GetComponent<Collider>();
        var randomPoint = collider.bounds.RandomPointInsideBounds();

        var trialNum = 100;
        bool success = false;
        for (int i = 0; i < trialNum; i++) {
            var pointInsideCollider = collider.ClosestPoint(randomPoint);
            
            if (Vector3.Distance(pointInsideCollider, randomPoint) < 0.01f) {
                randomPoint = pointInsideCollider;
                //Debug.DrawLine(randomPoint,randomPoint+Vector3.up*4, Color.green, 1f);
                success = true;
                break;
            } /*else {
                Debug.DrawLine(randomPoint,pointInsideCollider, Color.red, 0.5f);
            }*/
            
            randomPoint = collider.bounds.RandomPointInsideBounds();
        }

        if (!success) {
            randomPoint = collider.ClosestPoint(randomPoint);
        }

        return randomPoint;
    }

}