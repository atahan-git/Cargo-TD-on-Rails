using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

public class EnemyWave : MonoBehaviour, IShowOnDistanceRadar, ISpeedForEngineSoundsProvider {
    public EnemyIdentifier myEnemy;
    public EnemySwarmMaker drawnEnemies;
    public float mySpeed;

    public MiniGUI_IncomingWave waveDisplay;
                
    public bool isWaveMoving = false;
    public float wavePosition = -1;
    public bool isLeft;

    private LineRenderer lineRenderer;
    
    public Material deadlyMaterial;
    public Material safeMaterial;

    public float waveSpawnXSpread = 0;
    public float myXOffset = 0;
    public float targetXOffset = 0;


    private bool isTeleporting = false;
    private Vector2 teleportTiming = Vector2.zero;

    public bool isStealing = false;
    public bool isLeaving = false;
    public bool isForwardLeave = false;

    public bool IsTrain() {
        return false;
    }
    
    public Sprite GetMainSprite() {
        return DataHolder.s.GetEnemy(myEnemy.enemyUniqueName).GetComponent<EnemySwarmMaker>().enemyIcon;
    }

    public Sprite GetGunSprite() {
        return DataHolder.s.GetEnemy(myEnemy.enemyUniqueName).GetComponent<EnemySwarmMaker>().GetGunSprite();
    }
    
