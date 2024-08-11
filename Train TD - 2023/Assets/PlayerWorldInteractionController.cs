using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighlightPlus;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;


public class PlayerWorldInteractionController : MonoBehaviour { 
    public static PlayerWorldInteractionController s;
    private void Awake() {
        s = this;
        //HideInfo(); if things arent hidden by default ui follow world target dont work correctly
    }

    private void OnDestroy() {
        s = null;
    }

    
    public IPlayerHoldable currentSelectedThing;
    public MonoBehaviour currentSelectedThingMonoBehaviour => currentSelectedThing as MonoBehaviour;
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
    public InputActionReference cycleAction; 
    public InputActionReference showDetailClick; // detail click is its own button on gamepad
    public InputActionReference hideTutorial; 
    

    [SerializeField]
    private bool _canSelect;

    public bool canSelect {
        get { return _canSelect; }
        set { SetCannotSelect(value); }
    }
    
    protected void OnEnable()
    {
        clickCart.action.Enable();
        alternateClick.action.Enable();
        showDetailClick.action.Enable();
        clickCart.action.started += DragStart;
        clickCart.action.canceled += DragClickMaybeEnd;
        cycleAction.action.Enable();
        cycleAction.action.performed += CycleSelected;
        hideTutorial.action.Enable();
    }

    public int selectOffset = 0;
    public int placeOffset = 0;

    private void CycleSelected(InputAction.CallbackContext obj) {
        if (isDragging()) {
            placeOffset += 1;
        } else {
            selectOffset += 1;
        }
    }

    private void DragStart(InputAction.CallbackContext context) {
        if (!CanInteract()) {
            return;
        }

        if (context.interaction is HoldInteraction) {
            BeginDrag();
        } else {
            if (isComboDragStarted) {
                EndDrag();
            } else {
                BeginDrag();
            }
        }
    }

    private void DragClickMaybeEnd(InputAction.CallbackContext context) {
        if (!CanInteract()) {
            return;
        }
        if (context.interaction is HoldInteraction) {
            EndDrag();
        }
    }


    protected void OnDisable()
    {
        clickCart.action.Disable();
        alternateClick.action.Disable();
        showDetailClick.action.Disable();
        clickCart.action.started -= DragStart;
        clickCart.action.performed -= DragClickMaybeEnd;
        cycleAction.action.Disable();
        cycleAction.action.performed -= CycleSelected;
        hideTutorial.action.Disable();
    }

    public Color cantActColor = Color.white;
    public Color destroyedColor = Color.black;
    public Color moveColor = Color.blue;
    public Color reloadColor = Color.yellow;
    public Color directControlColor = Color.magenta;
    public Color engineBoostColor = Color.red;
    public Color repairColor = Color.green;
    public Color shieldColor = Color.cyan;
    public Color enemyColor = Color.white;
    public Color meepleColor = Color.white;
    public Color clickableColor = Color.white;
    public Color mergeItemColor = Color.yellow;

    bool CanInteract() {
        return !(!canSelect || Pauser.s.isPaused || PlayStateMaster.s.isLoading || DirectControlMaster.s.directControlLock > 0);
    }
    private void LateUpdate() {
        if (!CanInteract()) {
            return;
        }

        if (infoCardActive) {
            if (showDetailClick.action.WasPerformedThisFrame() || clickCart.action.WasPerformedThisFrame()) {
                HideInfo();
            }
        }

        if (!isDragging() && !infoCardActive) {
            CastRayToOutline();
        }

        //CheckAndDoDrag();
        if (isDragging()) {
            DoDrag();
        }
        
        if (PlayStateMaster.s.isCombatInProgress()) {
            CheckAndDoCombatClick();
        } else {
            CheckGate();
        }
    }

