using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class PhysicalAmmoBar : MonoBehaviour {
    
    [ReadOnly]
    public GameObject ammoChunk;
    public float ammoChunkHeight;

    public List<GameObject> allAmmoChunks = new List<GameObject>();
    public List<float> velocity = new List<float>();

    public Transform noAmmoPos;
    public Transform reloadSpawnPos;

    public bool ammoTypeSet = false;

    void Start() {
        if (!ammoTypeSet) {
            OnAmmoTypeChange();
        }
    }


    public void OnUse(float curAmmo) {
        while ( allAmmoChunks.Count > curAmmo) {
            var firstOne = allAmmoChunks[0];
            allAmmoChunks.RemoveAt(0);
            velocity.RemoveAt(0);
            Destroy(firstOne);
        }
    }


    public void OnAmmoTypeChange() {
        if (GetComponentInParent<Cart>()) {
            ammoChunk = LevelReferences.s.ammo_player;
        } else {
            ammoChunk = LevelReferences.s.ammo_enemy;
        }

        var oldAmmo = new List<GameObject>(allAmmoChunks);
        allAmmoChunks.Clear();
        
        for (int i = oldAmmo.Count-1; i >= 0; i--) {
            var chunk = Instantiate(ammoChunk, oldAmmo[i].transform.position, oldAmmo[i].transform.rotation);
            chunk.transform.SetParent(transform);
            allAmmoChunks.Add(chunk);
            Destroy(oldAmmo[i].gameObject);
        }
        
        allAmmoChunks.Reverse();
        ammoTypeSet = true;
    }

    public void OnReload(bool showEffect, float curAmmo) {
        var delta = Vector3.zero;
        while ( allAmmoChunks.Count < curAmmo) {
            var newOne = Instantiate(ammoChunk, reloadSpawnPos);
            newOne.transform.localPosition += delta + new Vector3(Random.Range(-0.005f, 0.005f), 0, Random.Range(-0.005f, 0.005f));
            newOne.transform.SetParent(transform);
            newOne.SetActive(true);
            allAmmoChunks.Add(newOne);
            if (showEffect) {
                velocity.Add(0);
            } else {
                velocity.Add(100);
            }
            delta.y += ammoChunkHeight;
        }
        
        if(!showEffect)
            Update();
    }


    private float acceleration = 2;
    private void Update() {
        var targetY = noAmmoPos.transform.localPosition.y;
        for (int i = 0; i < allAmmoChunks.Count; i++) {
            var target = allAmmoChunks[i].transform.localPosition;
            target.y = targetY;
            if (allAmmoChunks[i].transform.localPosition.y > target.y) {
                allAmmoChunks[i].transform.localPosition = Vector3.MoveTowards(allAmmoChunks[i].transform.localPosition, target, velocity[i] * Time.deltaTime);
                velocity[i] += acceleration * Time.deltaTime;
            } else {
                allAmmoChunks[i].transform.localPosition = target;
                velocity[i] = 0;
            }

            targetY += ammoChunkHeight;
        }
    }
}
