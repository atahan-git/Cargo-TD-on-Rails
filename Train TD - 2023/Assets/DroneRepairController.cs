using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class DroneRepairController : MonoBehaviour, IResetState, IDisabledState {

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
    private float repairCharge = 0;

    public bool carryDraggableMode;
    public bool caughtCarry;
    private Vector3 targetVelocity;
    public Vector3 catchPosition;
    [ShowInInspector]
    public IPlayerHoldable myCarry;

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
        
        public bool vampiric = false;
        public bool ancientDisabled = false;
        public bool lizardOverride = false;
        public bool IsActive() {
            return lizardOverride || !ancientDisabled;
        }
    }

    private bool canPickNewTargets = true;
    
    private void LateUpdate() {
        if (MissionLoseFinisher.s.isMissionLost) {
            return;
        }

        if (myHealth.myCart.isDestroyed) {
            repairCharge += selfRepairSpeed* Time.deltaTime;
            if (repairCharge > 1) {
                repairCharge -= 1;
                myHealth.RepairChunk(1);
            }
        }

        if (!carryDraggableMode &&LevelReferences.s.combatHoldableThings.Count > 0 && currentAffectors.IsActive()) {
            for (int i = 0; i < LevelReferences.s.combatHoldableThings.Count; i++) {
                var holdable = LevelReferences.s.combatHoldableThings[i];
                if (holdable != null && ((MonoBehaviour)holdable) != null) {
                    if (holdable.GetHoldingDrone() == null && holdable != PlayerWorldInteractionController.s.currentSelectedThing) {
                        myCarry = holdable;
                        holdable.SetHoldingDrone(this);

                        carryDraggableMode = true;
                        caughtCarry = false;
                    }
                }
            }
        }

        if (carryDraggableMode) {
            if (myCarry.GetUITargetTransform() == null) {
                StopHoldingThing();
                return;
            }

            droneScript.SetCurrentlyRepairingState(false);

            var carryMono = (MonoBehaviour)myCarry;

            var playerIsCarrying = myCarry == PlayerWorldInteractionController.s.currentSelectedThing && PlayerWorldInteractionController.s.isDragging();
            
            if (caughtCarry && !playerIsCarrying) {
                BringCarryCloser(carryMono);
            }
            

            var targetPos = myCarry.GetUITargetTransform().position + Vector3.up/6 + Vector3.forward/4;
            
            var targetRot = Quaternion.LookRotation(carryMono.transform.position - drone.transform.position);

            if (!caughtCarry && 
                (Vector3.Distance(drone.transform.position, targetPos) < 0.15f || 
                 Vector3.Distance(drone.transform.position, Train.s.trainMiddle.transform.position) > 10)) {
                CatchCarry();
            }

            if (!caughtCarry) {
                MoveDroneWithVelocity(targetPos, targetRot);
            } else {
                drone.transform.position = targetPos;
                drone.transform.rotation =  targetRot;
            }


        } else {
            if (!beingDirectControlled) {
                if (target == null) {
                    droneScript.SetCurrentlyRepairingState(false);
                    if (!gettingNewTarget) {
                        if(droneDockedPosition != null)
                            MoveDroneWithVelocity(droneDockedPosition.transform.position + Vector3.up*dockOffset, droneDockedPosition.transform.rotation);
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

                        TryDoRepair(target, out bool removedArrow, out bool repairSuccess);
                    } else {
                        repairTimer -= Time.deltaTime;
                        repairTimer = Mathf.Clamp(repairTimer, 0, 1);
                        droneScript.SetCurrentlyRepairingState(false);
                    }
                }
            }
        }
    }


    public float TryDoRepair(RepairableBurnEffect target, out bool removedArrow, out bool repairSuccess, float extraMultiplier = 1) {
        removedArrow = false;
        repairSuccess = false;
        if (target.hasArrow) {
            repairTimer = target.removeArrowState;
            repairTimer += Time.deltaTime * GetArrowRepairTimeMultiplier()*extraMultiplier;
            target.SetRemoveArrowState(repairTimer);
            if (repairTimer > 1) {
                target.RemoveArrow();
                removedArrow = true;
                repairTimer = 0;
            }
        } else {
            repairTimer += Time.deltaTime*GetRepairTimeMultiplier()*extraMultiplier;
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
        return Mathf.Min(10,0.2f * currentAffectors.efficiency); // at 1 efficiency repair arrow takes 5 sec
    }
    
    public float GetRepairTimeMultiplier() {
        return Mathf.Min(10,0.2f * currentAffectors.efficiency * repairSpeedMultiplier); // at 1 efficiency regular repair takes 5 sec
    }

    private void CatchCarry() {
        var rigid = ((MonoBehaviour)myCarry).GetComponent<Rigidbody>();

        //targetVelocity = rigid.velocity;
        targetVelocity = Vector3.zero;
        catchPosition = rigid.transform.position;
        catchPosition.y = 0;

        rigid.isKinematic = true;
        rigid.useGravity = false;
        caughtCarry = true;

        var rubbleFollowFloor = ((MonoBehaviour)myCarry).GetComponent<RubbleFollowFloor>();
        if (rubbleFollowFloor) {
            rubbleFollowFloor.UnAttachFromFloor();
            rubbleFollowFloor.canAttachToFloor = false;
        }
    }
    
    private void BringCarryCloser(MonoBehaviour carryMono) {

        var trainMiddlePos = Train.s.trainMiddle.position;
        catchPosition += Train.s.GetTrainForward() * LevelReferences.s.speed*Time.deltaTime;
        if (Vector3.Distance(catchPosition, trainMiddlePos) >2) {
            catchPosition = Vector3.Lerp(catchPosition, trainMiddlePos, 0.5f*Time.deltaTime);
        } else {
        }
        
        Debug.DrawLine(catchPosition, catchPosition+Vector3.up*5,Color.yellow);
        Debug.DrawLine(trainMiddlePos, trainMiddlePos+Vector3.up*5,Color.yellow);

        var targetHoldPos = catchPosition;
        targetHoldPos.y = 1f + Mathf.Sin(Time.time * 0.4f) * 0.2f;
        targetHoldPos.x += Mathf.Sin(Time.time * 0.22f) * 0.2f;
        targetHoldPos.z += Mathf.Sin(Time.time * 0.2f) * 0.2f;

        carryMono.transform.position = Vector3.SmoothDamp(carryMono.transform.position, targetHoldPos, ref targetVelocity, 0.1f);
        carryMono.transform.rotation = Quaternion.Slerp(carryMono.transform.rotation, Quaternion.identity, 1 * Time.deltaTime);
    }



    public float vampiricHealthStorage;
    public void DoRepair(ModuleHealth targetHealth, RepairableBurnEffect chunk) {
        targetHealth.RepairChunk(chunk);

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
            targetHealth.RepairChunk(100);
            for (int i = 1; i < currentAffectors.iron; i++) {
                Train.s.GetNextBuilding(i, targetHealth.myCart)?.GetHealthModule().RepairChunk(100);
                Train.s.GetNextBuilding(-i, targetHealth.myCart)?.GetHealthModule().RepairChunk(100);
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
        return 0.5f;
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
        if (carryDraggableMode) {
            distance = 10000;
            _maxVelocity = 10000;
            
            curVelocity += 1 * Time.deltaTime;
        }
        
        curVelocity = Mathf.Clamp(curVelocity, 0.2f, Mathf.Min(_maxVelocity, distance*5));
        
        drone.transform.position = Vector3.MoveTowards(drone.transform.position, pos, curVelocity*Time.deltaTime);
        drone.transform.rotation = ExtensionMethods.QuaterionSmoothDamp(drone.transform.rotation, rot, ref quatVel, 0.1f);
    }


    public void StopHoldingThing() {
        if (myCarry != null && myCarry.GetUITargetTransform() != null) {
            myCarry.SetHoldingDrone(null);
            var rigid = ((MonoBehaviour)myCarry).GetComponent<Rigidbody>();
            if (!rigid.GetComponentInParent<Train>()) {
                rigid.isKinematic = false;
                rigid.useGravity = true;
            }

            var rubbleFollowFloor = ((MonoBehaviour)myCarry).GetComponent<RubbleFollowFloor>();
            if (rubbleFollowFloor) {
                rubbleFollowFloor.canAttachToFloor = true;
            }
        }

        if (myCarry != null) {
            
            if (LevelReferences.s.combatHoldableThings.Contains(myCarry)) {
                LevelReferences.s.combatHoldableThings.Remove(myCarry);
            }
            myCarry = null;
        }
        
        caughtCarry = false;
        
        UnsetTarget();
        TryGetNewTarget();

        carryDraggableMode = false;
        
    }

    void OnTrainChanged() {
        if (target == null) {
            TryGetNewTarget();
        }
    }

    void TryGetNewTarget() {
        if(!droneActive || !canPickNewTargets || !currentAffectors.IsActive())
            return;

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
    }

    public void CartEnabled() {
        canPickNewTargets = true;
        drone.SetActive(true);
    }
}
