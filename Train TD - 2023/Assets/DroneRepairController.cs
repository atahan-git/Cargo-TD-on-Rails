using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneRepairController : MonoBehaviour {

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
    public IPlayerHoldable myCarry;

    private void LateUpdate() {
        var moveSpeed = 1f;
        var rotateSpeed = 120;

        if (myHealth.maxHealth - myHealth.currentHealth >= 50) {
            repairCharge += selfRepairPerSecond * Time.deltaTime;
            if (repairCharge >= 50) {
                myHealth.Repair(50);
                repairCharge -= 50;
            }
        } else {
            repairCharge -= Time.deltaTime;
        }

        repairCharge = Mathf.Clamp(repairCharge, 0, 100);


        if (!carryDraggableMode &&LevelReferences.s.combatHoldableThings.Count > 0) {
            for (int i = 0; i < LevelReferences.s.combatHoldableThings.Count; i++) {
                var holdable = LevelReferences.s.combatHoldableThings[i];
                if (holdable != null && ((MonoBehaviour)holdable) != null) {
                    if (holdable.GetHoldingDrone() == null) {
                        myCarry = holdable;
                        holdable.SetHoldingDrone(this);

                        carryDraggableMode = true;
                        caughtCarry = false;
                    }
                }
            }
        }

        if (carryDraggableMode) {
            droneScript.SetCurrentlyRepairingState(false);

            var carryMono = (MonoBehaviour)myCarry;

            var targetPos = myCarry.GetUITargetTransform().position + Vector3.up/6 + Vector3.forward/4;
            
            var targetRot = Quaternion.LookRotation(carryMono.transform.position - drone.transform.position);

            if (!caughtCarry && Vector3.Distance(drone.transform.position, targetPos) < 0.15f) {
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
            
            
            if (caughtCarry && myCarry != PlayerWorldInteractionController.s.currentSelectedThing) {
                var targetHoldPos = catchPosition;
                targetHoldPos.y = 1f + Mathf.Sin(Time.time*0.4f)*0.2f;
                targetHoldPos.x += Mathf.Sin(Time.time*0.22f)*0.2f;
                targetHoldPos.z += Mathf.Sin(Time.time*0.2f)*0.2f;

                if (catchPosition.magnitude > 2) {
                    catchPosition = Vector3.MoveTowards(catchPosition,Vector3.zero, 0.5f * Time.deltaTime);
                }

                carryMono.transform.position = Vector3.SmoothDamp(carryMono.transform.position, targetHoldPos, ref targetVelocity, 1f);
                carryMono.transform.rotation = Quaternion.Slerp(carryMono.transform.rotation, Quaternion.identity, 1 * Time.deltaTime);
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


                        if (repairTimer >= 2) {
                            health.RepairChunk(target);
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


    public float curVelocity;
    void MoveDroneWithVelocity(Vector3 pos, Quaternion rot) {
        var maxVelocity = 10f;
        var acceleration = 3f;

        curVelocity += acceleration * Time.deltaTime;

        var distance = Vector3.Distance( drone.transform.position,pos);
        if (carryDraggableMode) {
            distance = 10000;
        }
        
        curVelocity = Mathf.Clamp(curVelocity, 0.05f, Mathf.Min(maxVelocity, distance));
        
        drone.transform.position = Vector3.MoveTowards(drone.transform.position, pos, curVelocity*Time.deltaTime);
        drone.transform.rotation = ExtensionMethods.QuaterionSmoothDamp(drone.transform.rotation, rot, ref quatVel, 0.1f);
    }


    public void StopHoldingThing() {
        if (myCarry != null) {
            myCarry.SetHoldingDrone(null);

            if (LevelReferences.s.combatHoldableThings.Contains(myCarry)) {
                LevelReferences.s.combatHoldableThings.Remove(myCarry);
            }

            myCarry = null;
        }
        
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
}
