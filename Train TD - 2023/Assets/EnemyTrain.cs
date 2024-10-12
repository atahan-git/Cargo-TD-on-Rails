using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyTrain : MonoBehaviour {

    public List<EnemyHealth> carts = new List<EnemyHealth>();

    public void SetUpEnemyTrain() {
        carts = new List<EnemyHealth>(GetComponentsInChildren<EnemyHealth>());
        FillTrainGuns();
    }

    void FillTrainGuns() {
        var gunSlots = GetComponentsInChildren<EnemyGunSlot>();
        var curLevel = PlayStateMaster.s.currentLevel;

        for (int j = 0; j < gunSlots.Length; j++) {
            var thisSlot = gunSlots[j];
            switch (thisSlot.myType) {
                case EnemyGunSlot.GunSlotType.NormalOnly:
                    Instantiate(curLevel.enemyGuns[Random.Range(0, curLevel.enemyGuns.Length)], thisSlot.transform);
                    break;
                case EnemyGunSlot.GunSlotType.UniqueOnly:
                    Instantiate(curLevel.uniqueEquipment[Random.Range(0, curLevel.uniqueEquipment.Length)], thisSlot.transform);
                    break;
                case EnemyGunSlot.GunSlotType.EliteOnly:
                    Instantiate(curLevel.eliteEquipment[Random.Range(0, curLevel.eliteEquipment.Length)], thisSlot.transform);
                    break;
            }
        }
    }

    private float offsetSpeedChangeTime = 0.5f;
    private float currentOffsetChangeSpeed = 1f;
    private float leaveOffset = 0;
    private void Update() {
        if (LevelReferences.s.speed > 1) {
            offsetTime += Time.deltaTime * currentOffsetChangeSpeed * LevelReferences.s.speed * 0.1f;
            
            if (offsetSpeedChangeTime < 0) {
                offsetSpeedChangeTime = Random.Range(0.5f, 4);
                currentOffsetChangeSpeed = Random.Range(0.1f, 2f);
            }
        }

        if (!IsAlive()) {
            leaveOffset -= Time.deltaTime;
        }
    }

    void LateUpdate() {
        UpdateCartPositions();
    }

    public float GetTrainLength() {
        var totalLength = 0f;
        for (int i = 0; i < carts.Count; i++) {
            totalLength += carts[i].cartEnemyLength;
        }
        return totalLength;
    }

    private float offsetTime = 0;
    public void UpdateCartPositions() {
        if(carts.Count == 0)
            return;

        transform.position = PathAndTerrainGenerator.s.GetPointOnBossPath(0);
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnBossPath(0);

        var totalLength = GetTrainLength();

        var currentDistance = (totalLength / 2f);
        currentDistance += Mathf.Sin(offsetTime) * 0.5f;
        currentDistance += leaveOffset;

        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            var currentSpot = PathAndTerrainGenerator.s.GetPointOnBossPath(currentDistance);
            var currentRot = PathAndTerrainGenerator.s.GetRotationOnBossPath(currentDistance);

            var cartTransform = cart.transform;
            
            cartTransform.position = currentSpot;
            cartTransform.rotation = currentRot;

            currentDistance += -cart.cartEnemyLength;
            var index = i;
            cart.name = $"Cart {index }";
            /*cart.trainIndex = index;
            cart.cartPosOffset = currentDistance;*/
        }


        /*var upOffset = Vector3.up * trainFrontBackMiddleYOffset;

        var frontDist = (totalLength / 2f) + carts[0].length + 0.4f;
        var backDist = -(totalLength / 2f) + 0.2f;
        trainFront.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(frontDist) + upOffset;
        trainFront.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(frontDist);
        
        trainBack.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(backDist) + upOffset;
        trainBack.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(backDist);

        trainMiddle.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
        trainMiddle.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(0);*/
        
        //Physics.SyncTransforms();
        
        DoShake();
    }
    
    [Header("Train Shake Settings")] 
    public Vector3 shakeOffsetMax = new Vector3(0.005f, 0.012f, 0.005f);
    public float distancePerShake = 5f;
    public Vector3[] shakeOffsets;
    public bool[] shakeOffsetSet;
    void DoShake() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            var cartCount = carts.Count;
            
            if (shakeOffsets == null || shakeOffsets.Length != cartCount) {
                shakeOffsets = new Vector3[cartCount];
                shakeOffsetSet = new bool[cartCount];
            }
            
            
            var currentDistance = SpeedController.s.currentDistance;

            if (currentDistance > 7) {
                for (int i = 0; i < carts.Count; i++) {
                    var myCart = carts[i];

                    if (currentDistance % distancePerShake < 1f) {
                        if (!shakeOffsetSet[i]) {
                            shakeOffsets[i] = new Vector3(
                                Random.Range(-shakeOffsetMax.x, shakeOffsetMax.x),
                                Random.Range(-shakeOffsetMax.y, shakeOffsetMax.y),
                                Random.Range(-shakeOffsetMax.z, shakeOffsetMax.z)
                            );
                            shakeOffsetSet[i] = true;
                            
                            carts[i].transform.localPosition += shakeOffsets[i];
                        }

                    } else {
                        shakeOffsetSet[i] = false;
                    }

                    currentDistance += -myCart.cartEnemyLength;
                }
            }
        }
    }


    [Button]
    public void DebugSetCartsAndPositions() {
        carts = new List<EnemyHealth>(GetComponentsInChildren<EnemyHealth>());
        var totalLength = GetTrainLength();

        var currentDistance = (totalLength / 2f);

        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            var currentSpot = transform.position + Vector3.forward*currentDistance;
            var currentRot = Quaternion.identity;

            var cartTransform = cart.transform;
            
            cartTransform.position = currentSpot;
            cartTransform.rotation = currentRot;

            currentDistance += -cart.cartEnemyLength;
            var index = i;
        }
    }

    public bool IsAlive() {
        var oneRegularCartAlive = false;
        var oneEngineCartAlive = false;
        
        for (int i = 0; i < carts.Count; i++) {
            if (carts[i].isAlive) {
                if (carts[i].isEngine) {
                    oneEngineCartAlive = true;
                } else {
                    oneRegularCartAlive = true;
                }
            }
        }

        return oneRegularCartAlive && oneEngineCartAlive;
    }
}
