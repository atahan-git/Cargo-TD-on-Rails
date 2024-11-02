using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DroneRepairController : MonoBehaviour, IResetState, IDisabledState, IRepairJuiceProvider {

    public Transform droneDockedPosition;
    public int dockOffset;
    public GameObject drone;
    public RepairDrone droneScript;
    public bool beingDirectControlled = false;

    public bool droneCannotActivateOverride = false;
    public bool droneActive = true;
    
    public bool carryDraggableMode;
    public bool caughtCarry;
    private Vector3 targetVelocity;
    public Vector3 catchPosition;
    [ShowInInspector]
    public IPlayerHoldable myCarry;
    
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

    public float currentJuice = 100;
    public float baseJuice = 100;
    public float naturalJuiceGeneration = 1;

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

    private void LateUpdate() {
        if (MissionLoseFinisher.s.isMissionLost) {
            return;
        }

        /*if (currentJuice < JuiceCapacity()) {
            if (EnemyWavesController.s.AnyEnemyIsPresent()) {
                currentJuice += naturalJuiceGeneration * Time.deltaTime * 0.1f;
            } else {
                currentJuice += naturalJuiceGeneration * Time.deltaTime * 1f;
            }
        }*/
        
        if (myHealth.myCart.isDestroyed && PlayStateMaster.s.isCombatInProgress()) {
            curSelfRepair += selfRepairSpeed * Time.deltaTime*5;
            curSelfRepair = Mathf.Clamp01(curSelfRepair);
            selfRepairImage.fillAmount = curSelfRepair / 1f;
            if (curSelfRepair >= 1 && HasJuice()) {
                curSelfRepair = 0;
                myHealth.RepairChunk(2);
                UseJuice();
            }
        }

        if (HighWindsController.s.currentlyHighWinds || (!HasJuice() && !beingDirectControlled)) {
            droneScript.SetCurrentlyRepairingState(false);
            MoveDroneWithVelocity(droneDockedPosition.transform.position + Vector3.up * dockOffset, droneDockedPosition.transform.rotation);

            if (!gettingNewTarget) {
                TryGetNewTarget(); // also unsets the target
            }

            if (carryDraggableMode) {
                StopHoldingThing();
            }
            
            return;
        }

        if (beingDirectControlled)
            return;


        if (!carryDraggableMode && LevelReferences.s.combatHoldableThings.Count > 0 && currentAffectors.IsActive()) {
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


            var targetPos = myCarry.GetUITargetTransform().position + Vector3.up / 6 + Vector3.forward / 4;

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
                drone.transform.rotation = targetRot;
            }


        } else {
            if (!beingDirectControlled) {
                if (target == null || target.gameObject == null) {
                    // docked?
                    droneScript.SetCurrentlyRepairingState(false);
                    if (!gettingNewTarget) {
                        MoveDroneWithVelocity(droneDockedPosition.transform.position + Vector3.up * dockOffset, droneDockedPosition.transform.rotation);
                    }

                    //TryGetNewTarget();
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

                        TryDoRepair(target, out bool removedArrow, out bool repairSuccess, Time.deltaTime);
                    } else {
                        repairTimer -= Time.deltaTime;
                        repairTimer = Mathf.Clamp(repairTimer, 0, 1);
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


    public float TryDoRepair(RepairableBurnEffect target, out bool removedArrow, out bool repairSuccess,float deltaTime, float extraMultiplier = 1) {
        removedArrow = false;
        repairSuccess = false;
        
        
        if (HighWindsController.s.IsStopped()) {
            extraMultiplier *= 2;
        }
        
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
        multiplier /= BiomeEffectsController.s.GetCurrentEffects().repairTimeMultiplier;
        return Mathf.Min(20,multiplier); // at 1 efficiency regular repair takes 5 sec, at 2 it takes 2.5 and so on
    }

    public float vampiricHealthStorage;
    public void DoRepair(ModuleHealth targetHealth, RepairableBurnEffect chunk) {
        var chunkPos = chunk.transform.position;
        targetHealth.RepairChunk(chunk);
        UseJuice();

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
                if (repairable != null && HasJuice()) {
                    repairable.GetComponentInParent<ModuleHealth>().RepairChunk(repairable);
                    UseJuice();
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
            FullyRepairTarget(targetHealth);
            for (int i = 1; i < currentAffectors.iron; i++) {
                var frontCart = Train.s.GetNextBuilding(i, targetHealth.myCart);
                if (frontCart != null) {
                    FullyRepairTarget(frontCart.GetHealthModule());
                }
                var backCart = Train.s.GetNextBuilding(-i, targetHealth.myCart);
                if (backCart != null) {
                    FullyRepairTarget(backCart.GetHealthModule());
                }
            }
        }

        var extraRepairs = GetExtraRepairCount();
        if (extraRepairs > 0) {
            var otherRepairChunks = targetHealth.activeBurnEffects;
            
            if (otherRepairChunks.Contains(chunk)) {
                otherRepairChunks.Remove(chunk);
            }

            otherRepairChunks.RemoveAll(x => x.hasArrow);

            otherRepairChunks.Sort((a, b) => {
                float distanceToA = Vector3.Distance(a.transform.position, chunkPos);
                float distanceToB = Vector3.Distance(b.transform.position, chunkPos);
                return distanceToA.CompareTo(distanceToB);
            });

            var chunksToRepair = Mathf.Min(extraRepairs, otherRepairChunks.Count);
            for (int i = 0; i < chunksToRepair; i++) {
                if(HasJuice()) {
                    targetHealth.RepairChunk(otherRepairChunks[i]);
                    UseJuice();
                } else {
                    break;
                }
            }
        }
    }

    private int GetExtraRepairCount() {
        return currentAffectors.fire + (beingDirectControlled ? 2 : 0);
    }

    public int GetExtraRepairs(RepairableBurnEffect[] list, ModuleHealth targetHealth, RepairableBurnEffect chunk ) {
        var extraRepairs = GetExtraRepairCount();
        var chunkPos = chunk.transform.position;
        if (extraRepairs > 0) {
            var otherRepairChunks = targetHealth.activeBurnEffects;

            if (otherRepairChunks.Contains(chunk)) {
                otherRepairChunks.Remove(chunk);
            }
            
            otherRepairChunks.RemoveAll(x => x.hasArrow);

            otherRepairChunks.Sort((a, b) => {
                float distanceToA = Vector3.Distance(a.transform.position, chunkPos);
                float distanceToB = Vector3.Distance(b.transform.position, chunkPos);
                return distanceToA.CompareTo(distanceToB);
            });
            for (int i = 0; i < otherRepairChunks.Count; i++) {
                Debug.DrawLine(chunkPos, otherRepairChunks[i].transform.position);
            }

            var repairJuiceEnoughness = Mathf.FloorToInt(_juiceTracker.GetCurrentJuice() / JuiceUse())-1;
            
            extraRepairs = Mathf.Min(extraRepairs, otherRepairChunks.Count, list.Length, repairJuiceEnoughness);
            
            for (int i = 0; i < extraRepairs; i++) {
                list[i] = otherRepairChunks[i];
            }

        } 
        
        return extraRepairs;
    }

    private void FullyRepairTarget(ModuleHealth targetHealth) {
        var chunkCount = targetHealth.GetChunkCount();
        for (int i = 0; i < chunkCount; i++) {
            if (HasJuice()) {
                targetHealth.RepairChunk();
                UseJuice();
            } else {
                break;
            }
        }
    }

    private AmmoTracker _ammoTracker;
    private RepairJuiceTracker _juiceTracker;
    void UseAmmo() {
        if (_ammoTracker != null) {
            _ammoTracker.UseAmmo(AmmoUse());
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

        return _ammoTracker.HasAmmo(AmmoUse());
    }
    
    void UseJuice(int multiplier =1) {
        if (_juiceTracker != null) {
            _juiceTracker.UseJuice(JuiceUse()*multiplier);
        }
    }


    float JuiceUse() {
        return 5f;
    }
    
    public bool HasJuice(int multiplier = 1) {
        if (_juiceTracker == null) {
            _juiceTracker = GetComponentInParent<RepairJuiceTracker>();
        }

        if (_juiceTracker == null) {
            return true;
        }

        return _juiceTracker.HasJuice(JuiceUse()*multiplier);
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
            /*if (LevelReferences.s.combatHoldableThings.Contains(myCarry)) {
                LevelReferences.s.combatHoldableThings.Remove(myCarry);
            }*/
        }
        myCarry = null;
        
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
        if (!droneActive || !canPickNewTargets || !currentAffectors.IsActive()) {
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
    }

    public float AvailableJuice() {
        return currentJuice;
    }

    public float JuiceCapacity() {
        return baseJuice * currentAffectors.power;
    }

    public float UseJuice(float amountUsed) {
        if (currentJuice >= amountUsed) {
            currentJuice -= amountUsed;
            return 0;
        } else {
            amountUsed -= currentJuice;
            currentJuice = 0;
            return amountUsed;
        }
    }

    public float FillJuice(float amountToFill) {
        if (currentJuice > JuiceCapacity()) {
            return amountToFill;
        }
        
        if (currentJuice + amountToFill <= JuiceCapacity()) {
            currentJuice += amountToFill;
            return 0;
        } else {
            amountToFill -= JuiceCapacity() - currentJuice;
            currentJuice = JuiceCapacity();
            return amountToFill;
        }
    }
}


