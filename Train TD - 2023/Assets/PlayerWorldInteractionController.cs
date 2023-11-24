using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerWorldInteractionController : MonoBehaviour { 
    public static PlayerWorldInteractionController s;
    private void Awake() {
        s = this;
    }

    private void OnDestroy() {
        s = null;
    }


    public IPlayerHoldable selectedThing;
    public EnemyHealth selectedEnemy;
    public Artifact selectedArtifact;
    public Meeple selectedMeeple;
    
    public Cart selectedCart;
    public Vector3 dragBasePos;

    
    /* In Mouse:
     *  click : repair and shields
     *  Alt click: reload or active skills
     *  Hold click : move cart around
     *  Hold alt click : show details
     */
    // in gamepad all those also holds but we also have these few alt actions.
    public InputActionReference clickCart;
    public InputActionReference alternateClick; 
    public InputActionReference dragClick; // drag click is its own button on gamepad
    public InputActionReference showDetailClick; // detail click is its own button on gamepad
    

    [SerializeField]
    private bool _canSelect;
    public bool canSelect {
        get {
            return _canSelect;
        }
        set {
            SetCannotSelect(value);
        }
}

    public bool canOnlySelectCharSelectStuff;

    public bool canRepair = true;
    public bool autoRepairAtStation = true;
    public bool canReload = true;
    public bool autoReloadAtStation = true;
    public bool canSmith = true;

    public bool engineBoostDamageInstead = false;
    
    public void ResetValues() {
        repairAmountMultiplier = 1;
        reloadAmountMultiplier = 1;
        reloadAmountPerClickBoost = 0;
        shieldUpAmountMultiplier = 1;
        canRepair = true;
        canReload = true;
        canSmith = true;
        autoRepairAtStation = true;
        autoReloadAtStation = true;
        engineBoostDamageInstead = false;
    }
    
    protected void OnEnable()
    {
        clickCart.action.Enable();
        alternateClick.action.Enable();
        dragClick.action.Enable();
        showDetailClick.action.Enable();
    }

    

    protected void OnDisable()
    {
        clickCart.action.Disable();
        alternateClick.action.Disable();
        dragClick.action.Disable();
        showDetailClick.action.Disable();
    }

    public Color cantActColor = Color.white;
    public Color moveColor = Color.blue;
    public Color repairColor = Color.green;
    public Color shieldColor = Color.cyan;
    public Color reloadColor = Color.yellow;
    public Color directControlColor = Color.magenta;
    public Color engineBoostColor = Color.red;


    public float maxRepairCapacity = 1000;
    public float repairRecharge = 10;
    public float repairRechargeDelay = 1;
    public float curRepairRechargeDelay = 0;
    public float curRepairCapacity;
    private void Update() {
        if (curRepairRechargeDelay <= 0) {
            if (curRepairCapacity < maxRepairCapacity) {
                curRepairCapacity += repairRecharge*Time.deltaTime;
                curRepairCapacity = Mathf.Clamp(curRepairCapacity, 0, maxRepairCapacity);
            }
        } else {
            curRepairRechargeDelay -= Time.deltaTime;
        }



        if (!canSelect || Pauser.s.isPaused || PlayStateMaster.s.isLoading || DirectControlMaster.s.directControlLock > 0) {
            return;
        }

        if (infoCardActive) {
            if (showDetailClick.action.WasPerformedThisFrame() || clickCart.action.WasPerformedThisFrame()) {
                HideInfo();
            }
        }

        if (!isDragging() && !infoCardActive) {
            if(PlayStateMaster.s.isShopOrEndGame())
                CastRayToOutlineArtifact();
            
            if(selectedArtifact == null)
                CastRayToOutlineCart();
            
            if(PlayStateMaster.s.isCombatInProgress())
                CastRayToOutlineEnemy();
            
            if(selectedArtifact == null && selectedCart == null)
                CastRayToOutlineMeeple();
        }


        if (PlayStateMaster.s.isCombatInProgress()) {
            CheckAndDoClick();
            //CheckAndDoDragCombat();
        } else {
            if (!canOnlySelectCharSelectStuff) {
                CheckAndDoDrag();
            }

            CheckGate();
        }
    }

    public void SetCannotSelect(bool __canSelect) {
        _canSelect = __canSelect;
        if (!_canSelect) {
            Deselect();
        }
    }

    public void OnEnterCombat() {
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.clickGate);
    }

    public void OnLeaveCombat(bool realCombat) {
        Deselect();
    }

    public void OnEnterShopScreen() {
        Deselect();
    }


    Vector2 GetMousePos() {
        return Mouse.current.position.ReadValue();
    }

    private IClickableWorldItem lastGate;
    private bool clickedOnGate;
    void CheckGate() {
        RaycastHit hit;
        Ray ray = GetRay();
        if (Physics.Raycast(ray, out hit, 100f, LevelReferences.s.gateMask)) {
            var gate = hit.collider.GetComponentInParent<IClickableWorldItem>();

            if (gate != lastGate) {
                if (lastGate != null) {
                    lastGate._OnMouseExit();
                    OnSelectGate?.Invoke(lastGate, false);
                }
                
                lastGate = gate;
                lastGate._OnMouseEnter();
                OnSelectGate?.Invoke(lastGate, true);
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.clickGate);
            }

            if (clickCart.action.WasPressedThisFrame()) {
                HideInfo();
                clickedOnGate = true;
            }

            if (clickedOnGate && (clickCart.action.WasReleasedThisFrame())) {
                lastGate._OnMouseUpAsButton();
            }
            
        } else {
            if (lastGate != null) {
                lastGate._OnMouseExit();
                OnSelectGate?.Invoke(lastGate, false);
                GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.clickGate);
                
                lastGate = null;
                
                clickedOnGate = false;
            }
        }
    }


    void CheckAndDoDragCombat() {
        if (selectedCart != null) {
            DoCartDragCombat();
        }

        if (doCombatTrainLerp) {
            UpdateTrainCartPositionsSlowlyCombat();
        }
    }

    private bool doCombatTrainLerp = false;
    void UpdateTrainCartPositionsSlowlyCombat() {
        var carts = Train.s.carts;
        
        if(carts.Count == 0)
            return;
        
        var totalLength = 0f;
        for (int i = 0; i < carts.Count; i++) {
            totalLength += carts[i].length;
        }
        
        var currentSpot = - Vector3.back * (totalLength / 2f);

        //Debug.Log("combat lerping");

        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            if (cart == selectedCart && !cart.isMainEngine )
                currentSpot += Vector3.up * (isDragging() ? 0.4f : 0.05f);

            cart.transform.localPosition = Vector3.Lerp(cart.transform.localPosition, currentSpot, lerpSpeed * Time.deltaTime);
            cart.transform.localRotation = Quaternion.Slerp(cart.transform.localRotation, Quaternion.identity, slerpSpeed * Time.deltaTime);

            if (cart == selectedCart && !cart.isMainEngine)
                currentSpot -= Vector3.up * (isDragging() ? 0.4f : 0.05f);


            currentSpot += -Vector3.forward * cart.length;
            var index = i;
            cart.name = $"Cart {index}";
        }
    }

    public bool isDragging() {
        return isToggleDragStarted || isHoldDragStarted;
    }

    private float clickCartTime = 0;
    private Vector3 clickCartPos;
    private bool clickCartFired;
    private void DoCartDragCombat() {
        if (!isToggleDragStarted) {
            if (DragStarted(clickCart, ref clickCartTime, ref clickCartPos, ref clickCartFired)) {
                HideInfo();
                if (CanDragCart(selectedCart)) {
                    isHoldDragStarted = true;
                    BeginCartDragCombat();
                }
            }

            if (isHoldDragStarted) {
                if (clickCart.action.IsPressed()) {
                    CheckIfCartSnappingCombat();
                } else {
                    EndCartDragCombat();
                }
            }
        }
        
        if (!isHoldDragStarted) {
            if (!isToggleDragStarted) {
                if (dragClick.action.WasPerformedThisFrame() && !isHoldDragStarted) {
                    HideInfo();
                    if (CanDragCart(selectedCart)) {
                        isToggleDragStarted = true;
                        BeginCartDragCombat();
                    }
                }
            }else{
                CheckIfCartSnappingCombat();
                if (dragClick.action.WasPerformedThisFrame()) {
                    EndCartDragCombat();
                }
            }
        }
    }


    /// <summary>
    /// This should be called from an update function every frame.
    /// </summary>
    bool DragStarted(InputActionReference actionReference, ref float clickTime, ref Vector3 clickPos, ref bool fired) {
        if (actionReference.action.WasPressedThisFrame()) {
            clickTime = 0.2f;
            clickPos = GetMousePositionOnPlane();
        }

        if (actionReference.action.IsPressed()) {
            clickTime -= Time.deltaTime;
            var clickTimePassed = clickTime <= 0;
            var movedEnough = (clickPos - GetMousePositionOnPlane()).magnitude > 0.1f;

            if ((clickTimePassed || movedEnough) && !fired) {
                fired = true;
                return true;
            }
        } else {
            fired = false;
        }
        return false;
    }
    
    private void BeginCartDragCombat() {
        if (selectedCart.myLocation == UpgradesController.CartLocation.train) {
            isSnapping = false;
            
            dragBasePos = selectedCart.transform.position;
            offset = dragBasePos - GetMousePositionOnPlane();

            Train.s.StopShake(10000000, false);
            doCombatTrainLerp = true;
            CancelInvoke(nameof(StopCombatTrainLerp));

            
            PhysicalRangeShower.s.ShowCartRange(selectedCart,true);
            SelectBuilding(selectedCart, true, true, true);
            // SFX
            AudioManager.PlayOneShot(SfxTypes.OnCargoPickUp);
        }
    }
    
    
    void CheckIfCartSnappingCombat() {
        if (!SettingsController.GamepadMode()) {
            SnapToTrainCombat();
        }
    }

    public void MoveSelectedCart(bool isForward) {
        if (PlayStateMaster.s.isCombatInProgress() && isDragging()) {
            if (selectedCart != null) {
                if (CanDragCart(selectedCart)) {
                    if (isForward) {
                        if (selectedCart.trainIndex > 1) {
                            Train.s.RemoveCart(selectedCart);
                            Train.s.AddCartAtIndex(selectedCart.trainIndex - 1, selectedCart);
                        }
                    } else {
                        Train.s.RemoveCart(selectedCart);
                        Train.s.AddCartAtIndex(selectedCart.trainIndex + 1, selectedCart);
                    }
                }
            }
        }
    }
    private bool SnapToTrainCombat() {
        var carts = Train.s.carts;
        var zPos = GetMousePositionOnPlane().z;

        var totalLength = 0f;
        for (int i = 0; i < carts.Count; i++) {
            totalLength += carts[i].length;
        }

        var currentSpot = Vector3.forward * (totalLength / 2f);

        bool inserted = false;
        float distance = 0;

        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            currentSpot += Vector3.back * cart.length;

            if (i != 0) {
                distance = Mathf.Abs((currentSpot.z + (cart.length / 2f)) - zPos);
                //Debug.Log($"{currentSpot.z + (cart.length / 2f)} < {zPos} && {distance} < {cart.length*4}");
                if (currentSpot.z + (cart.length / 2f) < zPos && distance < cart.length*4) {
                    if (cart != selectedCart) {
                        inserted = true;
                        Debug.Log($"Changing index {i}");
                        Train.s.RemoveCart(selectedCart);
                        Train.s.AddCartAtIndex(i, selectedCart);
                        
                        PhysicalRangeShower.s.ShowCartRange(selectedCart,false);
                        
                    } else {
                        inserted = true;
                    }

                    if(inserted)
                        return true;
                }
            }
        }

        if (!inserted) {
            if (carts[carts.Count - 1] != selectedCart && distance < carts[0].length*4) {
                Train.s.RemoveCart(selectedCart);
                Train.s.AddCartAtIndex(carts.Count, selectedCart);
                
                PhysicalRangeShower.s.ShowCartRange(selectedCart, false);
            }
        }
        return true;
    }
    
    private void EndCartDragCombat() {
        isHoldDragStarted = false;
        isToggleDragStarted = false;
        
        
        PhysicalRangeShower.s.HideRange();
        
        SelectBuilding(selectedCart, true);
        Train.s.RestartShake(2.1f, true);
        Invoke(nameof(StopCombatTrainLerp),2);
    }

    void StopCombatTrainLerp() {
        doCombatTrainLerp = false;
    }

    bool CanDragArtifact(Artifact artifact) {
        return !canOnlySelectCharSelectStuff; //we might have artifacts later that are permanently glued to a cart.
    }
    bool CanDragCart(Cart cart) {
        return (!cart.isMainEngine && cart.canPlayerDrag) && !canOnlySelectCharSelectStuff;
    }

    public bool isToggleDragStarted = false;
    public bool isHoldDragStarted = false;
    public bool isSnapping = false;
    public Vector3 offset;
    public SnapCartLocation sourceSnapLocation;
    void CheckAndDoDrag() {
        if (selectedCart != null) {
            DoCartDrag();
        }else if (selectedArtifact != null) {
            DoArtifactDrag();
        }else if (selectedMeeple != null) {
            DoMeepleDrag();
        }
        
        UpdateTrainCartPositionsSlowly();
    }
    
    private void DoMeepleDrag() {
        if (!isToggleDragStarted) {
            if (clickCart.action.WasPressedThisFrame()) {
                HideInfo();
                    isHoldDragStarted = true;
                    BeginMeepleDrag();
            }

            if (isHoldDragStarted) {
                if (clickCart.action.IsPressed()) {
                    CheckIfMeepleSnapping();
                    if (!isSnapping) {
                        selectedMeeple.transform.position = GetMousePositionOnPlane() + offset;
                        selectedMeeple.transform.rotation = Quaternion.Slerp(selectedMeeple.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
                        offset = Vector3.Lerp(offset, Vector3.up/2f, lerpSpeed * Time.deltaTime);
                    }
                } else {
                    EndMeepleDrag();
                }
            }
        }
        
        if (!isHoldDragStarted) {
            if (!isToggleDragStarted) {
                if (dragClick.action.WasPerformedThisFrame()) {
                    HideInfo();
                        isToggleDragStarted = true;
                        BeginMeepleDrag();
                }
            }else{
                CheckIfMeepleSnapping();
                if (!isSnapping) {
                    selectedMeeple.transform.position = GetMousePositionOnPlane() + offset;
                    selectedMeeple.transform.rotation = Quaternion.Slerp(selectedMeeple.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
                    offset = Vector3.Lerp(offset, Vector3.zero, lerpSpeed * Time.deltaTime);
                }
                
                if (dragClick.action.WasPerformedThisFrame()) {
                    EndMeepleDrag();
                } 
            }
        }
    }

    private void BeginMeepleDrag() {
        isSnapping = false;
        selectedMeeple.SetHandlingState(true);
        selectedMeeple.GetClicked();

        dragBasePos = selectedMeeple.transform.position;
        offset = dragBasePos - GetMousePositionOnPlane();
    }

    void CheckIfMeepleSnapping() {
        // meeples cannot snap ever
    }

    
    
    private void EndMeepleDrag() {
        isHoldDragStarted = false;
        selectedMeeple.SetHandlingState(false);
    }

    private void DoArtifactDrag() {
        if (!isToggleDragStarted) {
            if (clickCart.action.WasPressedThisFrame()) {
                HideInfo();
                if (CanDragArtifact(selectedArtifact)) {
                    isHoldDragStarted = true;
                    BeginArtifactDrag();
                }
            }

            if (isHoldDragStarted) {
                if (clickCart.action.IsPressed()) {
                    CheckIfArtifactSnapping();
                    if (!isSnapping) {
                        selectedArtifact.transform.position = GetMousePositionOnPlane() + offset;
                        selectedArtifact.transform.rotation = Quaternion.Slerp(selectedArtifact.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
                        offset = Vector3.Lerp(offset, Vector3.up/2f, lerpSpeed * Time.deltaTime);
                    }
                } else {
                    EndArtifactDrag();
                }
            }
        }
        
        if (!isHoldDragStarted) {
            if (!isToggleDragStarted) {
                if (dragClick.action.WasPerformedThisFrame()) {
                    HideInfo();
                    if (CanDragArtifact(selectedArtifact)) {
                        isToggleDragStarted = true;
                        BeginArtifactDrag();
                    }
                }
            }else{
                CheckIfArtifactSnapping();
                if (!isSnapping) {
                    selectedArtifact.transform.position = GetMousePositionOnPlane() + offset;
                    selectedArtifact.transform.rotation = Quaternion.Slerp(selectedArtifact.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
                    offset = Vector3.Lerp(offset, Vector3.zero, lerpSpeed * Time.deltaTime);
                }
                
                if (dragClick.action.WasPerformedThisFrame()) {
                    EndArtifactDrag();
                } 
            }
        }
    }

    public Cart sourceSnapCart;
    private void BeginArtifactDrag() {
        isSnapping = false;
        sourceSnapCart = selectedArtifact.GetComponentInParent<Cart>();
        selectedArtifact.DetachFromCart();
        selectedArtifact.GetComponent<Rigidbody>().isKinematic = true;
        selectedArtifact.GetComponent<Rigidbody>().useGravity = false;

        dragBasePos = selectedArtifact.transform.position;
        offset = dragBasePos - GetMousePositionOnPlane();
        
        
        PhysicalRangeShower.s.ShowArtifactRange(selectedArtifact,true);
        
        
        swapArtifact = null;
        swapCart = null;
        
        if (ArtifactsController.s.myArtifacts.Contains(selectedArtifact)) {
            ArtifactsController.s.ArtifactsChanged();
        } 

        /*if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }*/
    }

    bool CanAttachToCart(Cart cart, Artifact artifact) {
        return !artifact.isComponent || cart.canAcceptComponentArtifact;
    }
    
    public Artifact swapArtifact;
    public Cart swapCart;
    void CheckIfArtifactSnapping() {
        var carts = Train.s.carts;
        var wasAttachedBefore = selectedArtifact.isAttached;    
        if (Mathf.Abs(GetMousePositionOnPlane().x) < 0.3f) { // snap to stuff on the cart
            isSnapping = false;

            var zPos = GetMousePositionOnPlane().z;

            var totalLength = 0f;
            for (int i = 0; i < carts.Count; i++) {
                totalLength += carts[i].length;
            }

            var currentSpot = transform.localPosition - Vector3.back * (totalLength / 2f);

            for (int i = 0; i < carts.Count; i++) {
                var cart = carts[i];
                currentSpot += -Vector3.forward * cart.length;

                var distance = Mathf.Abs((currentSpot.z + (cart.length / 2f)) - zPos);
                if (currentSpot.z + (cart.length / 2f) < zPos && distance < cart.length * 4) {
                    if (cart.myAttachedArtifact != selectedArtifact) {
                        if (swapArtifact != null) {
                            swapArtifact.AttachToCart(swapCart);
                            swapCart = null;
                            swapArtifact = null;
                        }

                        if (cart.myAttachedArtifact == null && CanAttachToCart(cart, selectedArtifact)) {
                            selectedArtifact.AttachToCart(cart);
                            PhysicalRangeShower.s.ShowArtifactRange(selectedArtifact, false);
                            isSnapping = true;

                        } else {
                            var canBeSwapped = CanDragArtifact(cart.myAttachedArtifact) && CanAttachToCart(cart, selectedArtifact);

                            if (canBeSwapped) {
                                if (sourceSnapCart != null) {
                                    swapArtifact = cart.myAttachedArtifact;
                                    if (swapArtifact != null) {
                                        swapCart = cart;
                                        swapArtifact.AttachToCart(sourceSnapCart);
                                    }
                                } else {
                                    swapArtifact = cart.myAttachedArtifact;
                                    if (swapArtifact != null) {
                                        swapCart = cart;
                                        swapArtifact.DetachFromCart();
                                        swapArtifact.transform.position += Vector3.up / 2f;
                                        //swapArtifact.transform.position = dragBasePos;
                                    }
                                }

                                selectedArtifact.AttachToCart(cart);
                                PhysicalRangeShower.s.ShowArtifactRange(selectedArtifact, false);
                                isSnapping = true;
                            }
                        }

                        if (isSnapping) {
                            AudioManager.PlayOneShot(SfxTypes.OnCargoDrop2);
                        }
                    } else {
                        isSnapping = true;
                    }

                    if (isSnapping) {
                        if (PlayStateMaster.s.isShop()) {
                            UpgradesController.s.UpdateCartShopHighlights();
                        } else {
                            UpgradesController.s.UpdateCargoHighlights();
                        }

                        return;
                    }
                }
            }
        } 
        
        // snap to snap locations
        
        RaycastHit hit;
        Ray ray = GetRay();
        if (Physics.Raycast(ray, out hit, 100f, LevelReferences.s.cartSnapLocationsLayer)) {
            var snapLocation = hit.collider.gameObject.GetComponentInParent<SnapCartLocation>();

            var snapLocationValidAndNew = snapLocation != null && snapLocation != currentSnapLoc;
            var snapLocationCanAcceptCart = !snapLocation.onlySnapCargo  && !snapLocation.onlySnapMysteriousCargo;
            var snapLocationEmpty = snapLocation.snapTransform.childCount == 0;
            var canSnap = snapLocationValidAndNew && snapLocationCanAcceptCart && snapLocationEmpty && !snapLocation.snapNothing;

            if (canSnap) {
                isSnapping = true;
                selectedArtifact.AttachToSnapLoc(snapLocation);
                currentSnapLoc = snapLocation;
                print("snapping to location");
            } else {
                //print("cant snap to location");
                isSnapping = currentSnapLoc != null;
            }
        } else {
            isSnapping = false;
            if (currentSnapLoc != null) {
                print("stopped snapping");
                currentSnapLoc = null;
            }
        }
        

        if (!isSnapping) {
            PhysicalRangeShower.s.HideRange();
            selectedArtifact.DetachFromCart();
            selectedArtifact.GetComponent<Rigidbody>().isKinematic = true;
            selectedArtifact.GetComponent<Rigidbody>().useGravity = false;
        }

        // SFX
        if (wasAttachedBefore != selectedArtifact.isAttached) {
            if (!selectedArtifact.isAttached)
                AudioManager.PlayOneShot(SfxTypes.OnCargoDrop);
        }
        
        if (swapArtifact != null) {
            swapArtifact.AttachToCart(swapCart);
            swapCart = null;
            swapArtifact = null;
        }
        
        
        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }
    }

    
    
    private void EndArtifactDrag() {
        isHoldDragStarted = false;
        if (!isSnapping) {
            selectedArtifact.GetComponent<Rigidbody>().isKinematic = false;
            selectedArtifact.GetComponent<Rigidbody>().useGravity = true;
        }
        

        /*if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }*/

        PhysicalRangeShower.s.HideRange();

        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }


        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.SaveCartStateWithDelay();
            Train.s.SaveTrainState();
        }
    }

    private void DoCartDrag() {
        if (!isToggleDragStarted) {
            if (clickCart.action.WasPressedThisFrame()) {
                HideInfo();
                if (CanDragCart(selectedCart)) {
                    isHoldDragStarted = true;
                    BeginCartDrag();
                }
            }

            if (isHoldDragStarted) {
                if (clickCart.action.IsPressed()) {
                    CheckIfCartSnapping();
                    if (!isSnapping) {
                        selectedCart.transform.position = GetMousePositionOnPlane() + offset;
                        selectedCart.transform.rotation = Quaternion.Slerp(selectedCart.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
                        offset = Vector3.Lerp(offset, Vector3.zero, lerpSpeed * Time.deltaTime);
                    }
                } else {
                    EndCartDrag();
                }
            }
        }
        
        if (!isHoldDragStarted) {
            if (!isToggleDragStarted) {
                if (dragClick.action.WasPerformedThisFrame()) {
                    HideInfo();
                    if (CanDragCart(selectedCart)) {
                        isToggleDragStarted = true;
                        BeginCartDrag();
                    }
                }
            }else{
                CheckIfCartSnapping();
                if (!isSnapping) {
                    selectedCart.transform.position = GetMousePositionOnPlane() + offset;
                    selectedCart.transform.rotation = Quaternion.Slerp(selectedCart.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
                    offset = Vector3.Lerp(offset, Vector3.zero, lerpSpeed * Time.deltaTime);
                }
                
                if (dragClick.action.WasPerformedThisFrame()) {
                    EndCartDrag();
                }
            }
        }
    }

    private void EndCartDrag() {
        isHoldDragStarted = false;
        isToggleDragStarted = false;
        if (!isSnapping) {
            selectedCart.GetComponent<Rigidbody>().isKinematic = false;
            selectedCart.GetComponent<Rigidbody>().useGravity = true;
        }

        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.SnapDestinationCargos(selectedCart);
        }

        if (selectedCart.isMysteriousCart &&
            !(selectedCart.myLocation == UpgradesController.CartLocation.train || selectedCart.myLocation == UpgradesController.CartLocation.cargoDelivery)) {
            UpgradesController.s.RemoveCartFromShop(selectedCart);
            Train.s.AddCartAtIndex(1, selectedCart);
            selectedCart.GetComponent<Rigidbody>().isKinematic = true;
            selectedCart.GetComponent<Rigidbody>().useGravity = false;
        }

        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }


        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.SaveCartStateWithDelay();
            Train.s.SaveTrainState();
        }

        if (selectedCart.myLocation != UpgradesController.CartLocation.train) {
            if (selectedCart.myAttachedArtifact != null) {
                selectedCart.myAttachedArtifact.DetachFromCart();
            }
        }

        if (swapCart != null) {
            if (swapCart.myLocation != UpgradesController.CartLocation.train) {
                if (swapCart.myAttachedArtifact != null) {
                    var artifact = swapCart.myAttachedArtifact;
                    artifact.DetachFromCart();
                    artifact.AttachToCart(selectedCart);
                }
                
            }
        }


        PhysicalRangeShower.s.HideRange();
    }

    private void BeginCartDrag() {
        currentSnapLoc = null;
        isSnapping = false;
        sourceSnapLocation = selectedCart.GetComponentInParent<SnapCartLocation>();
        prevCartTrainSnapIndex = -1;
        selectedCart.transform.SetParent(null);
        selectedCart.GetComponent<Rigidbody>().isKinematic = true;
        selectedCart.GetComponent<Rigidbody>().useGravity = false;

        dragBasePos = selectedCart.transform.position;
        offset = dragBasePos - GetMousePositionOnPlane();

        if (Train.s.carts.Contains(selectedCart)) {
            Train.s.RemoveCart(selectedCart);
            UpgradesController.s.AddCartToShop(selectedCart, UpgradesController.CartLocation.world);
        } else {
            UpgradesController.s.ChangeCartLocation(selectedCart, UpgradesController.CartLocation.world);
        }

        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }
        
        
        PhysicalRangeShower.s.ShowCartRange(selectedCart,true);
      
        // SFX
        AudioManager.PlayOneShot(SfxTypes.OnCargoPickUp);
    }

    public SnapCartLocation currentSnapLoc;
    void CheckIfCartSnapping() {
        var carts = Train.s.carts;
        if (Mathf.Abs(GetMousePositionOnPlane().x) < 0.3f) {
            isSnapping = SnapToTrain();
        } 
        
        if(!isSnapping || Mathf.Abs(GetMousePositionOnPlane().x) > 0.3f){
            PhysicalRangeShower.s.HideRange();
            
            if (sourceSnapLocation != null && UpgradesController.s.WorldCartCount() <= 0) {
                if (sourceSnapLocation.snapTransform.childCount > 0 && prevCartTrainSnapIndex > 0) {
                    var prevCart = sourceSnapLocation.GetComponentInChildren<Cart>();
                    if (prevCart != null) {
                        UpgradesController.s.RemoveCartFromShop(prevCart);
                        Train.s.AddCartAtIndex(prevCartTrainSnapIndex, prevCart);
                    }
                }
            }
            prevCartTrainSnapIndex = -1;

            if (carts.Contains(selectedCart)) {
                Train.s.RemoveCart(selectedCart);
                UpgradesController.s.AddCartToShop(selectedCart, UpgradesController.CartLocation.world);
                isSnapping = false;
            }

            if (!selectedCart.isCargo || !PlayStateMaster.s.isShop()) { // we dont want cargo to snap to flea market locations
                RaycastHit hit;
                Ray ray = GetRay();

                if (Physics.Raycast(ray, out hit, 100f, LevelReferences.s.cartSnapLocationsLayer)) {
                    var snapLocation = hit.collider.gameObject.GetComponentInParent<SnapCartLocation>();

                    var snapLocationValidAndNew = snapLocation != null && snapLocation != currentSnapLoc;
                    var snapLocationCanAcceptCart = (!snapLocation.onlySnapCargo || selectedCart.isCargo) && (!snapLocation.onlySnapMysteriousCargo || selectedCart.isMysteriousCart);
                    var snapLocationEmpty = snapLocation.snapTransform.childCount == 0;
                    var canSnap = snapLocationValidAndNew && snapLocationCanAcceptCart && snapLocationEmpty && !snapLocation.snapNothing;

                    if (canSnap) {
                        isSnapping = true;
                        selectedCart.transform.SetParent(snapLocation.snapTransform);
                        currentSnapLoc = snapLocation;
                        UpgradesController.s.ChangeCartLocation(selectedCart, snapLocation.myLocation);
                        print("snapping to location");
                    }
                } else {
                    if (currentSnapLoc != null) {
                        isSnapping = false;
                        print("stopped snapping");
                        currentSnapLoc = null;
                        selectedCart.transform.SetParent(null);
                        UpgradesController.s.ChangeCartLocation(selectedCart, UpgradesController.CartLocation.world);
                    }
                }
            }
        }

        if (PlayStateMaster.s.isShop()) {
            UpgradesController.s.UpdateCartShopHighlights();
        } else {
            UpgradesController.s.UpdateCargoHighlights();
        }
    }

    public int prevCartTrainSnapIndex = -1;
    private bool SnapToTrain() {
        var carts = Train.s.carts;
        var zPos = GetMousePositionOnPlane().z;

        var totalLength = 0f;
        for (int i = 0; i < carts.Count; i++) {
            totalLength += carts[i].length;
        }

        var currentSpot = transform.localPosition - Vector3.back * (totalLength / 2f);

        bool inserted = false;
        float distance = 0;

        var sourceSnapIsMarket = sourceSnapLocation != null && sourceSnapLocation.myLocation == UpgradesController.CartLocation.market;
        
        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            currentSpot += -Vector3.forward * cart.length;

            if (i != 0 || sourceSnapIsMarket) {
                distance = Mathf.Abs((currentSpot.z + (cart.length / 2f)) - zPos);
                if (currentSpot.z + (cart.length / 2f) < zPos && distance < cart.length*4) {
                    if (cart != selectedCart) {
                        if (carts.Contains(selectedCart)) {
                            Train.s.RemoveCart(selectedCart);
                        } else {
                            UpgradesController.s.RemoveCartFromShop(selectedCart);
                        }

                        var canBeSwapped = true;
                        if (sourceSnapLocation != null && UpgradesController.s.WorldCartCount() <= 0) {
                            if (sourceSnapLocation.snapTransform.childCount > 0) {
                                swapCart = sourceSnapLocation.GetComponentInChildren<Cart>();
                                if (swapCart != null) {
                                    UpgradesController.s.RemoveCartFromShop(swapCart);
                                    Train.s.AddCartAtIndex(prevCartTrainSnapIndex, swapCart);
                                }
                            }

                            if (sourceSnapIsMarket) {
                                swapCart = Train.s.carts[i];
                                canBeSwapped = !swapCart.isMysteriousCart && !swapCart.isCargo && !swapCart.isMainEngine;
                                if (canBeSwapped) {
                                    Train.s.RemoveCart(swapCart);
                                    UpgradesController.s.AddCartToShop(swapCart, sourceSnapLocation.myLocation);
                                    swapCart.transform.SetParent(sourceSnapLocation.snapTransform);
                                    prevCartTrainSnapIndex = i;
                                } else {
                                    i += 1;
                                }
                            }
                        }

                        //if (canBeSwapped) {
                            inserted = true;
                            Train.s.AddCartAtIndex(i, selectedCart);
                            PhysicalRangeShower.s.ShowCartRange(selectedCart,false);
                        //}
                    } else {
                        inserted = true;
                    }

                    if(inserted)
                        return true;
                }
            }
        }

        if (!inserted) {
            if (carts[carts.Count - 1] != selectedCart && distance < carts[0].length*4) {
                if (carts.Contains(selectedCart)) {
                    Train.s.RemoveCart(selectedCart);
                } else {
                    UpgradesController.s.RemoveCartFromShop(selectedCart);
                }

                Train.s.AddCartAtIndex(carts.Count, selectedCart);
                PhysicalRangeShower.s.ShowCartRange(selectedCart, false);
                return true;
            }
        }
        
        return false;
    }


    public float lerpSpeed = 5;
    public float slerpSpeed = 20;

    void UpdateTrainCartPositionsSlowly() {
        var carts = Train.s.carts;

        var isBasic = !PlayStateMaster.s.isCombatStarted();
        
        if(carts.Count == 0)
            return;
        
        var totalLength = 0f;
        for (int i = 0; i < carts.Count; i++) {
            totalLength += carts[i].length;
        }
        
        var currentDistance = (totalLength / 2f);
        var currentSpot =  Vector3.zero;

        /*Debug.Log($"normal: {transform.localPosition - Vector3.back * (totalLength / 2f)}");
        Debug.Log( $"Brole: {- Vector3.back * (totalLength / 2f)}");*/
        
        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];
            

            
            if (isBasic) {
                currentSpot = Vector3.forward*currentDistance;
            } else {
                currentSpot = PathAndTerrainGenerator.s.GetPointOnActivePath(currentDistance);
                cart.transform.rotation = Quaternion.Slerp(cart.transform.localRotation,PathAndTerrainGenerator.s.GetRotationOnActivePath(currentDistance),slerpSpeed * Time.deltaTime);
            }
            
            if (cart == selectedCart && !cart.isMainEngine )
                currentSpot += Vector3.up * (isDragging() ? 0.4f : 0.05f);
            currentDistance += -cart.length;

            cart.transform.localPosition = Vector3.Lerp(cart.transform.localPosition, currentSpot, lerpSpeed * Time.deltaTime);
            
            var artifact = cart.myAttachedArtifact;
            if (artifact != null) {
                artifact.transform.localPosition = Vector3.MoveTowards(artifact.transform.localPosition, Vector3.zero, 2*lerpSpeed * Time.deltaTime);
                artifact.transform.localRotation = Quaternion.Slerp(artifact.transform.localRotation, Quaternion.identity, 5*slerpSpeed * Time.deltaTime);
            }

            /*if (cart == selectedCart && !cart.isMainEngine)
                currentSpot -= Vector3.up * (isDragging() ? 0.4f : 0.05f);


            currentSpot += -Vector3.forward * cart.length;*/
            var index = i;
            cart.name = $"Cart {index}";
        }
    }

    public float repairAmountPerClick = 50f; // dont use this
    public float repairAmountMultiplier = 1; 
    public float reloadAmountPerClick = 2; // dont use this
    public float reloadAmountPerClickBoost = 0; 
    public float reloadAmountMultiplier = 1;
    public float shieldUpAmountPerClick = 100f; // dont use this
    public float shieldUpAmountMultiplier = 1; 

    public float GetReloadAmount() {
        return (reloadAmountPerClick+reloadAmountPerClickBoost) * reloadAmountMultiplier;
    }

    public float GetRepairAmount(ModuleHealth health) {
        var repairAmount = repairAmountPerClick * repairAmountMultiplier;
        repairAmount = Mathf.Clamp(repairAmount, 0, health.maxHealth - health.currentHealth);
        if (repairAmount > curRepairCapacity) {
            repairAmount = curRepairCapacity;
        }
        curRepairCapacity -= repairAmount;
        curRepairRechargeDelay = repairRechargeDelay;
        
        
        return repairAmount;
    }
    
    public float GetShieldUpAmount() {
        return shieldUpAmountPerClick * shieldUpAmountMultiplier;
    }


    private bool shielding = false;
    private float holdTimer;
    void CheckAndDoClick() {
        if (!isDragging()) {
            if (selectedCart != null) {
                /*if (DragStarted(clickCart, ref clickCartTime, ref clickCartPos, ref clickCartFired) || shielding) {
                    HideInfo();
                    TryShieldCartContinuous(selectedCart);
                    shielding = true;

                    if (!clickCart.action.IsPressed())
                        shielding = false;
                    //SelectBuilding(selectedCart, true, true);
                } else if (clickCart.action.WasReleasedThisFrame() /*|| dragClick.action.IsPressed()#1#) {
                    HideInfo();
                    TryRepairCart(selectedCart);
                } else*/ if (clickCart.action.WasPerformedThisFrame() && DirectControlMaster.s.directControlLock <= 0) {

                    PerformClick();

                }else if (clickCart.action.IsInProgress()) {
                    holdTimer -= Time.deltaTime;
                    if (holdTimer <= 0) {
                        holdTimer = 0.25f;
                        PerformClick();
                    }
                } else {
                    holdTimer = 1f;
                }
            } else if (selectedEnemy != null) {
                if (clickCart.action.WasPerformedThisFrame()) {
                    HideInfo();
                }
            }
        } else {
            holdTimer = 1f;
        }
    }

    void PerformClick() {
        SelectBuilding(selectedCart, true, false);
                    
        switch (currentSelectMode) {
            case SelectMode.cart:
                //TryRepairShieldCart(selectedCart);

                break;
            case SelectMode.directControl:
            case SelectMode.topButton:
                //var stateChanger = selectedCart.GetComponentInChildren<CursorStateChanger>();
                var directControllable = selectedCart.GetComponentInChildren<DirectControllable>();

                if (directControllable) {
                    DirectControlMaster.s.AssumeDirectControl(selectedCart.GetComponentInChildren<DirectControllable>());
                } /*else if (stateChanger) {
                            SetCursorState(stateChanger.targetState, stateChanger.color);
                        }*/
                break;
            case SelectMode.reload:
                var moduleAmmo = selectedCart.GetComponentInChildren<ModuleAmmo>(true);
                if (moduleAmmo) {
                    moduleAmmo.Reload(GetReloadAmount());
                }

                break;
            case SelectMode.engineBoost:
                SpeedController.s.ActivateBoost();
                break;
        }
    }

    public void UIRepair(Cart cart) {
        TryRepairShieldCart(cart);
    }

    void TryRepairShieldCart(Cart cart) {
        if (CanRepair(cart)) {
            return;
            cart.GetHealthModule()?.Repair(GetRepairAmount(cart.GetHealthModule()));
        }else if (CanShield(cart)) {
            cart.GetHealthModule()?.ShieldUp(GetShieldUpAmount());
        }
        if(selectedCart != null)
            SelectBuilding(selectedCart, true);
    }

    void TryRepairCart(Cart cart) {
        return;
        cart.GetHealthModule()?.Repair(GetRepairAmount(cart.GetHealthModule()));
        

        if(selectedCart != null)
            SelectBuilding(selectedCart, true, true, repairColor);
    }

    void TryShieldCartContinuous(Cart cart) {
        cart.GetHealthModule()?.ShieldUp(GetShieldUpAmount() * Time.deltaTime);
        if(selectedCart != null)
            SelectBuilding(selectedCart, true, true, shieldColor);
    }

    public void CartHPUIButton(Cart cart) {
        if (PlayStateMaster.s.isCombatInProgress()) {
            var moduleAmmo = cart.GetComponentInChildren<ModuleAmmo>();
            if (moduleAmmo != null) {
                moduleAmmo.Reload(GetReloadAmount());
            }

            if (cart.GetComponentInChildren<DirectControllable>()) {
                DirectControlMaster.s.AssumeDirectControl(cart.GetComponentInChildren<DirectControllable>());
            }

            if (cart.GetComponentInChildren<EngineBoostable>()) {
                SpeedController.s.ActivateBoost();
            }
        }
    }

    public Ray GetRay() {
        if (SettingsController.GamepadMode()) {
            return GamepadControlsHelper.s.GetRay();
        } else {
            return LevelReferences.s.mainCam.ScreenPointToRay(GetMousePos());
        }
    }

    public enum SelectMode {
        cart, reload, topButton, engineBoost, emptyCart, topSkill, directControl
    }

    public SelectMode currentSelectMode = SelectMode.cart;

    public float sphereCastRadiusGamepad = 0.3f;
    public float sphereCastRadiusMouse = 0.1f;

    public float GetSphereCastRadius(bool isArtifact = false) {
        if (SettingsController.GamepadMode()) {
            return sphereCastRadiusGamepad;
        } else {
            return sphereCastRadiusMouse * (isArtifact? 0.1f : 1f);
        }
    }

    public MiniGUI_BuildingInfoCard buildingInfoCard;
    public MiniGUI_BuildingInfoCard enemyInfoCard;
    public MiniGUI_BuildingInfoCard artifactInfoCard;
    [ReadOnly]
    public Color selectingTopButtonColor;

    
    private float alternateClickTime = 0;
    private Vector3 alternateClickPos;
    private bool alternateClickFired;
    void CastRayToOutlineCart() {
        RaycastHit hit;
        Ray ray = GetRay();

        if (Physics.SphereCast(ray, GetSphereCastRadius(), out hit, 100f, LevelReferences.s.buildingLayer)) {
            var lastSelectMode = currentSelectMode;
            currentSelectMode = SelectMode.emptyCart;
            
            if(hit.collider.GetComponentInParent<ModuleHealth>() && canRepair)
                currentSelectMode = SelectMode.cart;
            
            
            
            var topButton = hit.rigidbody.gameObject.GetComponentInChildren<IShowButtonOnCartUIDisplay>();
            if (topButton != null) {
                currentSelectMode = SelectMode.topButton;
                selectingTopButtonColor = topButton.GetColor();
            }
            
            var directControllable = hit.rigidbody.gameObject.GetComponentInChildren<DirectControllable>();
            if (directControllable != null) {
                currentSelectMode = SelectMode.directControl;
            }
            
            var reloadable = hit.rigidbody.gameObject.GetComponentInChildren<Reloadable>();
            if (reloadable != null && canReload) {
                currentSelectMode = SelectMode.reload;
            }
            
            var engineBoostable = hit.rigidbody.gameObject.GetComponentInChildren<EngineBoostable>();
            if (engineBoostable != null) {
                currentSelectMode = SelectMode.engineBoost;
            }
            
            var cart = hit.collider.GetComponentInParent<Cart>();

            if (cart.isDestroyed) {
                currentSelectMode = SelectMode.cart;
            }
            
            
            
            if (cart != selectedCart || lastSelectMode != currentSelectMode) {
                SelectBuilding(cart, true);

                // SFX
                AudioManager.PlayOneShot(SfxTypes.OnCargoHover);
            } else {
                if ((showDetailClick.action.WasPerformedThisFrame() || DragStarted(alternateClick, ref alternateClickTime, ref alternateClickPos, ref alternateClickFired))) {
                    ShowSelectedThingInfo();
                }
            }

        } else {
            if(selectedCart != null)
                Deselect();
        }
    }
    
    void CastRayToOutlineEnemy() {
        RaycastHit hit;
        Ray ray = GetRay();

        if (Physics.SphereCast(ray, GetSphereCastRadius(), out hit, 100f, LevelReferences.s.enemyLayer)) {
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) {
                if (enemy != selectedEnemy) {
                    SelectEnemy(enemy, true);
                } else {
                    if (PlayStateMaster.s.isShopOrEndGame() && showDetailClick.action.WasPerformedThisFrame() /*|| (holdOverTimer > infoShowTime && !SettingsController.GamepadMode())*/) {
                        ShowSelectedThingInfo();
                    }
                }
            }

        } else {
            if(selectedEnemy != null)
                Deselect();
        }
    }
    
    
    void CastRayToOutlineArtifact() {
        RaycastHit hit;
        Ray ray = GetRay();

        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, 100f, LevelReferences.s.artifactLayer)) {
            var artifact = hit.collider.GetComponentInParent<Artifact>();
            //print($"{artifact} - {selectedArtifact}");
            if (artifact != null) {
                if (artifact != selectedArtifact) {
                    SelectArtifact(artifact, true);
                } else {
                    if (showDetailClick.action.WasPerformedThisFrame() || DragStarted(alternateClick, ref alternateClickTime, ref alternateClickPos, ref alternateClickFired)) {
                        ShowSelectedThingInfo();
                    }
                }
            }
        } else {
            if (selectedArtifact != null)
                Deselect();
        }
        
    }


    private float meepleHoldTime = 0;
    void CastRayToOutlineMeeple() {
        RaycastHit hit;
        Ray ray = GetRay();

        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, 100f, LevelReferences.s.meepleLayer)) {
            var meeple = hit.collider.GetComponentInParent<Meeple>();
            //print($"{artifact} - {selectedArtifact}");
            if (meeple != null) {
                meepleHoldTime += Time.deltaTime;
                if (meeple != selectedMeeple) {
                    meepleHoldTime = 0;
                    SelectMeeple(meeple, true);
                } else {
                    if (showDetailClick.action.WasPerformedThisFrame() || 
                        DragStarted(alternateClick, ref alternateClickTime, ref alternateClickPos, ref alternateClickFired)||
                        meepleHoldTime > 1f) {
                        selectedMeeple.ShowChat();
                        //ShowSelectedThingInfo();
                    }
                }
            }
        } else {
            meepleHoldTime = 0;
            if (selectedMeeple != null)
                Deselect();
        }
        
    }


    private bool infoCardActive = false;
    void ShowSelectedThingInfo() {
        if (!infoCardActive) {
            infoCardActive = true;
            if(selectedCart != null)
                buildingInfoCard.SetUp(selectedCart);
            else if (selectedEnemy != null)
                enemyInfoCard.SetUp(selectedEnemy);
            else if (selectedArtifact != null)
                artifactInfoCard.SetUp(selectedArtifact);
            else
                infoCardActive = false;

            //SFX
            AudioManager.PlayOneShot(SfxTypes.OnInfoSelected);
        } 
    }

    void HideInfo() {
        infoCardActive = false;
        buildingInfoCard.Hide();
        enemyInfoCard.Hide();
        artifactInfoCard.Hide();
    }

    public void Deselect() {
        if (selectedCart != null) {
            var cart = selectedCart;
            selectedCart = null;
            SelectBuilding(cart, false);
        }

        if (selectedEnemy != null) {
            var enemy = selectedEnemy;
            selectedEnemy = null;
            SelectEnemy(enemy, false);
        }
        
        if (selectedArtifact != null) {
            var artifact = selectedArtifact;
            selectedArtifact = null;
            SelectArtifact(artifact, false);
        }

        if (selectedMeeple != null) {
            var meeple = selectedMeeple;
            selectedMeeple = null;
            SelectMeeple(meeple, false);
        }
        
        HideInfo();
    }

    public void SelectEnemy(EnemyHealth enemy, bool isSelecting, bool showShowDetails = true) {
        if(isSelecting && enemy == selectedEnemy)
            return;
        //Debug.Log("selecting enemy");
        Deselect();

        Outline outline = null;
        if(enemy != null)
            outline = enemy.GetComponentInChildren<Outline>();
        
        if (isSelecting) {
            if(showShowDetails)
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
            selectedEnemy = enemy;
        } else {
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.showDetails);
        }

        if (enemy != null) {
            outline.enabled = isSelecting;
        }

        OnSelectEnemy?.Invoke(enemy, isSelecting);
    }
    
    void SelectArtifact(Artifact artifact, bool isSelecting) {
        Deselect();

        Outline outline = null;
        if(artifact != null)
            outline = artifact.GetComponentInChildren<Outline>();
        
        if (isSelecting) {
            if (PlayStateMaster.s.isShopOrEndGame()) {
                PhysicalRangeShower.s.ShowArtifactRange(artifact,true);
            }
            
            Color myColor = moveColor;
            if (PlayStateMaster.s.isShopOrEndGame()) {
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
                if (!CanDragArtifact(artifact)) {
                    myColor = cantActColor;
                    GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
                    GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
                }
            } else {
                myColor = cantActColor;
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
            }
            selectedArtifact = artifact;
            
            outline.OutlineColor = myColor;
        } else {
            if (PlayStateMaster.s.isShopOrEndGame()) {
                PhysicalRangeShower.s.HideRange();
            }
            
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.showDetails);
        }

        if (outline != null) {
            outline.enabled = isSelecting;
        }

        OnSelectArtifact?.Invoke(artifact, isSelecting);
    }
    
    void SelectMeeple(Meeple meeple, bool isSelecting) {
        Deselect();

        Outline outline = null;
        if(meeple != null)
            outline = meeple.GetComponentInChildren<Outline>();
        
        if (isSelecting) {
            selectedMeeple = meeple;
            
            //GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
        } else {
            meeple.shownChat = false;
            //GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
        }

        if (outline != null) {
            outline.enabled = isSelecting;
        }
    }

    bool CanShield(Cart cart) {
        if (cart == null) {
            Debug.Log($"Cart is null {cart}");
        }
        var healthModule = cart.GetHealthModule();
        if (healthModule.maxShields > 0) {
            return true;
        } else {
            return false;
        }
    }
    
    bool CanRepair(Cart cart) {
        var healthModule = cart.GetHealthModule();
        if (healthModule.currentHealth < healthModule.maxHealth) {
            return true;
        } else {
            return false;
        }
    }

    void SelectBuilding(Cart building, bool isSelecting, bool isDefaultClick = true, bool isMove = false) {
        SelectBuilding(building, isSelecting, false, Color.white, isDefaultClick, isMove);
    }

    [HideInInspector]
    public UnityEvent<Cart, bool> OnSelectBuilding = new UnityEvent<Cart, bool>();
    [HideInInspector]
    public UnityEvent<EnemyHealth, bool> OnSelectEnemy = new UnityEvent<EnemyHealth, bool>();
    [HideInInspector]
    public UnityEvent<Artifact, bool> OnSelectArtifact = new UnityEvent<Artifact, bool>();
    [HideInInspector]
    public UnityEvent<IClickableWorldItem, bool> OnSelectGate = new UnityEvent<IClickableWorldItem, bool>();
    void SelectBuilding(Cart building, bool isSelecting, bool forceColor, Color forcedColor, bool isDefaultClick = true, bool isMove = false) {
        Deselect();
        
        Outline outline = null;
        if(building != null)
            outline = building.GetComponentInChildren<Outline>();

        if (isSelecting) {
            if (PlayStateMaster.s.isShopOrEndGame()) {
                PhysicalRangeShower.s.ShowCartRange(building,true);
            }
            
            if(!isMove)
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
            
            Color myColor = cantActColor;
            if (PlayStateMaster.s.isShopOrEndGame()) {
                myColor = moveColor;
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
            }

            if (building!= null && !CanDragCart(building)) {
                myColor = cantActColor;
                GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
                GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
            }

            if (PlayStateMaster.s.isCombatInProgress() && !isMove) {
                /*if (CanShield(building)) {
                    myColor = shieldColor;
                    GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.shield);
                }

                if (CanRepair(building)) {
                    myColor = repairColor;
                }

                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.repair);*/
                
                switch (currentSelectMode) {
                    case SelectMode.cart:
                        // do nothing. This will do the default action button
                        break;
                    case SelectMode.topButton:
                            myColor = selectingTopButtonColor;
                        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.selectTopButton);
                        break;
                    case SelectMode.directControl:
                            myColor = directControlColor;
                        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.directControl);
                        break;
                    case SelectMode.reload:
                            myColor = reloadColor;
                        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.reload);
                        break;
                    case SelectMode.engineBoost:
                            myColor = engineBoostColor;
                        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.engineBoost);
                        break;
                    case SelectMode.emptyCart:
                            myColor = cantActColor;
                        break;
                }
            }

            if (outline != null) {
                if (forceColor) {
                    outline.OutlineColor = forcedColor;
                } else {
                    outline.OutlineColor = myColor;
                }

                outline.OutlineWidth = 5;
            }

            selectedCart = building;
        } else {
            if (PlayStateMaster.s.isShopOrEndGame()) {
                PhysicalRangeShower.s.HideRange();
            }
            
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.repair);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.reload);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.directControl);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.showDetails);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.engineBoost);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.selectTopButton);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.shield);
        }
        

        if (building != null) {
            outline.enabled = isSelecting;
            var ranges = building.GetComponentsInChildren<RangeVisualizer>();
            for (int i = 0; i < ranges.Length; i++) {
                ranges[i].ChangeVisualizerEdgeShowState(isSelecting);
            }


            foreach (var artifactSlot in building.GetComponentsInChildren<VisualizeArtifactSlot>()) {
                artifactSlot.SetState(isSelecting && PlayStateMaster.s.isShopOrEndGame());
            }
        }

        OnSelectBuilding?.Invoke(building, isSelecting);
    }
    

    private void LogData(bool currentlyMultiBuilding, Cart newBuilding) {
        var buildingName = newBuilding.uniqueName;
        

        /*if (currentLevelStats.TryGetValue(buildingName, out BuildingData data)) {
            data.constructionData.Add(cData);
        } else {
            var toAdd = new BuildingData();
            toAdd.uniqueName = buildingName;
            currentLevelStats.Add(buildingName, toAdd);
        }*/
    }

    public void LogCurrentLevelBuilds(bool isWon) {
        /*foreach (var keyValPair in currentLevelStats) {
            var bName = keyValPair.Key;
            var bData = keyValPair.Value;
            var constStats = bData.constructionData;

            Dictionary<string, object> resultingDictionary = new Dictionary<string, object>();

            resultingDictionary["currentLevel"] = SceneLoader.s.currentLevel.levelName;
            resultingDictionary["isWon"] = isWon;

            resultingDictionary["buildCount"] = constStats.Count;

            if (constStats.Count > 0) {
                var averageTrainPosition = constStats.Average(x => x.buildTrainPercent);
                resultingDictionary["buildTrainPercent"] = RatioToStatsPercent(averageTrainPosition);

                var multiBuildRatio = (float)constStats.Count(x => x.isMultiBuild) / (float)constStats.Count;
                resultingDictionary["isMultiBuild"] = RatioToStatsPercent(multiBuildRatio);

                var averageBuildLevelDistance = constStats.Average(x => x.buildLevelDistance);
                resultingDictionary["buildMissionDistance"] = DistanceToStats(averageBuildLevelDistance);

                TrainBuilding.Rots maxRepeated = constStats.GroupBy(s => s.buildRotation)
                    .OrderByDescending(s => s.Count())
                    .First().Key;
                resultingDictionary["buildRotation"] = maxRepeated;
            } 
            

            resultingDictionary["buildDamage"] = (int)bData.damageData;

            //print(resultingDictionary);

            AnalyticsResult analyticsResult = Analytics.CustomEvent(
                bName,
                resultingDictionary
                );
            
            Debug.Log("Building Build Data Analytics " + analyticsResult);
            
            Instantiate(statsPrefab, statsParent).GetComponent<MiniGUI_StatDisplay>().SetUp(bName + " Build Count", (constStats.Count).ToString());
            Instantiate(statsPrefab, statsParent).GetComponent<MiniGUI_StatDisplay>().SetUp(bName + " Damage", ((int)bData.damageData).ToString());
        }*/
    }
    

    public Vector3 GetMousePositionOnPlane() {
        Plane plane = new Plane(Vector3.up, new Vector3(0,0.5f,0));

        float distance;
        Ray ray = GetRay();
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        } else {
            return Vector3.zero;
        }
    }

}

public interface IPlayerHoldable {
    
}