    public void SetCannotSelect(bool __canSelect) {
        _canSelect = __canSelect;
        if (!_canSelect) {
            Deselect();
        }
        isComboDragStarted = false;
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
    
    [HideInInspector]
    public UnityEvent<IClickableWorldItem, bool> OnSelectGate = new UnityEvent<IClickableWorldItem, bool>();
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

    public bool isDragging() {
        return isComboDragStarted;
    }

    private float clickCartTime = 0;
    private Vector3 clickCartPos;
    private bool clickCartFired;

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
    

    public void MoveSelectedCart(bool isForward) {
        if (PlayStateMaster.s.isCombatInProgress() && isDragging()) {
            var selectedCart = currentSelectedThing as Cart;
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

    bool CanDragArtifact(Artifact artifact) {
        return artifact.canDrag; 
    }
    bool CanDragCart(Cart cart) {
        return (!cart.isMainEngine && cart.canPlayerDrag) && (PlayStateMaster.s.isShopOrEndGame() || !cart.IsAttachedToTrain());
    }
    
    bool CanDragMeeple(Meeple meeple) {
        return PlayStateMaster.s.isShopOrEndGame();
    }
    
    bool CanDragGenericClickable(IGenericClickable genericClickable) {
        return true;
    }

    bool CanDragThing(IPlayerHoldable thing) {
        if (thing is Artifact artifact) {
            return CanDragArtifact(artifact);
        }else if ( thing is Cart cart) {
            return CanDragCart(cart);
        }else if (thing is EnemyHealth) {
            return false;
        }else if (thing is Meeple meeple) {
            return CanDragMeeple(meeple);
        } else if (thing is ScrapsItem scrapsItem) {
            return scrapsItem.CanDrag();
        }else if (thing is IGenericClickable genericClickable) {
            return CanDragGenericClickable(genericClickable);
        }
        
        return false;
    }

    public bool isComboDragStarted = false;
    public Vector3 offset;

    void BeginDrag() {
        if (isDragging()) {
            return;
        }
        CastRayToOutline();
        if (!CanDragThing(currentSelectedThing)) {
            return;
        }
        isComboDragStarted = true;
        
        isSnapping = false;

        displacedArtifact = null;
        currentSnapLoc = null;
        sourceSnapLocation = currentSelectedThingMonoBehaviour.GetComponentInParent<SnapLocation>();
        
        currentSelectedThing.SetHoldingState(true);
        dragBasePos = currentSelectedThingMonoBehaviour.transform.position;
        offset = dragBasePos - GetMousePositionOnPlane();

        if (currentSelectedThing is Cart selectedCart) {
            if (Train.s.carts.Contains(selectedCart)) {
                Train.s.RemoveCart(selectedCart);
                ShopStateController.s.AddCartToShop(selectedCart);
            } 
            
            MakeOnTrainCartSnapLocations();
        }

        var rubbleFollowFloor = currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>();
        if (rubbleFollowFloor) {
            rubbleFollowFloor.UnAttachFromFloor();
            rubbleFollowFloor.canAttachToFloor = false;
        }
        
        if(!(currentSelectedThing is Meeple))
            currentSelectedThingMonoBehaviour.transform.SetParent(null);

        if (currentSelectedThing is Artifact) {
            for (int i = 0; i < Train.s.carts.Count; i++) {
                var thisCart = Train.s.carts[i];
                foreach (var artifactSlot in thisCart.myArtifactLocations) {
                    artifactSlot.SetVisualizeState(artifactSlot.CanSnap(currentSelectedThing));
                }
            }
        }

        if (currentSelectedThing is IGenericClickable genericClickable) {
            genericClickable.Click();
            Deselect();
        }

        if (LevelReferences.s.combatHoldableThings.Contains(currentSelectedThing)) {
            LevelReferences.s.combatHoldableThings.Remove(currentSelectedThing);
        }
        
        // SFX
        AudioManager.PlayOneShot(SfxTypes.OnCargoPickUp);
    }

    public List<SnapLocation> onTrainCartSnapLocations = new List<SnapLocation>();
    public GameObject onTrainCartSnapPrefab;
    void MakeOnTrainCartSnapLocations() { // we call this AFTER removing the previous cart from the train
        var reqNumber = Train.s.carts.Count;
        while (onTrainCartSnapLocations.Count > reqNumber) {
            Destroy(onTrainCartSnapLocations[0].gameObject);
            onTrainCartSnapLocations.RemoveAt(0);
        }

        while (onTrainCartSnapLocations.Count < reqNumber) {
            var newLoc = Instantiate(onTrainCartSnapPrefab, transform).GetComponent<SnapLocation>();
            onTrainCartSnapLocations.Add(newLoc);
        }
    }

    void UpdateOnTrainCartSnapLocationPositions() {
        var adjustedTrainLength = Train.s.GetTrainLength();

        var myCart = (currentSelectedThing as Cart);

        if (Train.s.carts.Contains(myCart)) {
            //adjustedTrainLength -= myCart.length;
        } else {
            adjustedTrainLength += myCart.length;
        }

        var currentDistance = (adjustedTrainLength / 2f);
        currentDistance -= Train.s.carts[0].length;

        for (int i = 0; i < onTrainCartSnapLocations.Count; i++) {
            var thisLoc = onTrainCartSnapLocations[i];

            thisLoc.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(currentDistance);
            thisLoc.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(currentDistance);
            if (i+1 < Train.s.carts.Count) {
                currentDistance -= Train.s.carts[i+1].length;
            } else {
                currentDistance -= myCart.length;
            }
        }
    }
    

    public bool isSnapping = false;
    public SnapLocation sourceSnapLocation;
    public SnapLocation currentSnapLoc;
    public SnapLocation displacedArtifactSource;
    public Artifact displacedArtifact;
    private void DoDrag() {
        if(currentSelectedThing is Cart)
            UpdateOnTrainCartSnapLocationPositions();
        
        var newSnapLoc = GetSnapLoc();
        var shouldSnap = newSnapLoc != null;
        
        if (isSnapping != shouldSnap || newSnapLoc != currentSnapLoc) {
            isSnapping = shouldSnap;

            if (isSnapping) {
                AudioManager.PlayOneShot(SfxTypes.OnCargoDrop);
            } else {
                AudioManager.PlayOneShot(SfxTypes.OnCargoDrop2);
                if(!(currentSelectedThing is Meeple))
                    currentSelectedThingMonoBehaviour.transform.SetParent(null);

                if (currentSelectedThing is Cart selectedCart) {
                    Train.s.RemoveCart(selectedCart);
                    ShopStateController.s.AddCartToShop(selectedCart);
                    selectedCart.SetHighlightState(false);

                    for (int i = 0; i < Train.s.carts.Count; i++) {
                        Train.s.carts[i].SetHighlightState(false);
                    }
                }

                if (currentSelectedThing is Artifact selectedArtifact) {
                    selectedArtifact.DetachFromCart();
                    selectedArtifact.GetComponent<Rigidbody>().isKinematic = true;
                    selectedArtifact.GetComponent<Rigidbody>().useGravity = false;
                }
                
                
                dragBasePos = currentSelectedThingMonoBehaviour.transform.position;
                offset = dragBasePos - GetMousePositionOnPlane();
            }
        }
        isSnapping = newSnapLoc != null;

        var lerpToMouse = !isSnapping;

        if (isSnapping) {
            if (newSnapLoc != currentSnapLoc) {
                if (currentSelectedThing is Cart selectedCart) {
                    if (!newSnapLoc.IsEmpty()) {
                        newSnapLoc.UnSnapExistingObject();
                    }
                    
                    newSnapLoc.SnapObject(selectedCart.gameObject);
                    var onTrainIndex = onTrainCartSnapLocations.IndexOf(newSnapLoc);
                    if (onTrainIndex != -1) {
                        Train.s.AddCartAtIndex(onTrainIndex+1, selectedCart);
                        ShopStateController.s.RemoveCartFromShop(selectedCart);
                        
                        // merge highlights
                        /*for (int i = 0; i < Train.s.carts.Count; i++) {
                            Train.s.carts[i].SetHighlightState(false);
                        }

                        var canMerge = false;
                        Cart mergeCart = null;
                        if (selectedCart.trainIndex > 0) {
                            canMerge = DataHolder.s.GetMergeResult(selectedCart.uniqueName, Train.s.carts[selectedCart.trainIndex -1].uniqueName) != null;
                            if (canMerge) {
                                mergeCart = Train.s.carts[selectedCart.trainIndex -1];
                            }
                        }
                        if (!canMerge && selectedCart.trainIndex + 1 < Train.s.carts.Count) {
                            canMerge = DataHolder.s.GetMergeResult(selectedCart.uniqueName, Train.s.carts[selectedCart.trainIndex + 1].uniqueName) != null;
                            if (canMerge) {
                                mergeCart = Train.s.carts[selectedCart.trainIndex + 1];
                            }
                        }
                        if (canMerge) {
                            selectedCart.SetHighlightState(true, mergeItemColor);
                            mergeCart.SetHighlightState(true, mergeItemColor);
                        }*/

                    } else {
                        if (Train.s.carts.Contains(selectedCart)) {
                            Train.s.RemoveCart(selectedCart);
                            ShopStateController.s.AddCartToShop(selectedCart);
                        }
                    }

                }else if (currentSelectedThing is Artifact selectedArtifact) {
                    if (displacedArtifact != null) {
                        displacedArtifact.DetachFromCart();
                        if(displacedArtifactSource != null) {
                            displacedArtifact.AttachToSnapLoc(displacedArtifactSource);
                        }
                        displacedArtifact = null;
                    }
                    selectedArtifact.DetachFromCart();
                    selectedArtifact.GetComponent<Rigidbody>().isKinematic = true;
                    selectedArtifact.GetComponent<Rigidbody>().useGravity = false;
                    
                    if (newSnapLoc.IsEmpty()) {
                        selectedArtifact.AttachToSnapLoc(newSnapLoc);
                    } else {
                        displacedArtifact = newSnapLoc.GetSnappedObject().GetComponent<Artifact>();
                        if (displacedArtifact != null) {
                            displacedArtifactSource = newSnapLoc;
                            displacedArtifact.DetachFromCart();
                            if (sourceSnapLocation != null) {
                                displacedArtifact.AttachToSnapLoc(sourceSnapLocation);
                            }
                        } else {
                            newSnapLoc.UnSnapExistingObject();
                        }
                        
                        
                        selectedArtifact.AttachToSnapLoc(newSnapLoc);
                    }
                } else {
                    newSnapLoc.SnapObject(currentSelectedThingMonoBehaviour.gameObject);
                }

                currentSnapLoc = newSnapLoc;
            }
        } else {
            currentSnapLoc = null;
        }
        
        
        if(lerpToMouse) {
            currentSelectedThingMonoBehaviour.transform.position = GetMousePositionOnPlane() + offset;
            currentSelectedThingMonoBehaviour.transform.rotation = Quaternion.Slerp(currentSelectedThingMonoBehaviour.transform.rotation, Quaternion.identity, slerpSpeed * Time.deltaTime);
            if (currentSelectedThing is Cart) {
                offset = Vector3.Lerp(offset, Vector3.zero, lerpSpeed * Time.deltaTime);
            } else {
                offset = Vector3.Lerp(offset, Vector3.up / 2f, lerpSpeed * Time.deltaTime);
            }
        }
    }

    SnapLocation GetSnapLoc() {
        var checkPos = GetMousePositionOnPlane();
        checkPos.y = 0;
        return GetSnapLoc(checkPos, currentSelectedThing);
    }

    private SnapLocation[] validSnapLocations = new SnapLocation[16];
    public  SnapLocation GetSnapLoc(Vector3 checkPos, IPlayerHoldable holdable) {
        var index = 0;
        for (int i = 0; i < LevelReferences.s.allSnapLocations.Count; i++) {
            var thisSnapLoc = LevelReferences.s.allSnapLocations[i];

            if (thisSnapLoc.CanSnap(holdable)) {
                var snapPos = thisSnapLoc.transform.position;
                snapPos.y = 0;
                
                var distance = Vector3.Distance(snapPos, checkPos);
                thisSnapLoc.curDistance = distance;
                if (distance < thisSnapLoc.snapDistance) {
                    validSnapLocations[index] = thisSnapLoc;
                    index += 1;
                }
            }
        }

        if (index == 0) {
            return null;
        }

        var size = index;
        BubbleSort(validSnapLocations, size);
        
        placeOffset %= size;
        if (size > 0) {
            var item = validSnapLocations[placeOffset];
            if (size > 1) {
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.cycleSelectedItem);
            } else {
                GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.cycleSelectedItem);
            }
            
            return item;
        } else {
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.cycleSelectedItem);
        }

        return null;
    }


