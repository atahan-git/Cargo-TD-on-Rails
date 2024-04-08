using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Train : MonoBehaviour {
    public static Train s;

    public Transform trainFront;
    public Transform trainBack;
    public Transform trainMiddle;
    public float trainFrontBackDistanceOffset = 0.199f;
    public float trainFrontBackMiddleYOffset = 0.639f;

    public List<Cart> carts = new List<Cart>();

    public UnityEvent onTrainCartsChanged = new UnityEvent();
    public UnityEvent onTrainCartsOrHealthOrArtifactsChanged = new UnityEvent();

    public bool isTrainDrawn = false;

    public bool isMainTrain = false;

    private void Awake() {
        if (isMainTrain) {
            s = this;
        }
    }

    [Button]
    public void ReDrawCurrentTrain() {
        DrawTrain(GetTrainState());
    }

    public void DrawTrainBasedOnSaveData() {
        DrawTrain(DataSaver.s.GetCurrentSave().myTrain);
    }

    public void DrawTrain(DataSaver.TrainState trainState) {
        StopAllCoroutines();
        suppressRedraw = true;
        transform.DeleteAllChildren();
        suppressRedraw = false;
        
        carts = new List<Cart>();

        if (trainFront != null)
            Destroy(trainFront.gameObject);
        if (trainBack != null)
            Destroy(trainBack.gameObject);
        if (trainMiddle != null) 
            Destroy(trainMiddle.gameObject);
        

        if (trainState != null) {
            for (int i = 0; i < trainState.myCarts.Count; i++) {
                var cartState = trainState.myCarts[i];
                var cart = Instantiate(DataHolder.s.GetCart(cartState.uniqueName).gameObject, transform).GetComponent<Cart>();
                ApplyStateToCart(cart, cartState);
                carts.Add(cart);
            }

            trainFront = new GameObject().transform;
            trainFront.SetParent(transform);
            trainFront.gameObject.name = "Train Front";

            trainBack = new GameObject().transform;
            trainBack.SetParent(transform);
            trainBack.gameObject.name = "Train Back";
            
            
            trainMiddle = new GameObject().transform;
            trainMiddle.SetParent(transform);
            trainMiddle.gameObject.name = "Train Middle";
        }

        UpdateCartPositions(true);
        
        isTrainDrawn = true;
        
        Invoke(nameof(TrainChanged),0.01f);

        onTrainCartsChanged?.Invoke();
    }


    public void OnCartDestroyedOrRevived() {
        onTrainCartsChanged?.Invoke();
    }

    public void SaveTrainState(bool isInstant = false) {
        if (isInstant) {
            OneFrameLater();
            return;
        }
        
        if (!PlayStateMaster.s.isCombatInProgress()) {
            Invoke(nameof(OneFrameLater), 0.01f);
        }
    }

    void OneFrameLater() {
        // because sometimes train doesnt get updated fast enough
        DataSaver.s.GetCurrentSave().myTrain = GetTrainState();
        DataSaver.s.SaveActiveGame();
    }

    public DataSaver.TrainState GetTrainState() {
        var trainState = new DataSaver.TrainState();

        for (int i = 0; i < carts.Count; i++) {
            var cartScript = carts[i].GetComponent<Cart>();
            trainState.myCarts.Add(GetStateFromCart(cartScript));
        }

        return trainState;
    }

    public static void ApplyCartToState(Cart cart, DataSaver.TrainState.CartState buildingState) {
        if (cart != null) {
            buildingState.uniqueName = cart.uniqueName;
            buildingState.health = cart.GetCurrentHealth();
            buildingState.maxHealthReduction = cart.GetCurrentHealthReduction();

            var cargo = cart.GetComponentInChildren<CargoModule>();
            if (cargo != null) {
                buildingState.cargoState = DataSaver.TrainState.CartState.CargoState.GetStateFromModule(cargo);
            } else {
                buildingState.cargoState = null;
            }

            var ammo = cart.GetComponentInChildren<ModuleAmmo>();
            
            if (ammo != null) {
                buildingState.ammo = (int)ammo.curAmmo;
                /*buildingState.isFire = ammo.isFire;
                buildingState.isSticky = ammo.isSticky;
                buildingState.isExplosive = ammo.isExplosive;*/
            } else {
                buildingState.ammo = -1;
            }

            buildingState.attachedArtifacts = new List<DataSaver.TrainState.ArtifactState>();
            for (int i = 0; i < cart.myArtifactLocations.Count; i++) {
                var attachedArtifact = cart.myArtifactLocations[i].GetSnappedObject();
                if (attachedArtifact != null) {
                    var artifactState = new DataSaver.TrainState.ArtifactState();

                    ApplyArtifactToState(attachedArtifact.GetComponent<Artifact>(), artifactState);
                    buildingState.attachedArtifacts.Add(artifactState);
                }
            }

        } else {
            buildingState.EmptyState();
        }
    }

    public static DataSaver.TrainState.CartState GetStateFromCart(Cart cart) {
        var state = new DataSaver.TrainState.CartState();
        ApplyCartToState(cart, state);
        return state;
    }

    public static void ApplyStateToCart(Cart cart, DataSaver.TrainState.CartState cartState) {
        var tier1GunModuleSpawner = cart.GetComponentInChildren<Tier1GunModuleSpawner>();
        if (tier1GunModuleSpawner != null) {
            tier1GunModuleSpawner.SpawnGuns(cartState.uniqueName);
        }
        
        var tier2GunModuleSpawner = cart.GetComponentInChildren<Tier2GunModuleSpawner>();
        if (tier2GunModuleSpawner != null) {
            tier2GunModuleSpawner.SpawnGun(cartState.uniqueName);
        }
        
        
        cart.SetUpOverlays();
        
        if (cartState.health > 0) {
            cart.SetCurrentHealth(cartState.health, cartState.maxHealthReduction);
        }

        /*if (cartState.ammo >= 0) {
            var ammo = cart.GetComponentInChildren<ModuleAmmo>();
            if (ammo != null) 
                ammo.SetAmmo(cartState.ammo);
            
        }/*else if (cartState.ammo == -2) {
            var ammo = cart.GetComponentInChildren<ModuleAmmo>();
            if (ammo != null) {
                ammo.SetAmmo(ammo.maxAmmo);
            } else {
                cartState.ammo = -1;
            }
        }#1#*/


        var cargoModule = cart.GetComponentInChildren<CargoModule>();
        if (cargoModule != null) {
            cargoModule.SetCargo(cartState.cargoState);
        }

        for (int i = 0; i < cartState.attachedArtifacts.Count; i++) {
            var attachedArtifact = cartState.attachedArtifacts[i];
            if (attachedArtifact != null && attachedArtifact.uniqueName != null && attachedArtifact.uniqueName.Length > 0) {
                var artifact = Instantiate( DataHolder.s.GetArtifact(attachedArtifact.uniqueName).gameObject).GetComponent<Artifact>();
                ApplyStateToArtifact(artifact, attachedArtifact);
                artifact.AttachToSnapLoc(cart.myArtifactLocations[i], false,false);
            }
        }

        cart.ResetState();
    }
    
    public static void ApplyArtifactToState(Artifact artifact, DataSaver.TrainState.ArtifactState artifactState) {
        if (artifact != null) {
            artifactState.uniqueName = artifact.uniqueName;
        } else {
            artifactState.EmptyState();
        }
    }
    
    public static DataSaver.TrainState.ArtifactState GetStateFromArtifact(Artifact artifact) {
        var state = new DataSaver.TrainState.ArtifactState();
        ApplyArtifactToState(artifact, state);
        return state;
    }

    public static void ApplyStateToArtifact(Artifact artifact, DataSaver.TrainState.ArtifactState artifactState) {
    }

    public void RightBeforeLeaveMissionRewardArea() {
        StopCoroutine(nameof(LerpTrain));
        StartCoroutine(LerpTrain(Vector3.zero, GetTrainForward()*3, 4f ,false));
        showEntryMovement = true;
    }

    public void ShowEntrySparkles() {
        showEntrySparkles = true;
    }

    public static bool showEntryMovement = false;
    public static bool showEntrySparkles = false;

    public void OnEnterShopArea() {
        StopCoroutine(nameof(LerpTrain));
        if (showEntryMovement) {
            // only show this if the player just left the mission reward area
            StartCoroutine(LerpTrain(-GetTrainForward() * 3, Vector3.zero, 4f, true));
            showEntryMovement = false;
        }

        if (showEntrySparkles) {
            showEntrySparkles = false;
        }
    }

    public bool IsTrainMoving() {
        if (PlayStateMaster.s.isCombatInProgress())
            return true;

        return lerpingTrain;
    }


    private bool lerpingTrain = false;
    IEnumerator LerpTrain(Vector3 startPos, Vector3 endPos, float time, bool stopSparkles) {
        lerpingTrain = true;
        transform.position = startPos;
        var totalTime = time;
        if (stopSparkles) {
            SpeedController.s.currentBreakPower = 10;
        }
        
        while (time >= 0f) {
            transform.position = Vector3.Lerp(endPos, startPos, Mathf.Pow(time/totalTime, 2));

            time -= Time.deltaTime;
            yield return null;
        }
        
        if (stopSparkles) {
            SpeedController.s.currentBreakPower = 0;
        }

        transform.position = endPos;
        lerpingTrain = false;
    }

    public Vector3 GetTrainForward() {
        var index = Mathf.RoundToInt(carts.Count / 2f);
        index = Mathf.Clamp(index, 0, carts.Count-1);
        return carts[index].transform.forward;
    }

    public float GetTrainLength() {
        var totalLength = 0f;
        for (int i = 0; i < carts.Count; i++) {
            totalLength += carts[i].length;
        }

        return totalLength;
    }


    float lerpSpeed => PlayerWorldInteractionController.lerpSpeed;
    float slerpSpeed => PlayerWorldInteractionController.slerpSpeed;
    public bool newspaperMode = false;
    public void UpdateCartPositions(bool instant = false) {
        if(carts.Count == 0)
            return;

        if (newspaperMode) {
            UpdateCartPositionsNewspaper();
            return;
        }

        var totalLength = GetTrainLength();

        var currentDistance = (totalLength / 2f);

        var specialTreatmentForSelectedCart = PlayStateMaster.s.isShopOrEndGame();
        var selectedCart = PlayerWorldInteractionController.s.currentSelectedThing as Cart;

        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            var currentSpot = PathAndTerrainGenerator.s.GetPointOnActivePath(currentDistance);
            var currentRot = PathAndTerrainGenerator.s.GetRotationOnActivePath(currentDistance);
            if (specialTreatmentForSelectedCart && cart == selectedCart && !cart.isMainEngine )
                currentSpot += Vector3.up * (PlayerWorldInteractionController.s.isDragging() ? 0.4f : 0.05f);

            var cartTransform = cart.transform;
            if (!instant) {
                cartTransform.position = Vector3.Lerp(cartTransform.position, currentSpot, lerpSpeed * Time.deltaTime);
                cartTransform.rotation = Quaternion.Slerp(cartTransform.rotation, currentRot, slerpSpeed * Time.deltaTime);
            } else {
                cartTransform.position = currentSpot;
                cartTransform.rotation = currentRot;
            }

            currentDistance += -cart.length;
            var index = i;
            cart.name = $"Cart {index }";
            cart.trainIndex = index;
            cart.cartPosOffset = currentDistance;

            if (instant) {
                for (int j = 0; j < cart.myArtifactLocations.Count; j++) {
                    if(!cart.myArtifactLocations[j].IsEmpty())
                        cart.myArtifactLocations[j].GetSnappedObject().ResetTransformation();
                }
            }
        }


        var upOffset = Vector3.up * trainFrontBackMiddleYOffset;
        
        var frontDist = (totalLength / 2f) + trainFrontBackDistanceOffset;
        trainFront.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(frontDist) + upOffset;
        trainFront.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(frontDist);
        
        trainBack.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(-frontDist) + upOffset;
        trainBack.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(-frontDist);

        trainMiddle.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
        trainMiddle.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(0);
        
        
        DoShake();
    }


    public void UpdateCartPositionsNewspaper() {
        var trainMaxLength = 10f;

        var trainCurrentLength = GetTrainLength();

        var distanceAddon = trainCurrentLength/trainMaxLength;
        distanceAddon = Mathf.Clamp(distanceAddon, 1, 10);
        distanceAddon -= 1;
        distanceAddon *= 4;
        
        var totalLength = GetTrainLength();
        var currentDistance = 1f - ((carts[0].length/2f)/totalLength);
        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            var currentSpot = NewspaperController.s.GetNewspaperTrainPosition(currentDistance, distanceAddon);
            var currentRot = NewspaperController.s.GetNewspaperTrainRotation(currentDistance, distanceAddon);

            var cartTransform = cart.transform;
            cartTransform.rotation = Quaternion.Slerp(cartTransform.rotation, currentRot, slerpSpeed * Time.deltaTime * 0.2f);
            cartTransform.position = Vector3.Lerp(cartTransform.position, currentSpot + (cartTransform.up*-0.6f), lerpSpeed * Time.deltaTime * 0.2f);

            var index = i;
            /*cart.name = $"Cart {index }";
            cart.trainIndex = index;
            cart.cartPosOffset = currentDistance;*/
            currentDistance -= cart.length/totalLength;
        }
    }
    

    private bool suppressRedraw = false;
    public void CartDestroyed(Cart cart) {
        if(suppressRedraw)
            return;

        var index = carts.IndexOf(cart);

        if (index > -1) {
            RemoveCart(cart);
        } 

        CheckHealth();
        
        // draw train already calls this
        //trainUpdatedThroughNonBuildingActions?.Invoke();
    }

    void CheckHealth() {
        var health = 0f;
        for (int i = 0; i < carts.Count; i++) {
            var _cart = carts[i].GetHealthModule();
            if (!_cart.invincible) {
                health += _cart.currentHealth;
            }
        }

        //print(health);
        if (health <= 0) {
            MissionLoseFinisher.s.MissionLost(MissionLoseFinisher.MissionLoseReason.everyCartExploded);
        }
    }


    private void Update() {
        UpdateCartPositions();
    }

    public void UpdateThingsAffectingOtherThings(bool isActivating) {
        if (isActivating) {
            SetArtifactStatus(true);

            SpeedController.s.CalculateSpeedBasedOnCartCapacity();

            for (int i = 0; i < carts.Count; i++) {
                carts[i].GetHealthModule().UpdateHpState();
            }
            
            GetComponent<AmmoTracker>().RegisterAmmoProviders();

            MaxHealthModified();
        } else {
            SetArtifactStatus(false);
            
            for (int i = 0; i < carts.Count; i++) {
                carts[i].ResetState();
            }
            
            for (int i = 0; i < ShopStateController.s.shopCarts.Count; i++) {
                if(ShopStateController.s.shopCarts[i] != null && ShopStateController.s.shopCarts[i].gameObject != null)
                    ShopStateController.s.shopCarts[i].ResetState();
            }
            for (int i = 0; i < ShopStateController.s.shopArtifacts.Count; i++) {
                if(ShopStateController.s.shopArtifacts[i] != null && ShopStateController.s.shopArtifacts[i].gameObject != null)
                    ShopStateController.s.shopArtifacts[i].ResetState();
            }
            
            
            PlayerWorldInteractionController.s.ResetValues();
            SpeedController.s.ResetMultipliers();
            SpeedController.s.CalculateSpeedBasedOnCartCapacity();
        }
    }

    void SetArtifactStatus(bool isArm) {
        if (this != Train.s) {
            return;
        }
        var artifacts = new List<Artifact>();

        for (int i = 0; i < Train.s.carts.Count; i++) {
            var artifact = Train.s.carts[i].GetComponentInChildren<Artifact>();
            if(artifact != null)
                artifacts.Add(artifact);
        }
        for (int i = 0; i < artifacts.Count; i++) {
            var effects = artifacts[i].GetComponentsInChildren<ActivateWhenOnArtifactRow>();
            if (effects != null) {
                for (int j = 0; j < effects.Length; j++) {
                    if (effects[j].GetComponentInParent<Cart>() != null) {
                        if (isArm && !effects[j].GetComponentInParent<Cart>().isDestroyed) {
                            effects[j].Arm();
                        } else {
                            effects[j].Disarm();
                        }
                    }
                }
            }
        }
    }

    public void RemoveCart(Cart cart) {
        UpdateThingsAffectingOtherThings(false);
        
        carts.Remove(cart);
        cart.transform.SetParent(null);
        
        for (int i = 0; i < carts.Count; i++) {
            carts[i].trainIndex = i;
        }

        UpdateThingsAffectingOtherThings(true);
        
        onTrainCartsChanged?.Invoke();
    }

    public void AddCartAtIndex(int index, Cart cart) {
        var existingIndex = carts.IndexOf(cart);
        if (existingIndex != -1) {
            RemoveCart(cart);
        }
        
        UpdateThingsAffectingOtherThings(false);
        
        carts.Insert(index, cart);
        cart.transform.SetParent(transform);

        for (int i = 0; i < carts.Count; i++) {
            carts[i].trainIndex = i;
        }
        
        UpdateThingsAffectingOtherThings(true);
        
        onTrainCartsChanged?.Invoke();
    }


    public void CheckMerge() {
        for (int i = 0; i < carts.Count-1; i++) {
            var prevCart = carts[i];
            var nextCart = carts[i + 1];
            
            var mergeResultUniqueName = DataHolder.s.GetMergeResult(prevCart.uniqueName, nextCart.uniqueName);
            var legalMerge = mergeResultUniqueName != null;
            if (legalMerge) {
                var prevCartArtifacts = prevCart.GetComponentsInChildren<Artifact>();
                var nextCartArtifacts = nextCart.GetComponentsInChildren<Artifact>();
                
                RemoveCart(prevCart);
                RemoveCart(nextCart);
                var spawnPos = Vector3.Lerp(prevCart.transform.position, nextCart.transform.position, 0.5f);
                var spawnRot = prevCart.transform.rotation;
                var newMergedCart = Instantiate(
                        DataHolder.s.GetCart(mergeResultUniqueName), 
                        spawnPos, spawnRot)
                    .GetComponent<Cart>();

                VisualEffectsController.s.SmartInstantiate(LevelReferences.s.cartMergeEffect, spawnPos, spawnRot, VisualEffectsController.EffectPriority.Always);
                ApplyStateToCart(newMergedCart, new DataSaver.TrainState.CartState(){uniqueName = mergeResultUniqueName});
                AddCartAtIndex(prevCart.trainIndex, newMergedCart);

                var n = 0;
                for (int j = 0; j < prevCartArtifacts.Length; j++) {
                    if (n < newMergedCart.myArtifactLocations.Count) {
                        prevCartArtifacts[j].AttachToSnapLoc(newMergedCart.myArtifactLocations[n]);
                    } else {
                        prevCartArtifacts[j].DetachFromCart();
                    }
                    n++;
                }

                for (int j = 0; j < nextCartArtifacts.Length; j++) {
                    if (n < newMergedCart.myArtifactLocations.Count) {
                        nextCartArtifacts[j].AttachToSnapLoc(newMergedCart.myArtifactLocations[n]);
                    } else {
                        nextCartArtifacts[j].DetachFromCart();
                    }

                    n++;
                }
                
                
                Destroy(prevCart.gameObject);
                Destroy(nextCart.gameObject);
            }
        }
    }

    public void TrainChanged() {
        if (isTrainDrawn) {
            ArtifactsController.s.ArtifactsChanged();
            UpdateThingsAffectingOtherThings(false);
            UpdateThingsAffectingOtherThings(true);
            CheckHealth();
            onTrainCartsOrHealthOrArtifactsChanged?.Invoke();
        }
    }

    public void SwapCarts(Cart cart1, Cart cart2) {
        var cart1Index = carts.IndexOf(cart1);
        var cart2Index = carts.IndexOf(cart2);

        if (cart1Index > cart2Index) {
            SwapCarts(cart2, cart1);
            return;
        }
        
        UpdateThingsAffectingOtherThings(false);
        
        RemoveCart(cart1);
        RemoveCart(cart2);

        (cart1.transform.position, cart2.transform.position) = (cart2.transform.position, cart1.transform.position);
        
        AddCartAtIndex(cart1Index, cart2);
        AddCartAtIndex(cart2Index, cart1);
        
        UpdateThingsAffectingOtherThings(true);
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

                    currentDistance += -myCart.length;
                }
            }
        }
    }

    public void MaxHealthModified() {
        MiniGUI_TrainOverallHealthBar.s.MaxHealthChanged();
        HealthModified();
    }
    
    public void HealthModified() {
        MiniGUI_TrainOverallHealthBar.s.HealthChanged();
        onTrainCartsOrHealthOrArtifactsChanged?.Invoke();
    }

    public Cart GetNextBuilding(int amount, Cart cart) {
        var nextCart = cart.trainIndex - amount;
        if (nextCart >= 0 && nextCart < carts.Count) {
            return carts[nextCart];
        } else {
            return null;
        }
    }
    
    
    

    public DataSaver.TrainState minimumTrain;
    public List<DataSaver.TrainState.CartState> minimumTrainLastCarts = new List<DataSaver.TrainState.CartState>();
    public void CheckSetMinimumTrain() {
        var saveData = DataSaver.s.GetCurrentSave();

        if (saveData.myTrain == null || saveData.myTrain.myCarts.Count <= 3) {
            saveData.myTrain = minimumTrain;
            saveData.myTrain.myCarts.Insert(1, minimumTrainLastCarts[Random.Range(0, minimumTrainLastCarts.Count)]);
        }
    }


    public void SetNewspaperTrainState(bool isNewspaperTrain) {
        newspaperMode = isNewspaperTrain;
    }
}
