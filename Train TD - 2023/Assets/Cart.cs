using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighlightPlus;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cart : MonoBehaviour, IPlayerHoldable {
    public bool isMainEngine = false;
    public bool isCargo = false;

    public List<SnapLocation> myArtifactLocations = new List<SnapLocation>();
    
    public bool isBeingDisabled = false;
    public int trainIndex;
    public float cartPosOffset;

    public float length = 1.4f;
    [HideInInspector]
    public bool canPlayerDrag = true;

    public string displayName = "Unnamed But Nice in game name";
    public string uniqueName = "unnamed";

    public Sprite Icon;

    [Space] 
    public bool isDestroyed = false;

    public Transform uiTargetTransform;
    public Transform shootingTargetTransform;
    public Transform modulesParent;

    public bool canWarp = false;

    [NonSerialized] public GameObject currentlyRepairingUIThing;

    private bool avoidByDefault;
    private void Start() {
        SetUpOverlays();
        SetUpOutlines();
        ResetState();
        avoidByDefault = GetComponent<PossibleTarget>().avoid;
        
        SetComponentCombatShopMode();
        PlayStateMaster.s.OnCombatEntered.AddListener(SetComponentCombatShopMode);
        PlayStateMaster.s.OnShopEntered.AddListener(SetComponentCombatShopMode);
    }


    private Material cartOverlayMaterial;

    public Transform genericParticlesParent;
    
    public void ResetState() {
        SetUpOverlays();
        SetUpOutlines();
        //genericParticlesParent.DeleteAllChildren();
        GetHealthModule().ResetState();

        var modulesWithResetStates = GetComponentsInChildren<IResetState>();
        for (int i = 0; i < modulesWithResetStates.Length; i++) {
            modulesWithResetStates[i].ResetState(); 
        }

        var modulesWithApplyStates = GetComponentsInChildren<IChangeCartState>();
        for (int i = 0; i < modulesWithApplyStates.Length; i++) {
            modulesWithApplyStates[i].ChangeState(this);
        }
    }

    public void SetDisabledState() {
        var isDisabled = isDestroyed || isBeingDisabled;
        
        Train.s.OnCartDestroyedOrRevived();

        if (isDisabled) {
            GetComponent<PossibleTarget>().avoid = isDestroyed;

            var engineModule = GetComponentInChildren<EngineModule>();
            if (engineModule) {
                //engineModule.OnEngineLowPower?.Invoke(true);
                engineModule.isDestroyed = true;
                GetComponentInChildren<EngineFireController>().StopEngineFire();
            }

            var gunModules = GetComponentsInChildren<GunModule>();
            for (int i = 0; i < gunModules.Length; i++) {
                gunModules[i].DeactivateGun();
            
                if(GetComponentInChildren<TargetPicker>()){
                    GetComponentInChildren<TargetPicker>().enabled = false;
                }
            }

            var directControlModule = GetComponentInChildren<IDirectControllable>();
            if (directControlModule != null) {
                if(DirectControlMaster.s.directControlInProgress && directControlModule == DirectControlMaster.s.currentDirectControllable)
                    DirectControlMaster.s.DisableDirectControl();
            }
        
            /*var attachedToTrain = GetComponentsInChildren<ActivateWhenAttachedToTrain>();

            for (int i = 0; i < attachedToTrain.Length; i++) {
                attachedToTrain[i].DetachedFromTrain();
            }*/
        
            var disabledState = GetComponentsInChildren<IDisabledState>();

            foreach (var disabledModule in disabledState) {
                disabledModule.CartDisabled();
            }

        } else {
            GetComponent<PossibleTarget>().avoid = avoidByDefault;
        
            var engineModule = GetComponentInChildren<EngineModule>();
            if (engineModule) {
                engineModule.enabled = true;
                //engineModule.OnEngineLowPower?.Invoke(false);
                engineModule.isDestroyed = false;
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
        
            /*var duringCombat = GetComponentsInChildren<IActiveDuringCombat>();

            if (PlayStateMaster.s.isCombatStarted()) {
                for (int i = 0; i < duringCombat.Length; i++) {
                    duringCombat[i].ActivateForCombat();
                }
            }*/

            var disabledState = GetComponentsInChildren<IDisabledState>();

            foreach (var disabledModule in disabledState) {
                disabledModule.CartEnabled();
            }
        }
        
        Train.s.TrainChanged();

    }

    private void Update() {
        var pos = transform.position;
        if (pos.y < -10) {
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

    public void SetHoldingState(bool state) {
        if (state) {
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
        } else {
            GetComponent<Rigidbody>().isKinematic = false;
            GetComponent<Rigidbody>().useGravity = true;
        }
    }

    public bool CanDrag() {
        return (!isMainEngine && canPlayerDrag) && (PlayStateMaster.s.isShopOrEndGame() || !IsAttachedToTrain());
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
        
        SetDisabledState();
    }

    
    private void OnDestroy() {
        // Destroy material instances
        if (_meshes != null && _meshes.Length > 0) {
            Destroy(cartOverlayMaterial);
        }

        if(IsAttachedToTrain())
            Train.s.CartDestroyed(this);
        
        if(currentlyRepairingUIThing != null)
            Destroy(currentlyRepairingUIThing);
        
        PlayStateMaster.s.OnCombatEntered.RemoveListener(SetComponentCombatShopMode);
        PlayStateMaster.s.OnShopEntered.RemoveListener(SetComponentCombatShopMode);
    }


    //[ReadOnly]
    private HighlightEffect[] _outlines;
    //[ReadOnly]
    [NonSerialized]
    public MeshRenderer[] _meshes;

    private static readonly int BoostAmount = Shader.PropertyToID("_Boost_Amount");

    void SetUpOutlines() {
        if (_outlines == null || _outlines.Length ==0) {
            _outlines = GetComponentsInChildren<HighlightEffect>(true);
        }
    }

    public void SetUpOverlays() {
        if (_meshes == null || _meshes.Length == 0) {
            cartOverlayMaterial = Instantiate(LevelReferences.s.cartOverlayMaterial);
            
            _meshes = GetComponentsInChildren<MeshRenderer>(true);

            for (int i = 0; i < _meshes.Length; i++) {
                var materials = _meshes[i].materials.ToList();
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
                rend.materials[1].SetFloat(BoostAmount, value);
            }
        }
    }
    
    public void SetHighlightState(bool isHighlighted) {
        foreach (var outline in _outlines) {
            if (outline != null) {
                outline.highlighted = isHighlighted;
            }
        }
    }
    
    public void SetHighlightState(bool isHighlighted, Color color) {
        foreach (var outline in _outlines) {
            if (outline != null) {
                outline.highlighted = isHighlighted;
                outline.outlineColor = color;
            }
        }
    }

    public ModuleHealth GetHealthModule() {
        return GetComponent<ModuleHealth>();
    }

    public float GetCurrentHealth() {
        return GetHealthModule().currentHealth;
    }
    
    public float GetCurrentHealthReduction() {
        return GetHealthModule().maxHealthReduction;
    }

    public void SetCurrentHealth(float health, float maxHealthReduction = -1) {
        GetHealthModule().SetHealth(health, maxHealthReduction);
    }
    
    public void FullyRepair() {
        GetHealthModule().FullyRepair();
    }

    public bool IsAttachedToTrain() {
        return GetComponentInParent<Train>() != null;
    }
    
    private DroneRepairController _holder;
    public DroneRepairController GetHoldingDrone() {
        return _holder;
    }

    public void SetHoldingDrone(DroneRepairController holder) {
        _holder = holder;
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

public interface IDisabledState {
    public void CartDisabled();
    public void CartEnabled();
}


public interface IResetState {
    public void ResetState();
}

public interface IChangeCartState {
    public void ChangeState(Cart target);
}

