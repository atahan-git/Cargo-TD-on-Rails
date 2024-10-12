using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class AmmoDirectController : MonoBehaviour, IDirectControllable, IResetState {

    
    private void Start() {
        myHealth = GetComponentInParent<ModuleHealth>();
        myModuleAmmo = GetComponentInChildren<ModuleAmmo>();
        myAmmoBar = GetComponentInChildren<PhysicalAmmoBar>();
    }

    public ModuleHealth myHealth;
    public PhysicalAmmoBar myAmmoBar;
    public ModuleAmmo myModuleAmmo;
    public Transform[] directControlCamPositions;

    public Transform positionsParent;

    public Transform dropAmmoPos;
    public Transform dropAmmoBackMostPos;
    public Transform dropAmmoFrontMostPos;

    public float curPos;
    public Vector2 closeSpawnRange = new Vector2(0.3f, 0.35f);
    public Vector2 farSpawnRange = new Vector2(0f, 0.15f);

    public float changeDirChance = 0.1f;
    public bool curSpawnForward = true;
    public bool moveForward = true;
    public float moveSpeed = 0.2f;
    public float nextBlockDelay = 0.1f;
    public float nextBlockBadDelay = 0.2f;

    public float curDelay = 0;
    public bool needNewOnes = false;
    

    public bool dropping = false;

    // ammo box width = 0.13594
    // ammo box width half = 0.06797
    public float acceptableMatchDistance = 0.05f;
    public float perfectMatchDistance = 0.01f;
    
    public InputActionReference shootAction => DirectControlMaster.s.shootAction;
    public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;
    
    public GameObject ammo_perfect=> DirectControlMaster.s.ammo_perfect;
    public GameObject ammo_good=> DirectControlMaster.s.ammo_good;
    public GameObject ammo_full=> DirectControlMaster.s.ammo_full;
    public GameObject ammo_fail=> DirectControlMaster.s.ammo_fail;
    public GameObject highWindsActive=> DirectControlMaster.s.highWindsActive_ammo;

    public int baseReloadCount = 4;
    public int curReloadCount = 4;
    
    public List<GameObject> newOnes = new List<GameObject>();
    public List<Rigidbody> oldOnes = new List<Rigidbody>();

    public bool healOnReload = false;
    public int curPerfectComboCount = 0;


    public Affectors currentAffectors;

    [Serializable]
    public class Affectors {
        public float power = 1;
        public float speed = 1;
        public float efficiency = 1;

        public float uranium = 0;
        public float fire = 0;

        public bool vampiric = false;
    }

    public bool isThisDirectControlActive = false;
    public void ActivateDirectControl() {
        var currentCameraForward = MainCameraReference.s.cam.transform.forward;

        var bigestDot = float.MinValue;
        var targetTransform = directControlCamPositions[0];
        for (int i = 0; i < directControlCamPositions.Length; i++) {
            var dot = Vector3.Dot( directControlCamPositions[i].transform.forward,currentCameraForward);

            if (dot > bigestDot) {
                targetTransform = directControlCamPositions[i];
                bigestDot = dot;
            }
        }
        
        CameraController.s.ActivateDirectControl(targetTransform, false,true);
        
        ammo_perfect.SetActive(false);
        ammo_full.SetActive(false);
        ammo_good.SetActive(false);
        ammo_fail.SetActive(false);
        ammoFullColor.a = 0;
        ammoFull = false;
        DirectControlMaster.s.ammoMinigameUI.SetActive(true);
        
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.shoot);
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);

        SetNewCurPos(true);
        SetDropPos(curPos);

        needNewOnes = true;
        //myModuleAmmo.UseAmmo(10000);
        highWindsActive.SetActive(false);
        isThisDirectControlActive=true;
    }

    public void UpdateDirectControl() {
        // do nothing. This one has a regular update because the bullets need to keep falling
    }

    public void DisableDirectControl() {
        CameraController.s.DisableDirectControl();
        DirectControlMaster.s.ammoMinigameUI.SetActive(false);

        SetDropPos(0.5f);

        RemoveNewOnes();
        
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.directControlAlternativeActivate);
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);
        highWindsActive.SetActive(false);
        isThisDirectControlActive=false;
    }

    private bool ammoFull = false;
    Color ammoFullColor = Color.white;
    public void Update() {
        if (!isThisDirectControlActive) {
            if (dropping) {
                UpdateDroppingAmmoPhysics();
                if (!dropping) {
                    var dropWasSuccess = true;
                    var dropWasPerfect = false;

                    var newAmmoPos = newOnes[0].transform.position;
                    var preAmmoPos = myAmmoBar.noAmmoPos.position;
                    var matchDistMultiplier = 1f;
                    if (myAmmoBar.allAmmoChunks.Count > 0) {
                        preAmmoPos = myAmmoBar.allAmmoChunks[^1].transform.position;
                    } else {
                        matchDistMultiplier *= 2;
                    }

                    newAmmoPos.y = 0;
                    preAmmoPos.y = 0;
                    var dist = Vector3.Distance(preAmmoPos, newAmmoPos);
                    if (dist > acceptableMatchDistance) {
                        dropWasSuccess = false;
                    }

                    if (dropWasSuccess && (dist < perfectMatchDistance * currentAffectors.efficiency || currentAffectors.uranium > 0)) {
                        dropWasPerfect = true;
                    }

                    if (dropWasPerfect) {
                        curPerfectComboCount += 1;
                    } else {
                        curPerfectComboCount = 0;
                    }

                    curReloadCount = baseReloadCount * (curPerfectComboCount + 1);

                    if (currentAffectors.uranium > 0 && dropWasSuccess) {
                        myHealth.GetComponentInChildren<Artifact_UraniumGem>().ApplyDamage(myHealth.myCart, curReloadCount * 5);
                    }

                    if (dropWasSuccess) {
                        var toReload = newOnes.Count;
                        NewOnePlacementSuccess();
                        myModuleAmmo.Reload(toReload);
                        SetNewCurPos(true);
                    } else {
                        NewOnePlacementFailed();
                        SetNewCurPos(false);
                    }

                    if (dropWasPerfect) {
                        AnimateEffect(ammo_perfect);
                    } else if (dropWasSuccess) {
                        AnimateEffect(ammo_good);
                    } else {
                        AnimateEffect(ammo_fail);
                    }

                    needNewOnes = true;

                    dropping = false;
                }
            }
            return;
        }
        
        if (myHealth == null || myHealth.isDead || myHealth.myCart.isDestroyed || myHealth.myCart.isBeingDisabled || myModuleAmmo == null || myAmmoBar == null) {
            // in case our module gets destroyed
            DirectControlMaster.s.DisableDirectControl();
            return;
        }

        positionsParent.transform.localPosition = Vector3.Lerp(positionsParent.transform.localPosition, Vector3.up* (Mathf.Min(myModuleAmmo.curAmmo+18)+2)*myAmmoBar.ammoChunkHeight, 2*Time.deltaTime);

        var delayMultiplier = (1f / currentAffectors.speed);
        if (needNewOnes) {
            if (curDelay > 0) {
                curDelay -= Time.deltaTime;
            }

            var neededAmmoCount = Mathf.CeilToInt(myModuleAmmo.maxAmmo - myModuleAmmo.curAmmo);
            if (myModuleAmmo.maxAmmo - myModuleAmmo.curAmmo <= 0.2f) {
                neededAmmoCount = 0;
            }

            if (curDelay <= 0 ) {
                ammoFull = neededAmmoCount <= 0;
                ammo_full.SetActive(neededAmmoCount <= 0);
                if(neededAmmoCount > 0){
                    MakeNewOnes(Mathf.CeilToInt(curReloadCount*currentAffectors.power)); // this means sometimes we can "over-reload"
                }
            }
        }
        
        
        if (!needNewOnes && !dropping && shootAction.action.IsPressed() && !enterDirectControlShootLock) {
            SetNewOnesDropping();
            dropping = true;

            if (HighWindsController.s.currentlyHighWinds) {
                dropping = false;
                NewOnePlacementFailed();
                    
                curDelay = nextBlockBadDelay*delayMultiplier;
                SetNewCurPos(false);
            }
        }
        
        highWindsActive.SetActive(HighWindsController.s.currentlyHighWinds);
        var isStoppedMoving = HighWindsController.s.IsStopped();

        if (dropping) {
            UpdateDroppingAmmoPhysics();
            if (!dropping) { 
                var dropWasSuccess = true;
                var dropWasPerfect = false;
                
                var newAmmoPos = newOnes[0].transform.position;
                var preAmmoPos = myAmmoBar.noAmmoPos.position;
                var matchDistMultiplier = 1f;
                if (myAmmoBar.allAmmoChunks.Count > 0) {
                    preAmmoPos = myAmmoBar.allAmmoChunks[^1].transform.position;
                } else {
                    matchDistMultiplier *= 2;
                }

                newAmmoPos.y = 0;
                preAmmoPos.y = 0;
                var dist = Vector3.Distance(preAmmoPos, newAmmoPos);
                if (dist > acceptableMatchDistance) {
                    dropWasSuccess = false;
                }

                if (!isStoppedMoving) {
                    if (dropWasSuccess && (dist < perfectMatchDistance * currentAffectors.efficiency || currentAffectors.uranium > 0)) {
                        dropWasPerfect = true;
                    }

                    if (dropWasPerfect) {
                        curPerfectComboCount += 1;
                    } else {
                        curPerfectComboCount = 0;
                    }
                }

                curReloadCount = baseReloadCount*(curPerfectComboCount+1);

                if (currentAffectors.uranium > 0 && dropWasSuccess) {
                    myHealth.GetComponentInChildren<Artifact_UraniumGem>().ApplyDamage(myHealth.myCart, curReloadCount*5);
                }

                if (dropWasSuccess) {
                    var toReload = newOnes.Count;
                    NewOnePlacementSuccess();
                    myModuleAmmo.Reload(toReload);

                    curDelay = nextBlockDelay*delayMultiplier;
                    SetNewCurPos(true);
                } else {
                    NewOnePlacementFailed();
                    
                    curDelay = nextBlockBadDelay*delayMultiplier;
                    SetNewCurPos(false);
                }

                if (isStoppedMoving) {
                    curDelay = 0;
                }


                if (dropWasPerfect) {
                    AnimateEffect(ammo_perfect);
                }else if (dropWasSuccess) {
                    AnimateEffect(ammo_good);
                } else {
                    AnimateEffect(ammo_fail);
                }

                needNewOnes = true;

                dropping = false;
            }
        }

        //var speedMultiplier = currentAffectors.speed;
        var speedMultiplier = 1;
        
        if (!dropping && !needNewOnes) {
            if (isStoppedMoving) {
                curPos = Mathf.MoveTowards(curPos, 0.5f, moveSpeed * Time.deltaTime * speedMultiplier);
            } else {
                if (moveForward) {
                    curPos += moveSpeed * Time.deltaTime * speedMultiplier;
                    if (curPos >= 1f) {
                        curPos = 1f;
                        moveForward = false;
                    }
                } else {
                    curPos -= moveSpeed * Time.deltaTime * speedMultiplier;
                    if (curPos <= 0f) {
                        curPos = 0f;
                        moveForward = true;
                    }
                }
            }
        }

        SetDropPos(curPos);

        if (ammoFull) {
            ammoFullColor.a = Mathf.MoveTowards(ammoFullColor.a, 1f, 3 * Time.deltaTime);
            ammo_full.GetComponent<TMP_Text>().color = ammoFullColor;
            ammo_full.gameObject.SetActive(ammoFullColor.a > 0);
        } else {
            ammoFullColor.a = Mathf.MoveTowards(ammoFullColor.a, 0, 10 * Time.deltaTime);
            ammo_full.GetComponent<TMP_Text>().color = ammoFullColor;
            ammo_full.gameObject.SetActive(ammoFullColor.a > 0);
        }
    }
    
    public float vampiricHealthStorage;
    void DoAdd() {
        if (currentAffectors.vampiric) {
            vampiricHealthStorage += 50 * (curPerfectComboCount+1);

            while (vampiricHealthStorage > ModuleHealth.repairChunkSize) {
                GetComponentInParent<ModuleHealth>().RepairChunk();
                vampiricHealthStorage -= ModuleHealth.repairChunkSize;
            }
        }
    }

    void AnimateEffect(GameObject effect) {
        ammo_good.SetActive(false);
        ammo_fail.SetActive(false);
        ammo_perfect.SetActive(false);
        
        effect.SetActive(true);
        var anim = effect.GetComponent<Animation>();
        anim.Play();
    }
    
    private float acceleration = 2;
    public List<GameObject> droppingAmmoChunks = new List<GameObject>();
    public float velocity;
    private void UpdateDroppingAmmoPhysics() {
        var targetY = myAmmoBar.noAmmoPos.transform.position.y;
        if (myAmmoBar.allAmmoChunks.Count > 0) {
            for (int i = myAmmoBar.allAmmoChunks.Count - 1; i >= 0; i--) {
                if (myAmmoBar.velocity[i] <= 0) {
                    targetY =  myAmmoBar.allAmmoChunks[i].transform.position.y + myAmmoBar.ammoChunkHeight / 2f;
                    break;
                }
            }
        }
        
        for (int i = 0; i < droppingAmmoChunks.Count; i++) {
            var target = droppingAmmoChunks[i].transform.position;
            target.y = targetY;
            if (droppingAmmoChunks[i].transform.position.y > target.y) {
                droppingAmmoChunks[i].transform.position = Vector3.MoveTowards(droppingAmmoChunks[i].transform.position, target, velocity * Time.deltaTime);
            } else {
                dropping = false;
            }

            targetY += myAmmoBar.ammoChunkHeight;
        }

        if (droppingAmmoChunks.Count == 0) {
            dropping = false;
        }

        velocity += acceleration * Time.deltaTime;
    }

    void MakeNewOnes(int count) {
        needNewOnes = false;
        newOnes.Clear();
        
        var delta = Vector3.zero;
        for (int i = 0; i < count; i++) {
            var newOne  = Instantiate(myAmmoBar.ammoChunk, dropAmmoPos);
            newOne.transform.position += delta + new Vector3(Random.Range(-0.005f, 0.005f), 0, Random.Range(-0.005f, 0.005f));
            newOne.transform.GetChild(0).GetComponent<MeshRenderer>().material = LevelReferences.s.ammoLightUnactiveMat;
            //Instantiate(LevelReferences.s.reloadEffect_regular, newOne.transform);
            
            newOnes.Add(newOne);
            
            delta.y += myAmmoBar.ammoChunkHeight;
        }
        
        
        ammoFull = false;
    }

    void SetNewOnesDropping() {
        for (int i = 0; i < newOnes.Count; i++) {
            var newOne = newOnes[i];
            droppingAmmoChunks.Add(newOne);
            newOne.transform.SetParent(myAmmoBar.transform);
        }

        velocity = 0.2f;
    }
    
    void NewOnePlacementSuccess() {
        DoAdd();
        for (int i = 0; i < newOnes.Count; i++) {
            var newOne = newOnes[i];
            newOne.transform.GetChild(0).GetComponent<MeshRenderer>().material = LevelReferences.s.ammoLightActiveMat;
            myAmmoBar.allAmmoChunks.Add(newOne);
            myAmmoBar.velocity.Add(0f);
        }

        /*var otherAmmoModules = Train.s.GetComponentsInChildren<ModuleAmmo>();
        for (int i = 0; i < otherAmmoModules.Length; i++) {
            if (otherAmmoModules[i] != myModuleAmmo) {
                otherAmmoModules[i].Reload(droppingAmmoChunks.Count);
            }
        }*/
        
        droppingAmmoChunks.Clear();
        newOnes.Clear();


        /*if (healOnReload) {
            for (int i = 0; i < Train.s.carts.Count; i++) {
                Train.s.carts[i].GetHealthModule().RepairChunk(curPerfectComboCount+1);
            }
        }*/
    }

    public GameObject failedSpawnEffect;

    void NewOnePlacementFailed() {
        for (int i = 0; i < newOnes.Count; i++) {
            var newOne = newOnes[i];
            newOne.transform.SetParent(null);
            newOne.AddComponent<Rigidbody>();
            newOne.AddComponent<RubbleFollowFloor>();
            newOne.GetComponent<Rigidbody>().velocity = Vector3.down * velocity;
            oldOnes.Add(newOne.GetComponent<Rigidbody>());
        }

        droppingAmmoChunks.Clear();
        newOnes.Clear();

        if (failedSpawnEffect != null) {
            myHealth.DealDamage(50);
            VisualEffectsController.s.SmartInstantiate(failedSpawnEffect, myHealth.GetUITransform().position, Quaternion.identity);
        }
    }


    private void FixedUpdate() {
        for (int i = 0; i < oldOnes.Count; i++) {
            if (oldOnes[i] != null) {
                var forceMultiplier = 1f;
                if (oldOnes[i].position.x < 0) {
                    forceMultiplier *= -1;
                }

                var rubble = oldOnes[i].GetComponent<RubbleFollowFloor>();
                if (rubble.isAttachedToFloor) {
                    oldOnes.RemoveAt(i);
                    i -= 1;
                    continue;
                }
                
                var deathTime = rubble.deathTime;
                if (deathTime < 15) {
                    forceMultiplier *= 16f - deathTime;
                }
                
                oldOnes[i].AddForce(new Vector3(500, 0, 0) * Time.fixedDeltaTime * forceMultiplier);
            } else {
                oldOnes.RemoveAt(i);
                i -= 1;
            }
        }
    }

    void SetNewCurPos(bool isClose) {
        if (isClose) {
            curPos = Random.Range(closeSpawnRange.x, closeSpawnRange.y);
        } else {
            curPos = Random.Range(farSpawnRange.x, farSpawnRange.y);
        }

        if (Random.value < changeDirChance) {
            curSpawnForward = !curSpawnForward;
        }

        if (curSpawnForward) {
            curPos = 1 - curPos;
        }

        if (HighWindsController.s.IsStopped()) {
            curPos = 0.5f;
        }

        moveForward = !curSpawnForward;
    }

    void RemoveNewOnes() {
        for (int i = 0; i < newOnes.Count; i++) {
            var newOne = newOnes[i];
            Destroy(newOne.gameObject);
        }
        
        droppingAmmoChunks.Clear();
        newOnes.Clear();
    }
    
    void SetDropPos(float pos) {
        dropAmmoPos.transform.position = Vector3.Lerp(dropAmmoBackMostPos.transform.position, dropAmmoFrontMostPos.transform.position, pos);
    }

    public Color GetHighlightColor() {
        return PlayerWorldInteractionController.s.reloadColor;
    }

    public GamepadControlsHelper.PossibleActions GetActionKey() {
        return GamepadControlsHelper.PossibleActions.reloadControl;
    }

    public void ResetState() {
        currentAffectors = new Affectors();
    }
}
