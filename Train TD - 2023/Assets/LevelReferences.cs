using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LevelReferences : MonoBehaviour {

    public static LevelReferences s;

    public Transform playerTransform {
        get {
            return transform;
        }
    }

    public Camera mainCam {
        get {
            return MainCameraReference.s.cam;
        }
    }

    public GameObject burningEffect;
    
    [Space]

    public GameObject metalBulletHitEffectPrefab;
    public GameObject dirtBulletHitEffectPrefab;
    public GameObject enemyRegularHitEffectPrefab;
    public GameObject enemyCantPenetrateHitEffectPrefab;
    public GameObject rocketExplosionEffectPrefab;
    public GameObject laserHitPrefab;
    public GameObject mortarExplosionEffectPrefab;
    public GameObject mortarMiniHitPrefab;
    public GameObject waveDisplayPrefab;
    public GameObject enemyHealthPrefab;
    public GameObject bulletHealthPrefab;
    public GameObject cartHealthPrefab;
    public Transform cartHealthParent;
    public GameObject damageNumbersPrefab;
    public Transform uiDisplayParent;

    public GameObject luckyNegate;
    
    [Space]
    
    public GameObject repairEffectPrefab;
    public GameObject shieldUpEffectPrefab;
    
    [Space]
    
    public GameObject reloadEffect_regular;
    
    [Space]

    public GameObject smallDamagePrefab;
    public GameObject mediumDamagePrefab;
    public GameObject bigDamagePrefab;
    
    [Space]

    public GameObject buildingHPLowParticles;
    public GameObject buildingHPCriticalParticles;
    public GameObject buildingDestroyedParticles;
    
    [Space]

    public GameObject teleportStartEffect;
    public float teleportTime = 1f;
    public GameObject teleportCompleteEffect;
    
    [Space]

    public GameObject currentlySlowedEffect;
    
    [Space]

    public float speed = 1f;

    public static List<PossibleTarget> allTargets = new List<PossibleTarget>();
    public static TargetValues[] allTargetValues = new TargetValues[0];
    public static bool targetsDirty;


    [Space]
    public Material[] cartLevelMats;
    public Material[] artifactLevelMats;

    
    public struct TargetValues {
        public PossibleTarget.Type type;
        //public float health;
        public Vector3 position;
        public bool avoid;
        public bool flying;
        public TargetValues(PossibleTarget target) {
            type = target.myType;
            //health = target.GetHealth();
            position = target.targetTransform.position;
            avoid = target.avoid;
            flying = target.flying;
        }
    }

    [Space]
    public Train train;


    [Space]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public LayerMask buildingLayer;
    public LayerMask cartSnapLocationsLayer;
    public LayerMask gateMask;
    public LayerMask artifactLayer;
    public LayerMask meepleLayer;

    [Space]
    public SingleUnityLayer playerBulletLayer;
    public SingleUnityLayer enemyBulletLayer;
    
    [Space]
    public Color leftColor = Color.white;
    public Color rightColor = Color.white;

    [Space]
    public Sprite encounterIcon;
    public Color encounterColor = Color.cyan;
    public Sprite smallEnemyIcon;
    public Sprite eliteEnemyIcon;
    public Color eliteColor = Color.red;
    public Sprite bossEnemyIcon;

    [Space]
    public GameObject enemyCartReward;
    public GameObject enemyHasArtifactStar;

    [Space]
    public GameObject emptyCart;
    public GameObject scrapCart;

    [Space]
    public GameObject noAmmoWarning;

    [Space]
    public LevelSegmentScriptable debugBuggyLevel;

    [Space] 
    public GameObject bullet_regular;
    public GameObject bullet_repair;

    [Space] public float smallEffectFirstActivateTimeAfterCombatStarts = 10f;
    public float bigEffectFirstActivateTimeAfterCombatStarts = 15;
    public GameObject radiationDamagePrefab;
    public GameObject growthEffectPrefab;

    [Space] public GameObject coinDrop;
    /*public GameObject GetResourceParticle(ResourceTypes types) {
        switch (types) {
            case ResourceTypes.scraps:
                return resourceParticleScraps;
            default:
                return null;
        }
    }
    
    public GameObject GetResourceLostParticle(ResourceTypes types) {
        switch (types) {
            case ResourceTypes.scraps:
                return resourceLostParticleScraps;
            default:
                return null;
        }
    }*/
    
    
    public AnimationCurve selectMarkerPulseCurve;
    public AnimationCurve alphaPulseCurve;
    
    private void Awake() {
        s = this;
        
    }

    const int maxMoneyPileCount = 5;

    public void SpawnResourceAtLocation(ResourceTypes type, float amount, Vector3 location) {
        return;
        //SpawnResourceAtLocation(type, Mathf.RoundToInt(amount), location);
    }
    /*public void SpawnResourceAtLocation(ResourceTypes type, int amount, Vector3 location, bool customCollectTarget = false, Transform customCollectTargetTransform = null) {
        if (amount == 0)
            return;
        
        int count = Mathf.CeilToInt(amount / (float)maxMoneyPileCount);

        GameObject prefab = null;

        switch (type) {
            case ResourceTypes.scraps:
                //prefab = scrapPile;
                break;
        }

        if (prefab == null) {
            return;
        }

        while(amount > 0) {
            //var random = Random.insideUnitCircle * (count / 4f);
            var pile = Instantiate(prefab, location /*+ new Vector3(random.x, 0, random.y)#1#, Quaternion.identity);
            var pileComp = pile.GetComponent<ScrapPile>();
            var targetAmount = maxMoneyPileCount;
            if (amount < maxMoneyPileCount)
                targetAmount = amount;
            pileComp.SetUp(targetAmount, type);
            amount -= maxMoneyPileCount;

            StartCoroutine(ThrowPile(pile, customCollectTarget, customCollectTargetTransform));
        }
    }
    */
    
    
    /*[Space]
    public float throwHorizontalSpeed = 1f;
    public float throwVerticalSpeed = 3f;
    public float throwGravity = 9.81f;

    IEnumerator ThrowPile(GameObject pile, bool customCollectTarget = false, Transform customCollectTargetTransform = null) {
        var random = Random.insideUnitCircle.normalized;
        var direction = new Vector3(random.x, 0, random.y);
        if (customCollectTarget) {
            direction =  customCollectTargetTransform.position - pile.transform.position;
            direction.y = 0;
        }
        
        direction.Normalize();
        var verticalSpeed = throwVerticalSpeed;

        while (verticalSpeed > -throwVerticalSpeed) {
            pile.transform.position += (direction * throwHorizontalSpeed + Vector3.up * verticalSpeed) * Time.deltaTime;


            verticalSpeed -= throwGravity * Time.deltaTime;
            yield return null;
        }

        if (!customCollectTarget) {
            pile.GetComponent<ScrapPile>().CollectPile();
        } else {
            pile.GetComponent<ScrapPile>().CollectPileWithTarget(customCollectTargetTransform);
        }
    }*/

    private void Update() {
        if (allTargetValues.Length != allTargets.Count || targetsDirty) {
            allTargetValues = new TargetValues[allTargets.Count];
        }

        for (int i = 0; i < allTargetValues.Length; i++) {
            allTargetValues[i] = new TargetValues(allTargets[i]);
            allTargets[i].myId = i;
        }

        targetsDirty = false;
    }
}
