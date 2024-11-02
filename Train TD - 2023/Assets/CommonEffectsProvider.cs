using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using static VisualEffectsController;

public class CommonEffectsProvider : MonoBehaviour
{
    public static CommonEffectsProvider s;

    private void Awake() {
        s = this;
    }
    
    public GameObject poolTemplate;
    void Start()
    {
        for (int i = 0; i < effects.Length; i++) {
            var curEffect = effects[i];
            var effectPool = Instantiate(poolTemplate, transform).GetComponent<ObjectPool>();

            effectPool.RePopulateWithNewObject(curEffect.myObj);
            curEffect.myPool = effectPool;
            effectPool.gameObject.name = curEffect.type + " pool";
        }
    }
    
    private void LateUpdate() {
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(0);
    }


    public EffectHolder[] effects;


    [Serializable]
    public class EffectHolder {
        [HorizontalGroup("row1")]
        public CommonEffectType type;
        [HorizontalGroup("row1")]
        public GameObject myObj;
        [HideInInspector]
        public ObjectPool myPool;
    }

    public enum CommonEffectType {
        dirtHit = 0, trainHit=1, enemyHit = 2, cantPenetrateArmor=3,rocketExplosion=4,lazerHit=5,mortarExplosion=6,mortarMiniHit=7,
        smallDamage=8, mediumDamage=9,bigDamage=10,megaDamage=11, nothing = 12, dirtHitClick =13, trainHitClick=14, enemyHitClick=15, 
        gatlingTooHot = 16, gatlingChilled=17, fireSlowDecay=18, fireFastDecay=19, radGunSelfDamageMuzzleFlash=20
    }

    public void SpawnEffect(CommonEffectType type, Vector3 position, Quaternion rotation, Transform parent, EffectPriority priority = EffectPriority.Always) {
        if (type == CommonEffectType.nothing) {
            return;
        }
        var pool = GetEffect(type);
        if (pool != null) {
            var autoExpand = pool.autoExpand;
            if (priority == EffectPriority.Medium) {
                pool.autoExpand = false;
            }
            var obj = pool.Spawn(position, rotation);
            if (priority == EffectPriority.Medium) {
                pool.autoExpand = autoExpand;
            }
            obj.transform.SetParent(parent);
        }
    }
    
    public void SpawnEffect(CommonEffectType type, Vector3 position, Quaternion rotation, EffectPriority priority = EffectPriority.Always) {
        if (type == CommonEffectType.nothing) {
            return;
        }
        var pool = GetEffect(type);
        if (pool != null) {
            pool.Spawn(position, rotation);
        }
    }

    ObjectPool GetEffect(CommonEffectType type) {
        for (int i = 0; i < effects.Length; i++) {
            if (effects[i].type == type) {
                return effects[i].myPool;
            }
        }

        return null;
    }
}
