using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DroneRepairController : MonoBehaviour, IResetState, IDisabledState, IActiveDuringCombat {

    public Transform droneDockedPosition;
    public int dockOffset;
    public GameObject drone;
    public RepairDrone droneScript;
    public bool beingDirectControlled = false;

    public bool droneCannotActivateOverride = false;
    public bool droneActive = true;
    
    private void Start() {
        droneScript = drone.GetComponent<RepairDrone>();
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.AddListener(OnTrainChanged);
        myHealth = GetComponentInParent<ModuleHealth>();
    }

    private void OnDestroy() {
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.RemoveListener(OnTrainChanged);
    }

    public void ActivateAutoDrone() {
        if(droneCannotActivateOverride)
            return;

        droneActive = true;
        gettingNewTarget = false;
        TryGetNewTarget();
    }

    public void DisableAutoDrone() {
        if (target != null) {
            target.isTaken = false;
            target = null;
        }

        droneActive = false;
        gettingNewTarget = false;
        CancelInvoke();
    }

    public RepairableBurnEffect target;
    public bool gettingNewTarget = false;
    public ModuleHealth health;

    public ModuleHealth myHealth;

    private float repairTimer;
    private Vector3 velocity = Vector3.zero;
    private Quaternion quatVel = Quaternion.identity;

    public float selfRepairSpeed = 1;
    public Image selfRepairImage;
    private float curSelfRepair = 0;
    
    [Tooltip("at 1 multiplier repairing takes 5 secs")]
    public float repairSpeedMultiplier = 1f;

    public Affectors currentAffectors;
    [Serializable]
    public class Affectors {
        public float power = 1;
        public float speed = 1;
        public float efficiency = 1;

        public float uranium = 0f;
        public int fire = 0;
        public int iron = 0;
        public float explosionRange = 0;
        
        public bool vampiric = false;
        public bool ancientDisabled = false;
        public bool lizardOverride = false;
        public bool IsActive() {
            return lizardOverride || !ancientDisabled;
        }
    }

    private bool canPickNewTargets = true;

    public float chargeTime = 30f;
    public float repairChargeFullUseTime = 60f;
    
    private void LateUpdate() {
        if (MissionLoseFinisher.s.isMissionLost) {
            return;
        }
        
        if (myHealth.myCart.isDestroyed) {
            curSelfRepair += selfRepairSpeed* Time.deltaTime;
            curSelfRepair = Mathf.Clamp01(curSelfRepair);
            selfRepairImage.fillAmount = curSelfRepair / 1f;
            if (curSelfRepair >= 1) {
                curSelfRepair = 0;
                myHealth.FullyRepair();
            }
        }
        
        if (!beingDirectControlled) {
            if (target == null || target.gameObject == null) {
                // docked?
                droneScript.SetCurrentlyRepairingState(false);
                if (!gettingNewTarget) {
                    MoveDroneWithVelocity(droneDockedPosition.transform.position + Vector3.up*dockOffset, droneDockedPosition.transform.rotation);
                }

                if (Vector3.Distance(drone.transform.position, droneDockedPosition.transform.position) < 0.05f) {
                    droneScript.currentChargePercent += (1f / chargeTime) * Time.deltaTime;
                    droneScript.currentChargePercent = Mathf.Clamp01(droneScript.currentChargePercent);

                    if (droneScript.currentChargePercent >= 1f) {
                        droneScript.needToFullyCharge = false;
                        TryGetNewTarget();
                    }
                }
            } else {
                var targetPos = target.transform.position;
                var forward = PathAndTerrainGenerator.s.GetDirectionVectorOnActivePath(health.myCart.cartPosOffset);
                var right = Quaternion.Euler(0, 90, 0) * forward;
                var center = PathAndTerrainGenerator.s.GetPointOnActivePath(health.myCart.cartPosOffset);
                var toTheRight = IsToTheRight(targetPos, center, forward);

                if (toTheRight) {
                    targetPos += right * 0.2f;
                } else {
                    targetPos -= right * 0.2f;
                }

                targetPos += Vector3.up * 0.2f;

                var targetRot = Quaternion.LookRotation(target.transform.position - drone.transform.position);

                MoveDroneWithVelocity(targetPos, targetRot);

                if (Vector3.Distance(drone.transform.position, targetPos) < 0.15f) {
                    droneScript.SetCurrentlyRepairingState(true);

                    droneScript.currentChargePercent -= (1f / repairChargeFullUseTime) * Time.deltaTime;
                    
                    TryDoRepair(target, out bool removedArrow, out bool repairSuccess, Time.deltaTime);
                } else {
                    repairTimer -= Time.deltaTime;
                    repairTimer = Mathf.Clamp(repairTimer, 0, 1);
                    droneScript.SetCurrentlyRepairingState(false);
                }
            }
        }
        
    }


    public float TryDoRepair(RepairableBurnEffect target, out bool removedArrow, out bool repairSuccess,float deltaTime, float extraMultiplier = 1) {
        removedArrow = false;
        repairSuccess = false;
        if (target.hasArrow) {
            repairTimer = target.removeArrowState;
            repairTimer += deltaTime * GetArrowRepairTimeMultiplier()*extraMultiplier;
            target.SetRemoveArrowState(repairTimer);
            if (repairTimer > 1) {
                target.RemoveArrow();
                removedArrow = true;
                repairTimer = 0;
            }
        } else {
            repairTimer += deltaTime*GetRepairTimeMultiplier()*extraMultiplier;
        }

        if (repairTimer >= 1) {
            DoRepair(target.GetComponentInParent<ModuleHealth>(), target);
            
            TryGetNewTarget();

            repairSuccess = true;

            repairTimer = 0;
        }

        repairTimer = Mathf.Clamp01(repairTimer);

        return repairTimer;
    }


    public float GetArrowRepairTimeMultiplier() {
        var multiplier = 0.2f * GetEfficiency();
        multiplier /= TweakablesMaster.s.GetPlayerRepairTimeMultiplier();
        return Mathf.Min(20,multiplier); // at 1 efficiency repair arrow takes 5 sec, at 2 it takes 2.5 and so on
    }
    
    public float GetRepairTimeMultiplier() {
        var multiplier = 0.2f * GetEfficiency() * repairSpeedMultiplier;
        multiplier /= TweakablesMaster.s.GetPlayerRepairTimeMultiplier();
        return Mathf.Min(20,multiplier); // at 1 efficiency regular repair takes 5 sec, at 2 it takes 2.5 and so on
    }

    public float vampiricHealthStorage;
    public void DoRepair(ModuleHealth targetHealth, RepairableBurnEffect chunk) {
        targetHealth.RepairChunk(chunk);

        if (currentAffectors.explosionRange > 0) {
            //print("repair explosion");
            var otherRepairChunksInRange = Physics.OverlapSphere(chunk.transform.position, currentAffectors.explosionRange/2f, LevelReferences.s.cartRepairableSectionLayer);
            /*Debug.DrawLine(chunk.transform.position, chunk.transform.position+Vector3.up*currentAffectors.explosionRange, Color.green,5);
            Debug.DrawLine(chunk.transform.position, chunk.transform.position+Vector3.left*currentAffectors.explosionRange, Color.green,5);
            Debug.DrawLine(chunk.transform.position, chunk.transform.position+Vector3.right*currentAffectors.explosionRange, Color.green,5);
            Debug.DrawLine(chunk.transform.position, chunk.transform.position+Vector3.forward*currentAffectors.explosionRange, Color.green,5);
            Debug.DrawLine(chunk.transform.position, chunk.transform.position+Vector3.back*currentAffectors.explosionRange, Color.green,5);
            Debug.DrawLine(chunk.transform.position, chunk.transform.position+Vector3.down*currentAffectors.explosionRange, Color.green,5);
            Debug.Break();*/

            VisualEffectsController.s.SmartInstantiate(LevelReferences.s.repairExplosionEffect, chunk.transform.position, chunk.transform.rotation, Vector3.one * currentAffectors.explosionRange, 
                targetHealth.myCart.genericParticlesParent, VisualEffectsController.EffectPriority.Always);

            for (int i = 0; i < otherRepairChunksInRange.Length; i++) {
                var repairable = otherRepairChunksInRange[i].gameObject.GetComponentInParent<RepairableBurnEffect>();
                if (repairable != null) {
                    repairable.GetComponentInParent<ModuleHealth>().RepairChunk(repairable);
                }
            }
        }
        

        if (currentAffectors.uranium > 0) {
            UseAmmo();
        }

        if (currentAffectors.vampiric) {
            vampiricHealthStorage += 15;
            if (vampiricHealthStorage > ModuleHealth.repairChunkSize) {
                myHealth.RepairChunk();
                vampiricHealthStorage -= ModuleHealth.repairChunkSize;
            }
        }

        if (currentAffectors.iron > 0) {
            targetHealth.FullyRepair();
            for (int i = 1; i < currentAffectors.iron; i++) {
                Train.s.GetNextBuilding(i, targetHealth.myCart)?.FullyRepair();
                Train.s.GetNextBuilding(-i, targetHealth.myCart)?.FullyRepair();
            }
        } else {
            if (currentAffectors.power > 1) {
                var powerCount = 0;
                var powerTokens = currentAffectors.power - 1;
                while (powerTokens >= 1) {
                    targetHealth.RepairChunk();
                
                    powerCount += 1;
                    powerTokens -= 1;
                }

                if (Random.value < powerTokens) {
                    targetHealth.RepairChunk();
                
                    powerCount += 1;
                }
            }
        }
    }

    private AmmoTracker _ammoTracker;
    void UseAmmo() {
        if (_ammoTracker != null) {
            for (int i = 0; i < _ammoTracker.ammoProviders.Count; i++) {
                if (_ammoTracker.ammoProviders[i].AvailableAmmo() >= AmmoUse()) {
                    _ammoTracker.ammoProviders[i].UseAmmo(AmmoUse());
                }
            }
        }
    }


    float AmmoUse() {
        return 1f;
    }
    
    bool HasAmmo() {
        if (currentAffectors.uranium <= 0)
            return true;

        if (_ammoTracker == null) {
            _ammoTracker = GetComponentInParent<AmmoTracker>();
        }

        if (_ammoTracker == null) {
            return true;
        }

        var ammoUse = AmmoUse();
        for (int i = 0; i < _ammoTracker.ammoProviders.Count; i++) {
            if (_ammoTracker.ammoProviders[i].AvailableAmmo() >= ammoUse) {
                return true;
            }
        }

        return false;
    }

    public void UpdateDroneSize() {
        foreach (var drone in GetComponentsInChildren<RepairDrone>()) {
            drone.transform.localScale = Vector3.one * (1+currentAffectors.iron/2f);
        }
    }

    public float GetDroneMaxSpeed() {
        return maxVelocity * GetSpeed();
    }

    public float GetEfficiency() {
        if (currentAffectors.uranium > 0 && HasAmmo()) {
            return currentAffectors.efficiency+5;
        }
        return currentAffectors.efficiency;
    }

    public float GetSpeed() {
        if (currentAffectors.uranium > 0 && HasAmmo()) {
            return currentAffectors.speed+5;
        }
        return currentAffectors.speed;
    }


    public float maxVelocity = 10;
    public float curVelocity;
    void MoveDroneWithVelocity(Vector3 pos, Quaternion rot) {
        var acceleration = 0.05f * GetSpeed();
        var _maxVelocity = GetDroneMaxSpeed();

        acceleration = Mathf.Clamp(acceleration, 3, _maxVelocity);

        curVelocity += acceleration * Time.deltaTime;

        var distance = Vector3.Distance( drone.transform.position,pos);
        curVelocity = Mathf.Clamp(curVelocity, 0.2f, Mathf.Min(_maxVelocity, distance*5));
        
        drone.transform.position = Vector3.MoveTowards(drone.transform.position, pos, curVelocity*Time.deltaTime);
        drone.transform.rotation = ExtensionMethods.QuaterionSmoothDamp(drone.transform.rotation, rot, ref quatVel, 0.1f);
    }

    void OnTrainChanged() {
        if (target == null) {
            TryGetNewTarget();
        }
    }

    void TryGetNewTarget() {
        if (droneScript.currentChargePercent <= 0.05f) {
            droneScript.needToFullyCharge = true;
        }
        
        if (!droneActive || !canPickNewTargets || !currentAffectors.IsActive() || droneScript.needToFullyCharge) {
            UnsetTarget();
            gettingNewTarget = false;
            return;
        }

        if (!gettingNewTarget) {
            gettingNewTarget = true;
            UnsetTarget();
            Invoke(nameof(_GetNewTarget), 0.5f);
        }
    }

    void UnsetTarget() {
        if (target != null) {
            target.isTaken = false;
            target = null;
        }
    }

    void _GetNewTarget() {
        target = GetClosestBurn();

        if (target != null) {
            target.isTaken = true;
            health = target.GetComponentInParent<ModuleHealth>();

            //smoothTime = Mathf.Clamp(Vector3.Distance(drone.transform.position, target.transform.position), 0.2f,3f);
        }

        gettingNewTarget = false;
    }

    public RepairableBurnEffect GetClosestBurn() {
        List<RepairableBurnEffect> allRepairs = new List<RepairableBurnEffect>();

        for (int i = 0; i < Train.s.carts.Count; i++) {
            allRepairs.AddRange(Train.s.carts[i].GetHealthModule().activeBurnEffects);
        }

        RepairableBurnEffect newTarget = null;
        var closest = float.MaxValue;

        for (int i = 0; i < allRepairs.Count; i++) {
            var curRepair = allRepairs[i];

            if (curRepair.canRepair && !curRepair.isTaken) {
                var distance = Vector3.Distance(curRepair.transform.position, drone.transform.position);
                if (distance < closest) {
                    closest = distance;
                    newTarget = curRepair;
                }
            }
        }

        return newTarget;
    }

    // Function to check if a world position is to the right side of a center position
    public bool IsToTheRight(Vector3 worldPosition, Vector3 centerPosition, Vector3 direction)
    {
        // Vector from center position to the world position
        Vector3 centerToPoint = worldPosition - centerPosition;

        // Calculate cross product of direction vector and center-to-point vector
        Vector3 crossProduct = Vector3.Cross(direction, centerToPoint);

        // Check the sign of the y-component of the resulting vector
        // Assuming y-axis is the "up" direction
        return crossProduct.y > 0f;
    }

    public void ResetState() {
        currentAffectors = new Affectors();
        UpdateDroneSize();
    }

    public void CartDisabled() {
        canPickNewTargets = false;
        if (myHealth.myCart.isDestroyed) {
            drone.SetActive(false);
        }
        droneScript.SetCurrentlyRepairingState(false);
        UnsetTarget();
        
        selfRepairImage.transform.parent.gameObject.SetActive(true);
        curSelfRepair = 0;
    }

    public void CartEnabled() {
        canPickNewTargets = true;
        drone.SetActive(true);
        if(droneScript)
            droneScript.SetCurrentlyRepairingState(false);
        selfRepairImage.transform.parent.gameObject.SetActive(false);

        if (droneScript) {
            droneScript.currentChargePercent = 1f;
        }
    }

    public void ActivateForCombat() {
        //this.enabled = true;

        if (droneScript) {
            droneScript.currentChargePercent = 1f;
        }
    }

    public void Disable() {
        //this.enabled = false;
    }

    public class DroneStateData {
        public IPlayerHoldable currentlyHeld;
    }

    public interface IDroneState {
        public void EnterState();
        public void UpdateState();
        public void ExistState();
    }
}