    void EndDrag() {
        if (currentSelectedThing == null || !isDragging()) {
            return;
        }

        isComboDragStarted = false;

        if (isSnapping) {
            if (currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>() != null) {
                Destroy(currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>());
            }

            if (currentSelectedThing is Cart || currentSelectedThing is Artifact) {
                var rigid = (currentSelectedThingMonoBehaviour).GetComponent<Rigidbody>();
                rigid.isKinematic = true;
                rigid.useGravity = false;
            }

            if (PlayStateMaster.s.isCombatInProgress()) {
                if (displacedArtifact != null) {
                    if (!LevelReferences.s.combatHoldableThings.Contains(displacedArtifact)) {
                        LevelReferences.s.combatHoldableThings.Add(displacedArtifact);
                    }
                }
            }

        }else{
            currentSelectedThing.SetHoldingState(false);
            

            if (PlayStateMaster.s.isCombatInProgress()) {
                if (currentSelectedThingMonoBehaviour is Cart || currentSelectedThingMonoBehaviour is Artifact)
                    LevelReferences.s.combatHoldableThings.Add(currentSelectedThing);
            }

            var rubbleFollowFloor = currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>();

            if (PlayStateMaster.s.isCombatInProgress()) {
                if (rubbleFollowFloor) {
                    rubbleFollowFloor.canAttachToFloor = true;
                } else {
                    currentSelectedThingMonoBehaviour.gameObject.AddComponent<RubbleFollowFloor>();
                }
            } else {
                if (rubbleFollowFloor) {
                    rubbleFollowFloor.UnAttachFromFloor();
                    Destroy(rubbleFollowFloor);
                }
            }
            
        }
        
        if (PlayStateMaster.s.isShop()) {
            if (currentSelectedThing is Cart || currentSelectedThing is Artifact) {
                ShopStateController.s.SaveCartStateWithDelay();
                Train.s.SaveTrainState();
            }else if (currentSelectedThing is ScrapsItem) {
                ShopStateController.s.SaveCartStateWithDelay();
            }
        }
        
        if (currentSelectedThing is Artifact) {
            for (int i = 0; i < Train.s.carts.Count; i++) {
                var thisCart = Train.s.carts[i];
                foreach (var artifactSlot in thisCart.myArtifactLocations) {
                    artifactSlot.SetVisualizeState(false);
                }
            }
        }

        if (currentSelectedThing is Cart || currentSelectedThing is Artifact) {
            Train.s.UpdateThingsAffectingOtherThings();
        }

        Deselect();
        
        AudioManager.PlayOneShot(SfxTypes.OnCargoDrop2);
    }

