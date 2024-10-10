using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LevelReferences : MonoBehaviour {
    public static LevelReferences s;

    public Camera mainCam {
        get {
            return MainCameraReference.s.cam;
        }
    }

    public Material cartOverlayMaterial;
    public GameObject burningEffect;
    public GameObject enemyDamageEffect;
    public GameObject cartRepairableDamageEffect;
    public GameObject repairDoneEffect;
    public GameObject maxHealthReductionPlatePrefab;
    public GameObject repairExplosionEffect;
    
    [Space]

    public GameObject waveDisplayPrefab;
    public GameObject enemyHealthPrefab;
    public GameObject cartHealthPrefab;
    public GameObject bulletHealthPrefab;
    public GameObject damageNumbersPrefab;
    public GameObject missPrefab;
    public GameObject criticalDamagePrefab;
    public Transform uiDisplayParent;

    [Space]
    
    public GameObject repairEffectPrefab;
    public GameObject shieldUpEffectPrefab;
    public GameObject goodItemSpawnEffectPrefab;
    public GameObject cartBeingDisabledEffect;
    
    [Space]
    
    public GameObject reloadEffect_regular;
    
    [Space]

    public GameObject buildingHPLowParticles;
    public GameObject buildingHPCriticalParticles;
    public GameObject buildingDestroyedParticles;
    
    [Space]

    public GameObject teleportFromEffect;
    public GameObject teleportToEffect;
    
    
    public GameObject teleportingCartStartEffect;
    public GameObject teleportingCartEndEffect;
    
    [Space]

    public GameObject currentlySlowedEffect;
    
    [Space]

    public float speed = 1f;

    public List<PossibleTarget> allTargets = new List<PossibleTarget>();
    public TargetValues[] allTargetValues = new TargetValues[0];
    public int targetValuesCount = 0;
    public bool targetsDirty;

    
    public List<SnapLocation> allSnapLocations = new List<SnapLocation>();
    public struct TargetValues {
        public PossibleTarget.Type type;
        public Vector3 position;
        public bool avoid;
        public bool flying;
        public bool active;
        public int health;
        public float healthPercent;
        
        public TargetValues(PossibleTarget target) {
            type = target.myType;
            position = target.targetTransform.position;
            avoid = target.avoid;
            flying = target.flying;
            active = target.enabled;
            health = (int)target.GetHealth();
            healthPercent = target.GetHealthPercent();
        }

        public void Set(PossibleTarget target) {
            type = target.myType;
            position = target.targetTransform.position;
            avoid = target.avoid;
            flying = target.flying;
            active = target.enabled;
            health = (int)target.GetHealth();
            healthPercent = target.GetHealthPercent();
        }
    }

    [Space]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public LayerMask buildingLayer;
    public LayerMask cartRepairableSectionLayer;
    public LayerMask cartSnapLocationsLayer;
    public LayerMask gateMask;
    public LayerMask artifactLayer;
    public LayerMask meepleLayer;
    public LayerMask scrapsItemLayer;
    public LayerMask genericClickableLayer;
    public LayerMask allSelectablesLayer;

    [Space]
    public SingleUnityLayer playerBulletLayer;
    public SingleUnityLayer enemyBulletLayer;
    
    [Space]
    public Color leftColor = Color.white;
    public Color rightColor = Color.white;

    [Space]
    public Sprite encounterIcon;
    public Color encounterColor = Color.cyan;
    public Color eliteColor = Color.red;

    [Space]
    public GameObject emptyCart;
    public GameObject scrapCart;

    [Space]
    public GameObject noAmmoWarning;

    [Space] 
    public GameObject ammo_player;

    public GameObject ammo_enemy;
    public Material ammoLightUnactiveMat;
    public Material ammoLightActiveMat;

    [Space] public float smallEffectFirstActivateTimeAfterCombatStarts = 10f;
    public float bigEffectFirstActivateTimeAfterCombatStarts = 15;
    public GameObject radiationDamagePrefab;
    public GameObject growthEffectPrefab;
    public GameObject cartMergeEffect;

    public GameObject mergeGemItem;

    [Space] 
    public GameObject coinDrop;

    public AnimationCurve selectMarkerPulseCurve;
    public AnimationCurve alphaPulseCurve;
    [Space] 
    public GameObject enemyWaveMovingArrow;
    public Material enemyWaveMovingArrowMaterial;

    
    [Space] 
    [ShowInInspector]
    public List<IPlayerHoldable> combatHoldableThings = new List<IPlayerHoldable>();


    public void ClearCombatHoldableThings() {
        combatHoldableThings.Clear();
    }

    private void Awake() {
        s = this;
        allTargetValues = new TargetValues[64];
        for (int i = 0; i < allTargetValues.Length; i++) {
            allTargetValues[i] = new TargetValues();
        }
    }

    private void Update() {
        targetValuesCount = allTargets.Count;
        if (targetValuesCount > allTargetValues.Length) {
            allTargetValues = new TargetValues[Mathf.Max(64, allTargetValues.Length*2)];
            for (int i = 0; i < allTargetValues.Length; i++) {
                allTargetValues[i] = new TargetValues();
            }
            Debug.Log($"target values array expanded to {allTargetValues.Length}");
        }

        for (int i = 0; i < targetValuesCount; i++) {
            allTargetValues[i].Set(allTargets[i]);
            allTargets[i].myId = i;
        }

        targetsDirty = false;
    }
}
