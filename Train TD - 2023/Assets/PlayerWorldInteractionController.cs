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
using UnityEngine.UI;


public class PlayerWorldInteractionController : MonoBehaviour { 
    public static PlayerWorldInteractionController s;
    private void Awake() {
        s = this;
    }

    private void OnDestroy() {
        s = null;
    }

    
    public IPlayerHoldable currentSelectedThing;
    MonoBehaviour currentSelectedThingMonoBehaviour => currentSelectedThing as MonoBehaviour;
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
        get { return _canSelect; }
        set { SetCannotSelect(value); }
    }

    public bool canSmith = true;
    
    public void ResetValues() {
        canSmith = true;
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
    public Color destroyedColor = Color.black;
    public Color moveColor = Color.blue;
    public Color reloadColor = Color.yellow;
    public Color directControlColor = Color.magenta;
    public Color engineBoostColor = Color.red;
    public Color repairColor = Color.green;
    public Color enemyColor = Color.white;
    public Color meepleColor = Color.white;

    private void Update() {
        if (!canSelect || Pauser.s.isPaused || PlayStateMaster.s.isLoading || DirectControlMaster.s.directControlLock > 0) {
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

        CheckAndDoDrag();
        
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
        return isToggleDragStarted || isHoldDragStarted;
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
        return true; 
    }
    bool CanDragCart(Cart cart) {
        return (!cart.isMainEngine && cart.canPlayerDrag) && (PlayStateMaster.s.isShopOrEndGame() || !cart.IsAttachedToTrain());
    }
    
    bool CanDragMeeple(Meeple meeple) {
        return PlayStateMaster.s.isShopOrEndGame();
    }

    bool CanDragThing(IPlayerHoldable thing) {
        if (thing is Artifact artifact) {
            return CanDragArtifact(artifact);
        }else if ( thing is Cart cart) {
            return CanDragCart(cart);
        }else if (thing is EnemyHealth enemyHealth) {
            return false;
        }else if (thing is Meeple meeple) {
            return CanDragMeeple(meeple);
        } else {
            return false;
        }
    }

    public bool isToggleDragStarted = false;
    public bool isHoldDragStarted = false;
    public Vector3 offset;
    void CheckAndDoDrag() {
        if (!isToggleDragStarted) {
            if (clickCart.action.WasPressedThisFrame()) {
                HideInfo();
                if (CanDragThing(currentSelectedThing)) {
                    isHoldDragStarted = true;
                    BeginDrag();
                }
            }

            if (isHoldDragStarted) {
                if (clickCart.action.IsPressed()) {
                    DoDrag();
                } else {
                    EndDrag();
                }
            }
        }
        
        if (!isHoldDragStarted) {
            if (!isToggleDragStarted) {
                if (dragClick.action.WasPerformedThisFrame()) {
                    HideInfo();
                    if (CanDragThing(currentSelectedThing)) {
                        isToggleDragStarted = true;
                        BeginDrag();
                    }
                }
            }else{
                DoDrag();
                
                if (dragClick.action.WasPerformedThisFrame()) {
                    EndDrag();
                }
            }
        }
    }

    void BeginDrag() {
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
        
        currentSelectedThingMonoBehaviour.transform.SetParent(null);

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
    public Artifact displacedArtifact;
    private void DoDrag() {
        if(currentSelectedThing is Cart)
            UpdateOnTrainCartSnapLocationPositions();
        
        var newSnapLoc = GetSnapLoc();

        var shouldSnap = newSnapLoc != null;
        if (isSnapping != shouldSnap) {
            isSnapping = shouldSnap;
            if (isSnapping) {
                AudioManager.PlayOneShot(SfxTypes.OnCargoDrop);
            } else {
                AudioManager.PlayOneShot(SfxTypes.OnCargoDrop2);
                currentSelectedThingMonoBehaviour.transform.SetParent(null);

                if (currentSelectedThing is Cart selectedCart) {
                    Train.s.RemoveCart(selectedCart);
                    ShopStateController.s.AddCartToShop(selectedCart);
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
                    
                    Assert.IsTrue(newSnapLoc.IsEmpty()); // you should not be able to snap carts to full locations in the current implementation
                    
                    newSnapLoc.SnapObject(selectedCart.gameObject);
                    var onTrainIndex = onTrainCartSnapLocations.IndexOf(newSnapLoc);
                    if (onTrainIndex != -1) {
                        Train.s.AddCartAtIndex(onTrainIndex+1, selectedCart);
                        ShopStateController.s.RemoveCartFromShop(selectedCart);
                    } else {
                        Train.s.RemoveCart(selectedCart);
                        ShopStateController.s.AddCartToShop(selectedCart);
                    }
                }else if (currentSelectedThing is Artifact selectedArtifact) {
                    if (displacedArtifact != null) {
                        displacedArtifact.AttachToSnapLoc(currentSnapLoc);
                    }
                    
                    
                    if (newSnapLoc.IsEmpty()) {
                        selectedArtifact.AttachToSnapLoc(newSnapLoc);
                    } else {
                        displacedArtifact = newSnapLoc.GetSnappedObject().GetComponent<Artifact>();
                        if (sourceSnapLocation != null) {
                            displacedArtifact.DetachFromCart();
                        } else {
                            displacedArtifact.AttachToSnapLoc(sourceSnapLocation);
                        }
                        
                        selectedArtifact.AttachToSnapLoc(newSnapLoc);
                    }
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

        SnapLocation closestValidSnapLocation = null;
        var closestDistance = float.MaxValue;
        for (int i = 0; i < LevelReferences.s.allSnapLocations.Count; i++) {
            var thisSnapLoc = LevelReferences.s.allSnapLocations[i];

            if (thisSnapLoc.CanSnap(currentSelectedThing)) {
                var snapPos = thisSnapLoc.transform.position;
                snapPos.y = 0;
                
                var distance = Vector3.Distance(snapPos, checkPos);
                if (distance < thisSnapLoc.snapDistance && distance < closestDistance) {
                    closestDistance = distance;
                    closestValidSnapLocation = thisSnapLoc;
                }
            }
        }

        return closestValidSnapLocation;
    }


    void EndDrag() {
        isHoldDragStarted = false;
        isToggleDragStarted = false;
        
        if (!isSnapping) {
            currentSelectedThing.SetHoldingState(false);
        }
        
        if (PlayStateMaster.s.isShop()) {
            if (currentSelectedThing is Cart || currentSelectedThing is Artifact) {
                ShopStateController.s.SaveCartStateWithDelay();
                Train.s.SaveTrainState();
            }
        }


        if (currentSelectedThing is Cart myCart) {
            if (myCart.IsAttachedToTrain()) {
                if (currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>() != null) {
                    Destroy(currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>());
                }

                currentSelectedThing.GetHoldingDrone()?.StopHoldingThing();
            } else {
                if (currentSelectedThing.GetHoldingDrone() != null) {
                    currentSelectedThingMonoBehaviour.GetComponent<Rigidbody>().isKinematic = true;
                    currentSelectedThingMonoBehaviour.GetComponent<Rigidbody>().useGravity = false;
                }   
            }
        }

        if (currentSelectedThing is Artifact myArtifact) {
            if (myArtifact.isAttached) {
                if (currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>() != null) {
                    Destroy(currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>());
                }
                
                currentSelectedThing.GetHoldingDrone()?.StopHoldingThing();
            } else {
                if (currentSelectedThing.GetHoldingDrone() != null) {
                    currentSelectedThingMonoBehaviour.GetComponent<Rigidbody>().isKinematic = true;
                    currentSelectedThingMonoBehaviour.GetComponent<Rigidbody>().useGravity = false;
                }   
            }
        }
        
            
        var rubbleFollowFloor = currentSelectedThingMonoBehaviour.GetComponent<RubbleFollowFloor>();
        if (rubbleFollowFloor) {
            rubbleFollowFloor.canAttachToFloor = true;
        }
        
        
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

    public Ray GetRay() {
        if (SettingsController.GamepadMode()) {
            return GamepadControlsHelper.s.GetRay();
        } else {
            return LevelReferences.s.mainCam.ScreenPointToRay(GetMousePos());
        }
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
    public MiniGUI_BuildingInfoCard enemyInfoCard;
    public MiniGUI_BuildingInfoCard artifactInfoCard;

    private float alternateClickTime = 0;
    private Vector3 alternateClickPos;
    private bool alternateClickFired;
    
    private float meepleHoldTime = 0;
    void CastRayToOutline() {
        var newSelectable = GetFirstPriorityItem();

        if (newSelectable != null) {
            if (currentSelectedThing is Meeple) {
                meepleHoldTime += Time.deltaTime;
            }
            
            if (newSelectable != currentSelectedThing) {
                SelectThing(newSelectable, true);
                
                // SFX
                AudioManager.PlayOneShot(SfxTypes.OnCargoHover);
                
                meepleHoldTime = 0;
            }else {
                if ((showDetailClick.action.WasPerformedThisFrame() || DragStarted(alternateClick, ref alternateClickTime, ref alternateClickPos, ref alternateClickFired) || meepleHoldTime > 1f)) {
                    ShowSelectedThingInfo();
                }
            }
        } else {
            Deselect();
        }
    }

    private IPlayerHoldable GetFirstPriorityItem() {
        // selection preference: artifact -> cart -> enemy -> meeple
        RaycastHit hit;
        Ray ray = GetRay();
        
        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, 100f, LevelReferences.s.artifactLayer)) {
            return hit.collider.gameObject.GetComponentInParent<Artifact>();
        }
        
        if (Physics.SphereCast(ray, GetSphereCastRadius(), out hit, 100f, LevelReferences.s.buildingLayer)) {
            return hit.collider.gameObject.GetComponentInParent<Cart>();
        }

        if (Physics.SphereCast(ray, GetSphereCastRadius(), out hit, 100f, LevelReferences.s.enemyLayer)) {
            return hit.collider.GetComponentInParent<EnemyHealth>();
        }

        if (Physics.SphereCast(ray, GetSphereCastRadius(true), out hit, 100f, LevelReferences.s.meepleLayer)) {
            return hit.collider.GetComponentInParent<Meeple>();
        }

        return null;
    }

    private bool infoCardActive = false;
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
                meeple.ShowChat();
            } else {
                infoCardActive = false;
            }

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
        if (currentSelectedThing != null) {
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

        return myColor;
    }
    
    Color SelectMeeple(Meeple meeple) {
        return meepleColor;
    }

    void DeselectMeeple(Meeple meeple) {
        
    }

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
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
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
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.move);
        

        if (!CanDragCart(selectedCart)) {
            myColor = cantActColor;
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.move);
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.moveHoldGamepad);
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
    public DroneRepairController GetHoldingDrone();
    public void SetHoldingDrone(DroneRepairController holder);
}