    public const float lerpSpeed = 10;
    public const float slerpSpeed = 20;
    
    void CheckAndDoCombatClick() {
        if (!isDragging()) {
            if (currentSelectedThing != null ) {
                if (currentSelectedThing is Cart selectedCart) {
                    if (selectedCart.IsAttachedToTrain()) {
                        if (clickCart.action.WasPerformedThisFrame() && DirectControlMaster.s.directControlLock <= 0) {
                            PerformCartClick(selectedCart);

                        }
                    }
                }else if (currentSelectedThing is EnemyHealth) {
                    if (clickCart.action.WasPerformedThisFrame()) {
                        HideInfo();
                    }
                }
            } 
        } 
    }

    void PerformCartClick(Cart selectedCart) {
        var directControllable = selectedCart.GetComponentInChildren<IDirectControllable>();

        if (directControllable != null) {
            DirectControlMaster.s.AssumeDirectControl(selectedCart.GetComponentInChildren<IDirectControllable>());
        }
    }

    public bool screenIsFlipped = false;
    public Ray GetRay() {
        var forwardMoveAdjustment = Train.s.GetTrainForward() * LevelReferences.s.speed * Time.deltaTime;
        //var forwardMoveAdjustment = Vector3.zero;
        Ray ray;
        if (SettingsController.GamepadMode()) {
            ray= GamepadControlsHelper.s.GetRay();
        } else {
            var pos = GetMousePos();
            if (screenIsFlipped) {
                pos.x = Screen.width - pos.x;
            }

            ray= LevelReferences.s.mainCam.ScreenPointToRay(pos);
        }

        ray.origin -= forwardMoveAdjustment;
        return ray;
    }

    


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
    public MiniGUI_EnemyInfoCard enemyInfoCard;
    public MiniGUI_ArtifactInfoCard artifactInfoCard;

