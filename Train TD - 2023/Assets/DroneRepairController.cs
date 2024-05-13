using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class DroneRepairController : MonoBehaviour, IResetState, IDisabledState {

    public Transform droneDockedPosition;
    public GameObject drone;
    public RepairDrone droneScript;
    public bool beingDirectControlled = false;


    private void Start() {
        droneScript = drone.GetComponent<RepairDrone>();
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.AddListener(OnTrainChanged);
        myHealth = GetComponentInParent<ModuleHealth>();
    }

    private void OnDestroy() {
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.RemoveListener(OnTrainChanged);
    }

    public void ActivateAutoDrone() {
        gettingNewTarget = false;
        TryGetNewTarget();
    }

    public void DisableAutoDrone() {
        if (target != null) {
            target.isTaken = false;
            target = null;
        }

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

    public float selfRepairPerSecond = 1;
    private float repairCharge = 0;

    public bool carryDraggableMode;
    public bool caughtCarry;
    private Vector3 targetVelocity;
    public Vector3 catchPosition;
    [ShowInInspector]
    public IPlayerHoldable myCarry;

    public float autoRepairTime = 2f;

    public Affectors currentAffectors;
    [Serializable]
    public class Affectors {
        public float directControlRepairTime = 0.7f;
        public float repairRateIncreaseMultiplier = 1;
        public float repairRateIncreaseReducer = 1;
        public float droneAccelerationReducer = 1;
        public float droneAccelerationIncreaser = 1;
        public int megaRepair = 0;
        public float droneSizeMultiplier = 1;
    }

    private bool canPickNewTargets = true;
    
    private void LateUpdate() {
        if (MissionLoseFinisher.s.isMissionLost) {
            return;
        }

        if (myHealth.GetMaxHealth() - myHealth.currentHealth >= ModuleHealth.repairChunkSize) {
            repairCharge += selfRepairPerSecond * Time.deltaTime;
            if (repairCharge >= ModuleHealth.repairChunkSize) {
                myHealth.RepairChunk();
                repairCharge -= ModuleHealth.repairChunkSize;
            }
        } else {
            repairCharge -= Time.deltaTime;
        }

        repairCharge = Mathf.Clamp(repairCharge, 0, 100);


        if (!carryDraggableMode &&LevelReferences.s.combatHoldableThings.Count > 0) {
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

            var targetPos = myCarry.GetUITargetTransform().position + Vector3.up/6 + Vector3.forward/4;
            
            var targetRot = Quaternion.LookRotation(carryMono.transform.position - drone.transform.position);

            if (!caughtCarry && Vector3.Distance(drone.transform.position, targetPos) < 0.15f) {
                CatchCarry();
            }
            
            if (caughtCarry && myCarry != PlayerWorldInteractionController.s.currentSelectedThing) {
                BringCarryCloser(carryMono);
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
                        MoveDroneWithVelocity(droneDockedPosition.transform.position, droneDockedPosition.transform.rotation);
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

                    if (Vector3.Distance(drone.transform.position, targetPos) < 0.01f) {
                        repairTimer += Time.deltaTime;
                        droneScript.SetCurrentlyRepairingState(true);


                        if (repairTimer >= GetRepairTime()) {
                            DoRepair(health, target);
                            TryGetNewTarget();

                            repairTimer = 0;
                        }
                    } else {
                        repairTimer -= Time.deltaTime;
                        droneScript.SetCurrentlyRepairingState(false);
                    }
                }
            }
        }
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
        var targetHoldPos = catchPosition;
        targetHoldPos.y = 1f + Mathf.Sin(Time.time * 0.4f) * 0.2f;
        targetHoldPos.x += Mathf.Sin(Time.time * 0.22f) * 0.2f;
        targetHoldPos.z += Mathf.Sin(Time.time * 0.2f) * 0.2f;

        if (catchPosition.magnitude > 2) {
            catchPosition = Vector3.SmoothDamp(catchPosition, Vector3.zero, ref targetVelocity, 1);
        }

        carryMono.transform.position = Vector3.SmoothDamp(carryMono.transform.position, targetHoldPos, ref targetVelocity, 0.1f);
        carryMono.transform.rotation = Quaternion.Slerp(carryMono.transform.rotation, Quaternion.identity, 1 * Time.deltaTime);
    }



    public void DoRepair(ModuleHealth targetHealth, RepairableBurnEffect chunk) {
        targetHealth.RepairChunk(chunk);
        
        if (currentAffectors.megaRepair > 0) {
            targetHealth.RepairChunk(100);
            for (int i = 1; i < currentAffectors.megaRepair; i++) {
                Train.s.GetNextBuilding(i, targetHealth.myCart)?.GetHealthModule().RepairChunk(100);
                Train.s.GetNextBuilding(-i, targetHealth.myCart)?.GetHealthModule().RepairChunk(100);
            }
        }

    }
    

    public float GetRepairTime() {
        return autoRepairTime * (currentAffectors.repairRateIncreaseReducer / currentAffectors.repairRateIncreaseMultiplier) * TweakablesMaster.s.myTweakables.autoRepairTimeMultiplier;
    }


    public float curVelocity;
    void MoveDroneWithVelocity(Vector3 pos, Quaternion rot) {
        var maxVelocity = 10f;
        var acceleration = 3f;
        acceleration /= currentAffectors.droneAccelerationReducer;

        curVelocity += acceleration * Time.deltaTime;

        var distance = Vector3.Distance( drone.transform.position,pos);
        if (carryDraggableMode) {
            distance = 10000;
            maxVelocity = 10000;
        }
        
        curVelocity = Mathf.Clamp(curVelocity, 0.05f, Mathf.Min(maxVelocity, distance));
        
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
        List<RepairableBurnEffect> allRepairs = new List<RepairableBurnEffect>();

        for (int i = 0; i < Train.s.carts.Count; i++) {
            allRepairs.AddRange(Train.s.carts[i].GetHealthModule().activeBurnEffects);
        }


        var closest = float.MaxValue;

        for (int i = 0; i < allRepairs.Count; i++) {
            var curRepair = allRepairs[i];

            if (curRepair.canRepair && !curRepair.isTaken) {
                var distance = Vector3.Distance(curRepair.transform.position, drone.transform.position);
                if (distance < closest) {
                    closest = distance;
                    target = curRepair;
                }
            }
        }

        if (target != null) {
            target.isTaken = true;
            health = target.GetComponentInParent<ModuleHealth>();

            //smoothTime = Mathf.Clamp(Vector3.Distance(drone.transform.position, target.transform.position), 0.2f,3f);
        }

        gettingNewTarget = false;
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
        
        foreach (var drone in GetComponentsInChildren<RepairDrone>()) {
            drone.transform.localScale = Vector3.one;
        }
    }

    public void CartDisabled() {
        canPickNewTargets = false;
        UnsetTarget();
    }

    public void CartEnabled() {
        canPickNewTargets = true;
    }
}