    private void Start() {
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    private DisablerHarpoonModule _disablerHarpoonModule;
    public void SetUp(EnemyIdentifier data, float position, bool isMoving, bool _isLeft, Artifact artifact) {
        myEnemy = data;
        var en = DataHolder.s.GetEnemy(myEnemy.enemyUniqueName);
        var mySwarm = en.GetComponent<EnemySwarmMaker>();
        if (mySwarm == null) {
            Debug.LogError($"Enemy is missing swarm maker {en.gameObject.name} {data.enemyUniqueName}");
        }

        mySpeed = mySwarm.speed;
        isTeleporting = mySwarm.isTeleporting;
        teleportTiming = mySwarm.teleportTiming;
        isStealing = mySwarm.isStealing;
        neverLeave = mySwarm.neverLeave;
        isNuker = mySwarm.isNuker;
        nukingTime = mySwarm.nukingTime;
        wavePosition = position;
        isWaveMoving = isMoving;

        if (isMoving) {
            currentSpeed = mySpeed;
        }
        
        isLeft = _isLeft;
        SpawnEnemy(artifact);
        
        SetTargetPosition();
        myXOffset = targetXOffset;

        DistanceAndEnemyRadarController.s.RegisterUnit(this);

        teleportTimer = 0;

        _disablerHarpoonModule = GetComponentInChildren<DisablerHarpoonModule>();
    }

    private void OnDestroy() {
        DestroyRouteDisplay();
        DistanceAndEnemyRadarController.s.RemoveUnit(this);
        EnemyWavesController.s.RemoveWave(this);
    }


    const float speedChangeDelta = 1f;
    public float currentSpeed = 0;
    public float targetSpeed = 0;
    public float distance;
    public float targetDistanceOffset;
    public float currentDistanceOffset;
    public float targetDistChangeTimer;
    public float teleportTimer;

    private bool instantLerp = false;
    private bool movePos = true;
    public Vector3 lookVector = Vector3.forward;
    public void UpdateBasedOnDistance(float playerPos) {
        if (PlayStateMaster.s.isCombatInProgress()) {
            distance = Mathf.Abs(playerPos - wavePosition);
            
            if (!isWaveMoving) {
                if (distance < 10)
                    isWaveMoving = true;
            }

            targetSpeed = Mathf.Min(mySpeed, Mathf.Max(distance, LevelReferences.s.speed) + 0.2f);

            if (isLeaving) {
                targetSpeed = isForwardLeave ? mySpeed : -mySpeed;
            }

            if (isWaveMoving) {
                if (playerPos < wavePosition && !isLeaving) {
                    targetSpeed = Mathf.Min(mySpeed,Mathf.FloorToInt(LevelReferences.s.speed - 1));
                }

                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeDelta * Time.deltaTime);

                wavePosition += currentSpeed * Time.deltaTime;
            }

            lookVector = PathAndTerrainGenerator.s.GetDirectionVectorOnActivePath(wavePosition - playerPos + currentDistanceOffset);
            var left = Quaternion.AngleAxis(-90, Vector3.up) * lookVector;
            var targetPos = PathAndTerrainGenerator.s.GetPointOnActivePath(wavePosition - playerPos + currentDistanceOffset) + left * myXOffset;
            //Debug.DrawLine(targetPos, targetPos + left * 5, Color.red, 1f);
            //Debug.DrawLine(targetPos, targetPos +Vector3.up*5, Color.green, 1f);
            if(movePos)
                transform.position = Vector3.Lerp(transform.position,targetPos, 20*Time.deltaTime);
            
            if (instantLerp) {
                transform.position = targetPos;
                instantLerp = false;
            }
            var effectiveSpeed = currentSpeed - slowAmount;
            slowAmount -= slowDecay * Time.deltaTime;
            slowAmount = Mathf.Clamp(slowAmount, 0, float.MaxValue);
            if (slowAmount <= 0) {
                ToggleSlowedEffect(false);
            }
            
            currentDistanceOffset = Mathf.MoveTowards(currentDistanceOffset, targetDistanceOffset, effectiveSpeed / 10f * Time.deltaTime);

            targetDistChangeTimer -= Time.deltaTime;
            if (targetDistChangeTimer <= 0 && !isLeaving) {
                targetDistChangeTimer = Random.Range(2f, 15f);
                if (isStealing) {
                    targetDistChangeTimer = Random.Range(15f, 25f);
                }
                SetTargetPosition();
            }
            
            if (isTeleporting && !isLeaving) {
                teleportTimer -= Time.deltaTime;
                if (teleportTimer <= 0 && distance < 10) {
                    teleportTimer = Random.Range(teleportTiming.x, teleportTiming.y);
                    Teleport();
                }
                
                if (distance > 30 && teleportTimer <= 0 && distance < 40) {
                    teleportTimer = Random.Range(teleportTiming.y * 0.9f, teleportTiming.y * 1.1f);
                    Teleport();
                }
            }

            var showRouteDisplay = distance > 10 && distance < 60;
            
            if (waveDisplay != null && distance < 10) {
                PlayEnemyEnterSound();
            }
            
            if (showRouteDisplay) {
                CreateRouteDisplay();
            } else {
                DestroyRouteDisplay();
            }
            
            

            
            if (distance > 100 && isLeaving) {
                Destroy(gameObject);
            }

            myXOffset = Mathf.MoveTowards(myXOffset, targetXOffset, 0.1f * Time.deltaTime * currentSpeed);
            
        } else if(PlayStateMaster.s.isCombatFinished()) {
            if (MissionWinFinisher.s.isWon) {
                targetSpeed = 0;
            } else {
                targetSpeed = mySpeed;
            }
            transform.position = Vector3.forward * (wavePosition - playerPos + currentDistanceOffset - 0.2f) + Vector3.left * myXOffset;
            wavePosition += currentSpeed * Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeDelta * Time.deltaTime);
            DestroyRouteDisplay();
        }
    }

    private List<GameObject> teleportEffects = new List<GameObject>();
    public void Teleport() {
        targetDistChangeTimer = Random.Range(4f, 15f);
        var cars = GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < cars.Length; i++) {
            var target = cars[i].transform;
            var effect = Instantiate(LevelReferences.s.teleportStartEffect, target.transform.position,Quaternion.identity);
            effect.transform.SetParent(target);
            teleportEffects.Add(effect);
        }
        
        Invoke(nameof(FinishTeleport), LevelReferences.s.teleportTime);
    }

    void FinishTeleport() {
        for (int i = 0; i < teleportEffects.Count; i++) {
            if (teleportEffects[i] != null) {
                teleportEffects[i].transform.SetParent(null);
            }
        }

        teleportEffects.Clear();

        currentSpeed = LevelReferences.s.speed;
        isLeft = !isLeft;
        SetTargetPosition();
        myXOffset = targetXOffset;
        currentDistanceOffset = targetDistanceOffset;
        wavePosition = SpeedController.s.currentDistance;
        instantLerp = true;
        
        var cars = GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < cars.Length; i++) {
            var target = cars[i].transform;
            Instantiate(LevelReferences.s.teleportCompleteEffect, target.transform.position, Quaternion.identity).transform.SetParent(target);
        }
    }

    private void SetTargetPosition() {
        if(isStealing) {
            SetTargetPositionStealing();
        } else if (isNuker) {
            SetPositionNuking();
        } else {
            var trainLength = Train.s.GetTrainLength();
            var halfLength = (trainLength / 2f) + DataHolder.s.cartLength;
            targetDistanceOffset = Random.Range(-halfLength, halfLength);

            targetXOffset = Random.Range(0.7f+waveSpawnXSpread, 3.2f-waveSpawnXSpread);
            if (!isLeft)
                targetXOffset = -targetXOffset;
        }
    }
    
    
    public bool isNuker = false;

    public float nukingTime = 20;
    public float currentNukingTime = 0;
    
    public Cart nukingTarget;
    void SetPositionNuking() {
        var carts = new List<Cart>();
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            var legalCartType = !cart.GetHealthModule().invincible;
            var hasDirectControl = cart.GetComponentInChildren<DirectControllable>() != null;
            if (!cart.isDestroyed && legalCartType && !cart.isBeingDisabled && !hasDirectControl) {
                carts.Add(cart);
            }
        }


        if (carts.Count == 0) {
            return;
        }
        
        nukingTarget = carts[Random.Range(0, carts.Count)];

        targetDistanceOffset = nukingTarget.transform.position.z + Random.Range(-0.35f, 0.35f); // this works because the Train is at perfectly 0

        targetXOffset = Random.Range(1.9f, 2.5f);
        
        GetComponentInChildren<DisablerHarpoonModule>().SetTarget(nukingTarget); // we are assuming stealers come in packs of 1
        
        if (!isLeft)
            targetXOffset = -targetXOffset;

        currentNukingTime = nukingTime;
        targetDistChangeTimer = 100000;
    }
    

    private Cart lastTarget;
    void SetTargetPositionStealing() {
        var carts = new List<Cart>();
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            var legalCartType = !cart.isCargo && !cart.isMainEngine && !cart.isMysteriousCart;
            var hasDirectControl = cart.GetComponentInChildren<DirectControllable>() != null;
            if (!cart.isDestroyed && legalCartType && !cart.isBeingDisabled && !hasDirectControl) {
                carts.Add(cart);
            }
        }


        if (carts.Count == 0) {
            return;
        }
        
        Cart randomCart = null;
        for (int i = 0; i < 10; i++) {
            randomCart = carts[Random.Range(0, carts.Count)];

            if(lastTarget == null)
                break;
            if (Vector3.Distance(randomCart.transform.position, lastTarget.transform.position) > 0.01f) {
                break;
            }
        }

        if (randomCart == null) {
            return;
        }

        lastTarget = randomCart;

        targetDistanceOffset = randomCart.transform.position.z + Random.Range(-0.5f, 0.5f); // this works because the Train is at perfectly 0

        targetXOffset = Random.Range(0.9f, 1.7f);
        
        GetComponentInChildren<DisablerHarpoonModule>().SetTarget(randomCart); // we are assuming stealers come in packs of 1
        
        if (!isLeft)
            targetXOffset = -targetXOffset;
    }
    void SpawnEnemy(Artifact artifact = null) {
        drawnEnemies = Instantiate(DataHolder.s.GetEnemy(myEnemy.enemyUniqueName), transform).GetComponent<EnemySwarmMaker>();
        drawnEnemies.transform.ResetTransformation();
        waveSpawnXSpread = drawnEnemies.SetData(myEnemy.enemyCount, artifact);

        /*if (hasPowerUp) {
            drawnEnemies.enemyIcon = powerUp.icon;
        }*/
    }

    void DestroyRouteDisplay() {
        if (waveDisplay != null) {
            Destroy(waveDisplay.gameObject);
            //lineRenderer.enabled = false;
        }
    }

    [NonSerialized]
    public UnityEvent OnEnemyEnter = new UnityEvent();
    void PlayEnemyEnterSound() {
        drawnEnemies.PlayEnemyEnterSound();
        OnEnemyEnter?.Invoke();
    }


    const float lineDistance = 2f;
    private const float lineHeight = 0.5f;
    void CreateRouteDisplay() {
        if (waveDisplay == null) {
            waveDisplay = Instantiate(LevelReferences.s.waveDisplayPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_IncomingWave>();
            waveDisplay.SetUp(this);


            var points = new List<Vector3>();
            
            var myPos = transform.position;
            var close = myPos;
            close.z = Mathf.Clamp(close.z, -2, 2);
            var far = myPos;
            far.z = Mathf.Clamp(far.z, -15, 15);

            close.y = 0.5f;
            far.y = 0.5f;
            
            points.Add(far);
            points.Add(close);

            lineRenderer = GetComponentInChildren<LineRenderer>();
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            var enemyType = DataHolder.s.GetEnemy(myEnemy.enemyUniqueName).GetComponent<EnemyTypeData>().myType;
            lineRenderer.material = enemyType == EnemyTypeData.EnemyType.Deadly ? deadlyMaterial : safeMaterial;
            targetAlpha = 0f;
            lineRenderer.material.SetFloat("alpha", targetAlpha);
            lineRenderer.enabled = true;
        }
    }

    private float targetAlpha = 0;
    private float currentAlpha = 0;
    private float currentLerpSpeed = 2f;
    
    [Header("line alpha lerp options")]
    public float activeAlpha = 0.8f;
    public float disabledAlpha = 0.2f;
    public float onLerpSpeed = 2f;
    public float offLerpSpeed = 0.5f;

    public bool isLerp = true;

    public void ShowPath() {
        //lineRenderer.enabled = true;
        targetAlpha = activeAlpha;
        currentLerpSpeed = onLerpSpeed;
    }

    public void HidePath() {
        //lineRenderer.enabled = false;
        targetAlpha = disabledAlpha;
        currentLerpSpeed = offLerpSpeed;
    }


    private float nukingDiveMoveSpeed = 0f;
    private void Update() {
        LerpLineRenderedAlpha();
        if (isNuker) {
            if (_disablerHarpoonModule != null && !isNuking) {
                if (_disablerHarpoonModule.harpoonEngaged) {
                    currentNukingTime -= Time.deltaTime;

                    if (currentNukingTime <= 1f) {
                        EngageNuke();
                    }
                } else {
                    currentNukingTime = nukingTime;
                }

                _disablerHarpoonModule.UpdateColor(1f - currentNukingTime / nukingTime);
            }

            if (isNuking) {
                currentNukingTime -= Time.deltaTime;
                _disablerHarpoonModule.UpdateColor(1f - currentNukingTime / nukingTime);


                transform.position = Vector3.MoveTowards(transform.position, nukingTarget.transform.position, nukingDiveMoveSpeed * Time.deltaTime);
                nukingDiveMoveSpeed += 2f * Time.deltaTime;

                if (Vector3.Distance(transform.position, nukingTarget.transform.position) < 0.4f) {
                    Destroy(gameObject);
                    var carRealPos = GetComponentInChildren<CarLikeMovementOffsetsController>();
                    Instantiate(LevelReferences.s.bigDamagePrefab, carRealPos.transform.position, carRealPos.transform.rotation);
                    nukingTarget.GetHealthModule().Die();
                }
            }
        }
    }

    private bool isNuking = false;
    void EngageNuke() {
        isNuking = true;
        targetDistanceOffset = nukingTarget.transform.position.z;
        targetXOffset = 0;
        movePos = false;
        nukingDiveMoveSpeed = 0;
    }

    void LerpLineRenderedAlpha() {
        if(isLerp)
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, currentLerpSpeed * Time.deltaTime);
        else
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, currentLerpSpeed * Time.deltaTime);
        
        lineRenderer.material.SetFloat("alpha", currentAlpha);
    }

    public float GetDistance() {
        return wavePosition + currentDistanceOffset;
    }

    public Sprite GetIcon() {
        return DataHolder.s.GetEnemy(myEnemy.enemyUniqueName).GetComponent<EnemySwarmMaker>().enemyIcon;
    }

    public bool isLeftUnit() {
        return isLeft;
    }

    public float GetSpeed() {
        return currentSpeed;
    }
    
    
    public float slowAmount;
    public float slowDecay = 0.1f;
    public void AddSlow(float amount) {
        slowAmount += amount;
        ToggleSlowedEffect(true);
    }
    
    public List<GameObject> activeSlowedEffects = new List<GameObject>();
    private bool isSlowedOn = false;
    void ToggleSlowedEffect(bool isOn) {
        if (isOn && !isSlowedOn) {
            var enemies = GetComponentsInChildren<EnemyHealth>();
            for (int i = 0; i < enemies.Length; i++) {
                var effect = Instantiate(LevelReferences.s.currentlySlowedEffect, enemies[i].transform.position, Quaternion.identity);
                effect.transform.SetParent(enemies[i].transform);
                activeSlowedEffects.Add(effect);
            }

            isSlowedOn = true;
        }

        if (!isOn && isSlowedOn) {
            for (int i = 0; i < activeSlowedEffects.Count; i++) {
                SmartDestroy(activeSlowedEffects[i].gameObject);
            }
            
            activeSlowedEffects.Clear();
            isSlowedOn = false;
        }
    }

    void SmartDestroy(GameObject target) {
        var particles = GetComponentsInChildren<ParticleSystem>();

        foreach (var particle in particles) {
            particle.transform.SetParent(null);
            particle.Stop();
            Destroy(particle.gameObject, 1f);
        }
            
        Destroy(target);
    }


    public bool neverLeave = false;

    public void Leave(bool _isForwardLeave) {
        if(neverLeave)
            return;
        
        isLeaving = true;
        isStealing = false;
        isTeleporting = false;
        isWaveMoving = true;
        isForwardLeave = _isForwardLeave;
        
        SetTargetPosition();
        /*if (isForwardLeave) {
            targetDistanceOffset = SpeedController.s.missionDistance;
        } else {
            targetDistanceOffset = -SpeedController.s.missionDistance;
        }*/
    }
}