    private float alternateClickTime = 0;
    private Vector3 alternateClickPos;
    private bool alternateClickFired;
    
    void CastRayToOutline() {
        var newSelectable = GetFirstPriorityItem();

        if (newSelectable != null) {
            if (newSelectable != currentSelectedThing) {
                SelectThing(newSelectable, true);
                
                // SFX
                AudioManager.PlayOneShot(SfxTypes.OnCargoHover);
            }else {
                if ((showDetailClick.action.WasPerformedThisFrame() || DragStarted(alternateClick, ref alternateClickTime, ref alternateClickPos, ref alternateClickFired))) {
                    ShowSelectedThingInfo();
                }
            }
        } else {
            Deselect();
        }
    }

    private IPlayerHoldable GetFirstPriorityItem() {
        if (SettingsController.GamepadMode()) {
            return GetFirstPriorityItemGamePad();
        } else {
            return GetFirstPriorityItemMouse();
        }
    }

    private RaycastHit[] hitCastArray = new RaycastHit[32];
    private IPlayerHoldable[] hitSeenObjects = new IPlayerHoldable[32];
    private IPlayerHoldable GetFirstPriorityItemGamePad() {
        // selection preference: artifact -> cart -> enemy -> meeple -> merge item -> IGenericClickable
        RaycastHit hit;
        Ray ray = GetRay();

        var size = Physics.SphereCastNonAlloc(ray, GetSphereCastRadius(true), hitCastArray, 20f, LevelReferences.s.allSelectablesLayer);
        if (size == 0) {
            return null;
        }
        
        RemoveDupes(hitCastArray, ref size);
        BubbleSort(hitCastArray, size);

        selectOffset %= size;
        if (size > 0) {
            var item = hitCastArray[selectOffset].collider.GetComponentInParent<IPlayerHoldable>();
            if (size > 1) {
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.cycleSelectedItem);
            } else {
                GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.cycleSelectedItem);
            }
            
