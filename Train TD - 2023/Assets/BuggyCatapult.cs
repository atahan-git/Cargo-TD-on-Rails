using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class BuggyCatapult : MonoBehaviour, IEnemyEquipment {
    public Sprite gunSprite;
    
    
    [HideInInspector]
    public UnityEvent onBulletFiredEvent = new UnityEvent();

    public List<GameObject> curAmmo = new List<GameObject>();

    public GameObject buggyBulletPrefab;
    public Transform shootSpawnPos;

    public float timer = 20;

    private void Start() {
        timer = 25;
    }


    [Button]
    public void SetAmmoCount(int count) {
        count = Mathf.Clamp(count, 1, 30);

        for (int i = curAmmo.Count-1; i >= 1; i--) {
            DestroyImmediate(curAmmo[i]);
            curAmmo.RemoveAt(i);
        }
        
        float ammoYIncrease = 0.1202f;
        for (int i = 0; i < count-1; i++) {
            var ammo = Instantiate(curAmmo[0], curAmmo[0].transform.parent);
            curAmmo.Add(ammo);
            ammo.transform.localPosition += Vector3.up*ammoYIncrease*(i+1);
            ammo.gameObject.name = curAmmo[0].name + $" ({i+1})";
        }
    }

    public float GetFireDelay() {
        return 5f;
    }

    private bool hasAmmo = true;
    void Update() {
        timer -= Time.deltaTime;

        if (timer <= 0) {
            timer = 10f;
            onBulletFiredEvent?.Invoke();
            var bullet  = Instantiate(buggyBulletPrefab, shootSpawnPos.position, shootSpawnPos.rotation);
            bullet.GetComponent<Rigidbody>().velocity = shootSpawnPos.forward * 8 + shootSpawnPos.right*LevelReferences.s.speed;
            bullet.GetComponent<Rigidbody>().detectCollisions = false;

            if (curAmmo.Count > 0) {
                var index = curAmmo.Count - 1;
                Destroy(curAmmo[index]);
                curAmmo.RemoveAt(index);
            } else {
                GetComponentInChildren<BuggyCatapultAnimator>().enabled = false;
                hasAmmo = false;
                this.enabled = false;
            }
        }
    }
    
    
    
    public Sprite GetSprite() {
        return gunSprite;
    }

    public string GetName() {
        return GetComponent<ClickableEntityInfo>().info;
    }

    public string GetDescription() {
        return GetComponent<ClickableEntityInfo>().tooltip.text;
    }
}
