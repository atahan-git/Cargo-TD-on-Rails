using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TeleporterHarpoonsModule : MonoBehaviour
{
    public float range = 4;

    [Serializable]
    public class HarpoonData {
        public GameObject harpoonObject;
        public Transform harpoonSpawnLocation;
        public Transform ropeEndPoint;

        public Transform harpoonRotatePoint;

        public bool harpoonEngaged;
        public bool ropeLerpInProgress;
        public bool harpoonLerpInProgress;

        public Cart target;

        public GameObject activeTeleportingEffectObject;
    }


    public HarpoonData[] bothHarpoons = new HarpoonData[2];

    private EnemyWave myWave;
    private EnemyHealth myHealth;
    private void Start() {
        myWave = GetComponentInParent<EnemyWave>();
        myHealth = GetComponentInParent<EnemyHealth>();
        
        waitTimer = waitState;
        aimAndFireTimer = aimAndFireState;
        teleportingTimer = teleportingState;
    }


    // 1. wait state for 10 seconds
    // 2. aim and fire state for 15 seconds
    // 3. teleporting state for 2 seconds
    // - repeat-
    

    public float waitTimer;
    public float aimAndFireTimer;
    public float teleportingTimer;

    public float waitState = 10f;
    public float aimAndFireState = 15f;
    public float teleportingState = 2f;

    void DisconnectHarpoons() {
        for (int i = 0; i < bothHarpoons.Length; i++) {
            var harpoonObject = bothHarpoons[i].harpoonObject;
            var harpoonSpawnLocation = bothHarpoons[i].harpoonSpawnLocation;
            var ropeEndPoint = bothHarpoons[i].ropeEndPoint;

            if (bothHarpoons[i].harpoonEngaged) {
                if (!bothHarpoons[i].harpoonLerpInProgress) {
                    Instantiate(harpoonDisengageEffect, harpoonObject.transform.position, harpoonObject.transform.rotation);
                
                    bothHarpoons[i].harpoonObject = Instantiate(harpoonObject, harpoonSpawnLocation.position, harpoonSpawnLocation.rotation);
                
                    bothHarpoons[i].harpoonObject.transform.SetParent(harpoonSpawnLocation);
                    bothHarpoons[i].ropeLerpInProgress = true;
                    StartCoroutine(RopeLerp(bothHarpoons[i]));
                } else {
                    StopAllCoroutines();
                    harpoonObject.transform.position = harpoonSpawnLocation.position;
                    harpoonObject.transform.rotation = harpoonSpawnLocation.rotation;
                
                    var ropeTarget = harpoonObject.transform.GetChild(0);
                    ropeEndPoint.transform.position = ropeTarget.transform.position;
                    ropeEndPoint.SetParent(ropeTarget);
                    bothHarpoons[i].ropeLerpInProgress = false;
                    bothHarpoons[i].harpoonLerpInProgress = false;
                }
                
                bothHarpoons[i].harpoonEngaged = false;
                harpoonReAttachTimer = 2f;
                
                bothHarpoons[i].target = null;
            }
        }
        
    }

    public GameObject harpoonShootEffect;
    public GameObject harpoonDisengageEffect;
    public GameObject teleportingCartStartEffect;
    public GameObject teleportingCartEndEffect;
    IEnumerator RopeLerp(HarpoonData myHarpoon) {
        myHarpoon.ropeEndPoint.SetParent(null);
        var ropeTarget = myHarpoon.harpoonObject.transform.GetChild(0);
        var ropeEndPoint = myHarpoon.ropeEndPoint;
        while (ropeEndPoint.transform.position.y < 1) {
            ropeEndPoint.transform.position += Vector3.up * Time.deltaTime * 3;
            yield return null;
        }
        
        while (Vector3.Distance(ropeEndPoint.transform.position, ropeTarget.transform.position) > 0.01f) {
            ropeEndPoint.transform.position = Vector3.MoveTowards(ropeEndPoint.transform.position, ropeTarget.transform.position, 3 * Time.deltaTime);
            yield return null;
        }

        ropeEndPoint.transform.position = ropeTarget.transform.position;
        ropeEndPoint.SetParent(ropeTarget);

        yield return new WaitForSeconds(2);
        myHarpoon.ropeLerpInProgress = false;
    }
    
    IEnumerator HarpoonLerp(HarpoonData myHarpoon) {
        var harpoonEngagePoint = new GameObject("HarpoonEngagePoint");
        var target = myHarpoon.target;
        var harpoonObject = myHarpoon.harpoonObject;
        
        var targetCollider = target.GetComponentInChildren<BoxCollider>();
        harpoonEngagePoint.transform.position = targetCollider.ClosestPoint(transform.position);
        var deeperIntoTargetVector = harpoonEngagePoint.transform.position - harpoonObject.transform.position;
        harpoonEngagePoint.transform.position += deeperIntoTargetVector.normalized * 0.3f;
        harpoonEngagePoint.transform.SetParent(target.transform);


        while (Vector3.Distance(harpoonObject.transform.position, harpoonEngagePoint.transform.position) > 0.01f) {
            harpoonObject.transform.position = Vector3.MoveTowards(harpoonObject.transform.position, harpoonEngagePoint.transform.position, 6 * Time.deltaTime);
            yield return null;
        }

        harpoonObject.transform.position = harpoonEngagePoint.transform.position;
        harpoonObject.transform.SetParent(harpoonEngagePoint.transform);

        myHarpoon.harpoonLerpInProgress = false;
        myHarpoon.harpoonEngaged = true;
    }


    public bool canShoot;
    private float harpoonReAttachTimer;
    public float carTeleportTimer = 0;
    public float carTeleportingState = 10f;

    private void Update() {
        var distance = Mathf.Abs(myWave.GetDistance() - SpeedController.s.currentDistance);

        myWave.teleportTimer = 1000; // we dont want to use the same teleporter logic as the rest of the enemies
        myWave.targetDistChangeTimer = 1000;

        // if we get too far away we still want to teleport closer
        if (carTeleportTimer <= 0 && distance > 30 && distance < 40) {
            carTeleportTimer = carTeleportingState;
            myWave.Teleport();
        }

        if (carTeleportTimer > 0) {
            carTeleportTimer -= Time.deltaTime;
        }


        // if we get out of range stop everything and try again
        if (distance > range && teleportingTimer > 0) { // but we don't want to cancel after the teleporting has begun
            for (int i = 0; i < bothHarpoons.Length; i++) {
                if (bothHarpoons[i].harpoonEngaged) {
                    DisconnectHarpoons();
                }
            }

            waitTimer = waitState;
            aimAndFireTimer = aimAndFireState;
            teleportingTimer = teleportingState;
            return;
        }

        if (distance > range) {
            return;
        }

        // wait state
        if (waitTimer > 0) {
            waitTimer -= Time.deltaTime;
            return;
        }

        //aim and fire state
        if (aimAndFireTimer > 0) {
            aimAndFireTimer -= Time.deltaTime;

            for (int i = 0; i < bothHarpoons.Length; i++) {
                var currentHarpoon = bothHarpoons[i];
                var otherHarpoon = bothHarpoons[(i + 1) % 2];

                AimAndFire(currentHarpoon, otherHarpoon);
            }

            return;
        }

        // teleporting state
        if (teleportingTimer > 0) {
            teleportingTimer -= Time.deltaTime;

            for (int i = 0; i < bothHarpoons.Length; i++) {
                var currentHarpoon = bothHarpoons[i];

                if (currentHarpoon.activeTeleportingEffectObject == null) {
                    var newEffect = Instantiate(teleportingCartStartEffect, currentHarpoon.target.transform.position, currentHarpoon.target.transform.rotation);
                    newEffect.transform.SetParent(currentHarpoon.target.transform);
                    currentHarpoon.activeTeleportingEffectObject = newEffect;
                }
            }

            return;
        }

        // after teleporting state do the teleport
        if (bothHarpoons[0].activeTeleportingEffectObject != null) {
            
            Train.s.SwapCarts(bothHarpoons[0].target, bothHarpoons[1].target);
            

            Instantiate(teleportingCartEndEffect, bothHarpoons[0].target.transform.position, bothHarpoons[0].target.transform.rotation);
            Instantiate(teleportingCartEndEffect, bothHarpoons[1].target.transform.position, bothHarpoons[1].target.transform.rotation);

            bothHarpoons[0].activeTeleportingEffectObject = null;
            bothHarpoons[1].activeTeleportingEffectObject = null;

            DisconnectHarpoons();
            carTeleportTimer = carTeleportingState;
            return;
        }

        
        // then wait a bit then teleport our car and begin again.
        if (carTeleportTimer < 0) {
            carTeleportTimer = carTeleportingState;
            myWave.Teleport();

            // finish up
            waitTimer = waitState;
            aimAndFireTimer = aimAndFireState;
            teleportingTimer = teleportingState;
        }
    }

    void AimAndFire(HarpoonData currentHarpoon, HarpoonData otherHarpoon) {
        if (currentHarpoon.target == null) {
            var doCount = 0;
            do {
                currentHarpoon.target = Train.s.carts[Random.Range(1, Train.s.carts.Count)].GetComponent<Cart>(); // make sure it doesnt target the engine at the front
                doCount += 1;
            } while (currentHarpoon.target == otherHarpoon.target && doCount < 20);

            if (currentHarpoon.target == otherHarpoon.target) { // there arent enough carts left
                DisconnectHarpoons();
                
                carTeleportTimer = carTeleportingState;
                waitTimer = waitState;
                aimAndFireTimer = aimAndFireState;
                teleportingTimer = teleportingState;
                return;
            }
        }


        var lookAxis = currentHarpoon.target.transform.position - currentHarpoon.harpoonRotatePoint.position;
        var lookRotation = Quaternion.LookRotation(lookAxis, Vector3.up);
        currentHarpoon.harpoonRotatePoint.rotation = Quaternion.Lerp(currentHarpoon.harpoonRotatePoint.rotation, lookRotation, 10 * Time.deltaTime);
        canShoot = Quaternion.Angle(currentHarpoon.harpoonRotatePoint.rotation, lookRotation) < 5;

        if (currentHarpoon.harpoonEngaged) {

            /*if (Vector3.Distance(transform.position, target.transform.position) > range) {
                DisconnectHarpoons();
            }*/
            
        } else {
            harpoonReAttachTimer -= Time.deltaTime;

            if (harpoonReAttachTimer <= 0) {
                if (!currentHarpoon.ropeLerpInProgress && !currentHarpoon.harpoonLerpInProgress && canShoot) {
                    currentHarpoon.harpoonLerpInProgress = true;
                    Instantiate(harpoonShootEffect, currentHarpoon.harpoonObject.transform.position, currentHarpoon.harpoonObject.transform.rotation);
                    StartCoroutine(HarpoonLerp(currentHarpoon));
                }
            }
        }
    }

    void OnCartDestroyed() {
        DisconnectHarpoons();
        carTeleportTimer = carTeleportingState;
        waitTimer = waitState;
        aimAndFireTimer = aimAndFireState;
        teleportingTimer = teleportingState;
    }

    private void OnEnable() {
        Train.s.onTrainCartsChanged.AddListener(OnCartDestroyed);
    }

    private void OnDisable() {
        Train.s.onTrainCartsChanged.RemoveListener(OnCartDestroyed);
    }
}