            return item;
        } else {
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.cycleSelectedItem);
        }

        return null;
    }

    IPlayerHoldable GetFirstPriorityItemMouse() {
        // selection preference: artifact -> cart -> enemy -> meeple -> merge item
        RaycastHit hit;
        Ray ray = GetRay();
        
        Debug.DrawRay(GetRay().origin,GetRay().direction*10);

        var maxDistance = 20f;
        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, maxDistance, LevelReferences.s.artifactLayer)) {
            return hit.collider.gameObject.GetComponentInParent<Artifact>();
        }
    
        if (Physics.Raycast(ray, out hit, maxDistance, LevelReferences.s.buildingLayer)) {
            return hit.collider.gameObject.GetComponentInParent<Cart>();
        }

        if (Physics.Raycast(ray,  out hit, maxDistance, LevelReferences.s.enemyLayer)) {
            return hit.collider.GetComponentInParent<EnemyHealth>();
        }

        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, maxDistance, LevelReferences.s.meepleLayer)) {
            return hit.collider.GetComponentInParent<Meeple>();
        }

        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, maxDistance, LevelReferences.s.scrapsItemLayer)) {
            return hit.collider.gameObject.GetComponentInParent<ScrapsItem>();
        }
        
        if (Physics.SphereCast(ray, GetSphereCastRadius(), out hit, maxDistance, LevelReferences.s.genericClickableLayer)) {
            return hit.collider.gameObject.GetComponentInParent<IGenericClickable>();
        }

        return null;
    }

    void RemoveDupes(RaycastHit[] arr, ref int size) {
        Array.Clear(hitSeenObjects, 0, hitSeenObjects.Length);
        var lastLegalIndex = 0;
        for (int i = 0; i < size; i++) {
            var holdable = arr[i].collider.GetComponentInParent<IPlayerHoldable>();
            if (holdable != null && !hitSeenObjects.Contains(holdable)) {
                arr[lastLegalIndex] = arr[i];
                hitSeenObjects[lastLegalIndex] = holdable;
                lastLegalIndex += 1;
            }
        }

        size = lastLegalIndex;
    }
    
    void BubbleSort(RaycastHit[] arr, int size) {
        var source = GetRay().origin;
        for (int i = 0; i < size - 1; i++)
        {
            for (int j = 0; j < size - i - 1; j++) {
                var distj = Vector3.Distance(arr[j].point, source);
                var distj1 = Vector3.Distance(arr[j+1].point, source);
                if (distj > distj1)
                {
                    // Swap array[j] and array[j + 1]
                    (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                }
            }
        }
    }

    void BubbleSort(SnapLocation[] arr, int size) {
        for (int i = 0; i < size - 1; i++)
        {
            for (int j = 0; j < size - i - 1; j++)
            {
                if (arr[j].curDistance > arr[j + 1].curDistance)
                {
                    // Swap array[j] and array[j + 1]
                    (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                }
            }
        }
    }
    
    public bool infoCardActive = false;
    void ShowSelectedThingInfo() {
        if (!infoCardActive) {
            infoCardActive = true;
            if (currentSelectedThing is Artifact artifact) {
                artifactInfoCard.SetUp(artifact);
            }else if ( currentSelectedThing is Cart cart) {
                buildingInfoCard.SetUp(cart);
            }else if (currentSelectedThing is EnemyHealth enemyHealth) {
                enemyInfoCard.SetUp(enemyHealth);
            }else if (currentSelectedThing is Meeple meeple) {
                meeple.GetClicked();
                infoCardActive = false;
            } else if (currentSelectedThing is IGenericClickable genericClickable) {
                genericClickable.Click();
            }else {
                infoCardActive = false;
            }

            if(PlayStateMaster.s.isCombatInProgress())
                TimeController.s.SetTimeSlowForDetailScreen(true);
            //SFX
            AudioManager.PlayOneShot(SfxTypes.OnInfoSelected);
        } 
    }

    void HideInfo() {
        infoCardActive = false;
        buildingInfoCard.Hide();
        enemyInfoCard.Hide();
        artifactInfoCard.Hide();
        
        TimeController.s.SetTimeSlowForDetailScreen(false);
    }

    [Button]
    public void Deselect() {
        if (currentSelectedThing != null && currentSelectedThingMonoBehaviour != null && currentSelectedThingMonoBehaviour.gameObject != null) {
            var temp = currentSelectedThing;
            currentSelectedThing = null;
            SelectThing(temp, false);
        }
        HideInfo();
    }


    public void DirectControlSelectEnemy(EnemyHealth enemy, bool isSelecting, bool showShowDetails = true) {
        if(isSelecting && enemy == (EnemyHealth)currentSelectedThing)
            return;
        
        Deselect();

        HighlightEffect outline = null;
        if(enemy != null)
            outline = enemy.GetComponentInChildren<HighlightEffect>();
        
        if (isSelecting) {
            if(showShowDetails)
                GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
            currentSelectedThing = enemy;
        } else {
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.showDetails);
        }

        if (outline != null) {
            outline.highlighted = isSelecting;
        }

        OnSelectSomething?.Invoke(enemy, isSelecting);
    }
    
    Color SelectEnemy(EnemyHealth enemy) {
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
        return enemyColor;
    }
    
    Color SelectArtifact(Artifact artifact) {

        Color myColor = moveColor;
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
        if (!CanDragArtifact(artifact)) {
            myColor = cantActColor;
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
        }
        
        /*if (PlayStateMaster.s.isShopOrEndGame()) {
            GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
            GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
            if (!CanDragArtifact(artifact)) {
                myColor = cantActColor;
                GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
            }
        } else {
            myColor = cantActColor;
            GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);
        }*/

        return myColor;
    }
    
    Color SelectMeeple(Meeple meeple) {
        return meepleColor;
    }
    void DeselectMeeple(Meeple meeple) { }
    
    Color SelectGenericClickable(IGenericClickable clickable) {
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.clickGate);
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.showDetails);
        return clickableColor;
    }
    void DeselectGenericClickable(IGenericClickable clickable) { }

    Color SelectScrapsItem(ScrapsItem scrapsItem) {
        return scrapsItem.GetSelectColor();
    }
    void DeselectScrapsItem(ScrapsItem scrapsItem) { }

    [HideInInspector]
    public UnityEvent<IPlayerHoldable, bool> OnSelectSomething = new UnityEvent<IPlayerHoldable, bool>();
    void SelectThing(IPlayerHoldable newSelectableThing, bool isSelecting) {
        Deselect();

        if (newSelectableThing == null) {
            return;
        }
        
        HighlightEffect outline = null;
        outline = ((MonoBehaviour)newSelectableThing).GetComponentInChildren<HighlightEffect>();

        if (isSelecting) {
            
            GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.showDetails);

            Color myColor = cantActColor;
            if (newSelectableThing is Artifact artifact) {
                myColor = SelectArtifact(artifact);
            }else if ( newSelectableThing is Cart cart) {
                myColor = SelectCart(cart);
            }else if (newSelectableThing is EnemyHealth enemyHealth) {
                myColor = SelectEnemy(enemyHealth);
            }else if (newSelectableThing is Meeple meeple) {
                myColor = SelectMeeple(meeple);
            }else if (newSelectableThing is ScrapsItem scrapsItem) {
                myColor = SelectScrapsItem(scrapsItem);
            }else if (newSelectableThing is IGenericClickable genericClickable) {
                myColor = SelectGenericClickable(genericClickable);
            }

            if (outline != null) {
                outline.outlineColor = myColor;
            }
            
            currentSelectedThing = newSelectableThing;
        } else {
            if (newSelectableThing is Cart cart) {
                DeselectCart(cart);
            }

            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.showDetails);
            
            
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.repairControl);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.reloadControl);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.gunControl);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.engineControl);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.shieldControl);
        }
        

        if (outline != null) {
            outline.highlighted = isSelecting;
        }

        OnSelectSomething?.Invoke(newSelectableThing, isSelecting);
    }

    Color SelectCart(Cart selectedCart) {
        Color myColor = cantActColor;
        

        myColor = moveColor;
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
        

        if (!CanDragCart(selectedCart)) {
            myColor = cantActColor;
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
        }

        if (PlayStateMaster.s.isCombatInProgress() && selectedCart.IsAttachedToTrain()) {
            if (selectedCart.isDestroyed) {
                myColor = destroyedColor;
            } else {

                var directControllable = selectedCart.GetComponentInChildren<IDirectControllable>();
                if (directControllable != null) {
                    myColor = directControllable.GetHighlightColor();
                    GamepadControlsHelper.s.AddPossibleActions(directControllable.GetActionKey());
                }

                /*switch (currentSelectMode) {
                    case SelectMode.cart:
                        // do nothing. This will do the default action button
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
                }*/
            }
        }

        var ranges = selectedCart.GetComponentsInChildren<RangeVisualizer>();
        for (int i = 0; i < ranges.Length; i++) {
            ranges[i].ChangeVisualizerEdgeShowState(true);
        }


        foreach (var artifactSlot in selectedCart.myArtifactLocations) {
            artifactSlot.SetVisualizeState(PlayStateMaster.s.isShopOrEndGame());
        }
        
        selectedCart.GetHealthModule().SetHealthBarState(true);

        return myColor;
    } 
    
    void DeselectCart(Cart deselectedCart) {
         var ranges = deselectedCart.GetComponentsInChildren<RangeVisualizer>();
         for (int i = 0; i < ranges.Length; i++) {
             ranges[i].ChangeVisualizerEdgeShowState(false);
         }
         
         foreach (var artifactSlot in deselectedCart.myArtifactLocations) {
             artifactSlot.SetVisualizeState(false);
         }

         deselectedCart.GetHealthModule().SetHealthBarState(false);
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
    public Transform GetUITargetTransform();
    public void SetHoldingState(bool state);
}


public interface IGenericClickable : IPlayerHoldable {
    public void Click();
}