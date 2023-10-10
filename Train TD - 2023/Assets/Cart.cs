using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cart : MonoBehaviour {

    public bool needsToBeBought = false;
    [ShowIf("needsToBeBought")]
    public int buyCost = 50;

    public int level;

    public UpgradesController.CartRarity myRarity;
    [Tooltip("If empty will drop in every act. If numbers are given will only drop in those acts")]
    public List<int> possibleDropActs = new List<int>();

    public bool isMainEngine = false;
    public bool isMysteriousCart = false;
    public bool isCargo = false;

    public Artifact myAttachedArtifact;

    public Transform artifactParent;
    public bool canAcceptComponentArtifact = false;
    public Transform artifactChunkTransform;
    
    public bool isBeingDisabled = false;
    public int trainIndex;

    public int cartSize = 1;

    public bool isRepairable => !isMainEngine && !isCargo && !isMysteriousCart;
    public bool loseGameIfYouLoseThis => isMainEngine || isMysteriousCart;
    //public bool isRepairable => !isCargo;

    public UpgradesController.CartLocation myLocation = UpgradesController.CartLocation.train;

    public float length = 1.4f;
    [HideInInspector]
    public bool canPlayerDrag = true;

    public string displayName = "Unnamed But Nice in game name";
    public string uniqueName = "unnamed";

    public Sprite Icon;

    public AudioClip[] moduleBuiltSound;

    [Space] 
    public bool isDestroyed = false;

    public Transform uiTargetTransform;
    public Transform shootingTargetTransform;
    public Transform modulesParent;

    [NonSerialized] public GameObject currentlyRepairingUIThing;
    private void Start() {
        SetComponentCombatShopMode();
        PlayStateMaster.s.OnCombatEntered.AddListener(SetComponentCombatShopMode);
        PlayStateMaster.s.OnShopEntered.AddListener(SetComponentCombatShopMode);
        SetUpOverlays();
        SetUpOutlines();
        ResetState();
    }


    public Material cartOverlayMaterial;

    public MeshRenderer cartMaterial;

    public Transform genericParticlesParent;
    
    public void ResetState() {
        SetUpOverlays();
        SetUpOutlines();
        genericParticlesParent.DeleteAllChildren();
        GetHealthModule().ResetState(level);
        
        artifactChunkTransform.DeleteAllChildren();

        cartMaterial.material = LevelReferences.s.cartLevelMats[level];
        
        if (myAttachedArtifact != null) {
            myAttachedArtifact.ResetState();
        }
        
        var modulesWithResetStates = GetComponentsInChildren<IResetState>();
        for (int i = 0; i < modulesWithResetStates.Length; i++) {
            modulesWithResetStates[i].ResetState(level); // level goes 0, 1, 2
        }
    }

    public void SetDisabledState() {
        var isDisabled = isDestroyed || isBeingDisabled;

        if (isDisabled) {
            GetComponent<PossibleTarget>().enabled = false;

            var engineModule = GetComponentInChildren<EngineModule>();
            if (engineModule) {
                engineModule.enabled = false;
                GetComponentInChildren<EngineFireController>().StopEngineFire();
            }

            var gunModules = GetComponentsInChildren<GunModule>();
            for (int i = 0; i < gunModules.Length; i++) {
                gunModules[i].DeactivateGun();
            
                if (gunModules[i].beingDirectControlled) {
                    DirectControlMaster.s.DisableDirectControl();
                } 
            
                if(GetComponentInChildren<TargetPicker>()){
                    GetComponentInChildren<TargetPicker>().enabled = false;
                }
            }
        
            /*var attachedToTrain = GetComponentsInChildren<ActivateWhenAttachedToTrain>();

            for (int i = 0; i < attachedToTrain.Length; i++) {
                attachedToTrain[i].DetachedFromTrain();
            }*/
        
            var duringCombat = GetComponentsInChildren<IActiveDuringCombat>();
        
            for (int i = 0; i < duringCombat.Length; i++) { duringCombat[i].Disable(); }
        } else {
            GetComponent<PossibleTarget>().enabled = true;
        
            var engineModule = GetComponentInChildren<EngineModule>();
            if (engineModule) {
                engineModule.enabled = true;
                GetComponentInChildren<EngineFireController>().ActivateEngineFire();
            }

            var gunModules = GetComponentsInChildren<GunModule>();
            for (int i = 0; i < gunModules.Length; i++) {
                gunModules[i].ActivateGun();
            
                if(GetComponentInChildren<TargetPicker>()){
                    GetComponentInChildren<TargetPicker>().enabled = true;
                }
            }
        
            /*var attachedToTrain = GetComponentsInChildren<ActivateWhenAttachedToTrain>();

            for (int i = 0; i < attachedToTrain.Length; i++) {
                attachedToTrain[i].AttachedToTrain();
            }*/
        
            var duringCombat = GetComponentsInChildren<IActiveDuringCombat>();

            if (PlayStateMaster.s.isCombatStarted()) {
                for (int i = 0; i < duringCombat.Length; i++) {
                    duringCombat[i].ActivateForCombat();
                }
            }
        }
        
        Train.s.ArtifactsChanged();

    }

    private void Update() {
        var pos = transform.position;
        if (pos.y < -1) {
            pos.y = 1;
            transform.position = pos;
        }
    }

    public Transform GetShootingTargetTransform() {
        return shootingTargetTransform;
    }

    public Transform GetUITargetTransform() {
        return uiTargetTransform;
    }

    public void SetComponentCombatShopMode() {
        var duringCombat = GetComponentsInChildren<IActiveDuringCombat>();
        var duringShopping = GetComponentsInChildren<IActiveDuringShopping>();
        
        for (int i = 0; i < duringCombat.Length; i++) { duringCombat[i].Disable(); }
        for (int i = 0; i < duringShopping.Length; i++) { duringShopping[i].Disable(); }

        if (PlayStateMaster.s.isCombatStarted()) {
            for (int i = 0; i < duringCombat.Length; i++) {
                duringCombat[i].ActivateForCombat();
            }
        } else if (PlayStateMaster.s.isShop()) {
            for (int i = 0; i < duringShopping.Length; i++) {
                duringShopping[i].ActivateForShopping();
            }
        }
    }

    
    private void OnDestroy() {
        // Destroy material instances
        if (_meshes != null && _meshes.Length > 0) {
            Destroy(cartOverlayMaterial);
        }

        if(GetComponentInParent<Train>() != null)
            Train.s.CartDestroyed(this);
        
        if(currentlyRepairingUIThing != null)
            Destroy(currentlyRepairingUIThing);
        
        PlayStateMaster.s.OnCombatEntered.RemoveListener(SetComponentCombatShopMode);
        PlayStateMaster.s.OnShopEntered.RemoveListener(SetComponentCombatShopMode);
    }


    [ReadOnly]
    public Outline[] _outlines;
    [ReadOnly]
    public MeshRenderer[] _meshes;

    private static readonly int BoostAmount = Shader.PropertyToID("_Boost_Amount");

    void SetUpOutlines() {
        if (_outlines == null || _outlines.Length ==0) {
            _outlines = GetComponentsInChildren<Outline>(true);
        }
    }

    public void SetUpOverlays() {
        if (_meshes == null || _meshes.Length == 0) {
            cartOverlayMaterial = Instantiate(cartOverlayMaterial);
            
            _meshes = GetComponentsInChildren<MeshRenderer>(true);

            for (int i = 0; i < _meshes.Length; i++) {
                var materials = _meshes[i].sharedMaterials.ToList();
                materials.Add(cartOverlayMaterial);
                _meshes[i].materials = materials.ToArray();
            }

            GetHealthModule().myCart = this;
        }
    }
    
    public void SetBuildingBoostState (float value) {
        var _renderers = _meshes;
        for (int j = 0; j < _renderers.Length; j++) {
            var rend = _renderers[j];
            if (rend != null) {
                rend.sharedMaterials[1].SetFloat(BoostAmount, value);
            }
        }
    }
    
    public void SetHighlightState(bool isHighlighted) {
        foreach (var outline in _outlines) {
            if (outline != null) {
                outline.enabled = isHighlighted;
            }
        }
    }

    public ModuleHealth GetHealthModule() {
        return GetComponent<ModuleHealth>();
    }

    public int GetCurrentHealth() {
        return (int)GetComponent<ModuleHealth>().currentHealth;
    }

    public void SetCurrentHealth(float health) {
        GetComponent<ModuleHealth>().SetHealth(health);
    }

    public void SetAttachedToTrainModulesMode(bool isAttached) {
        var attachedToTrain = GetComponentsInChildren<ActivateWhenAttachedToTrain>();
        for (int i = 0; i < attachedToTrain.Length; i++) {
            if (isAttached && !isDestroyed) {
                attachedToTrain[i].AttachedToTrain();
            } else {
                attachedToTrain[i].DetachedFromTrain();
            }
        }
    }
}


public interface IActiveDuringCombat {
    public void ActivateForCombat();
    public void Disable();
}
public interface IActiveDuringShopping {
    public void ActivateForShopping();
    public void Disable();
}


public interface IResetState {
    public void ResetState(int level);
}
