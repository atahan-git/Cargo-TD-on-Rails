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

    [ReadOnly]
    public ModuleAmmo moduleAmmo;

    public bool isRepairAmmo = false;
    void Start() {
        moduleAmmo = GetComponentInParent<ModuleAmmo>();
        moduleAmmo.OnReload.AddListener(OnReload);
        moduleAmmo.OnUse.AddListener(OnUse);
        moduleAmmo.OnAmmoTypeChange.AddListener(OnAmmoTypeChange);
        OnAmmoTypeChange();
        OnReload(false);
        OnUse();
    }


    void OnUse() {
        while ( allAmmoChunks.Count > moduleAmmo.curAmmo) {
            var firstOne = allAmmoChunks[0];
            allAmmoChunks.RemoveAt(0);
            velocity.RemoveAt(0);
            Destroy(firstOne);
        }
    }


    void OnAmmoTypeChange() {
        if (isRepairAmmo) {
            ammoChunk = LevelReferences.s.bullet_repair;
        } else {
            ammoChunk = LevelReferences.s.bullet_regular;
            if (moduleAmmo.isFire && moduleAmmo.isSticky && moduleAmmo.isExplosive) {
                ammoChunk = LevelReferences.s.bullet_fire_sticky_explosive;
            } else if (moduleAmmo.isFire && moduleAmmo.isSticky) {
                ammoChunk = LevelReferences.s.bullet_fire_sticky;
            } else if (moduleAmmo.isFire && moduleAmmo.isExplosive) {
                ammoChunk = LevelReferences.s.bullet_fire_explosive;
            } else if (moduleAmmo.isSticky && moduleAmmo.isExplosive) {
                ammoChunk = LevelReferences.s.bullet_sticky_explosive;
            } else if (moduleAmmo.isFire) {
                ammoChunk = LevelReferences.s.bullet_fire;
            } else if (moduleAmmo.isSticky) {
                ammoChunk = LevelReferences.s.bullet_sticky;
            } else if (moduleAmmo.isExplosive) {
                ammoChunk = LevelReferences.s.bullet_explosive;
            }
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
    }

    void OnReload(bool showEffect) {
        var delta = Vector3.zero;
        while ( allAmmoChunks.Count < moduleAmmo.curAmmo) {
            var newOne = Instantiate(ammoChunk, reloadSpawnPos);
            newOne.transform.position += delta + new Vector3(Random.Range(-0.005f, 0.005f), 0, Random.Range(-0.005f, 0.005f));
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
        var targetY = noAmmoPos.transform.position.y;
        for (int i = 0; i < allAmmoChunks.Count; i++) {
            var target = allAmmoChunks[i].transform.position;
            target.y = targetY;
            if (allAmmoChunks[i].transform.position.y > target.y) {
                allAmmoChunks[i].transform.position = Vector3.MoveTowards(allAmmoChunks[i].transform.position, target, velocity[i] * Time.deltaTime);
                velocity[i] += acceleration * Time.deltaTime;
            } else {
                velocity[i] = 0;
            }

            targetY += ammoChunkHeight;
        }
    }
}
