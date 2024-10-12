using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour {
    public static CameraController s;

    private void Awake() {
        s = this;
    }

    public Camera mainCamera {
        get {
            return LevelReferences.s.mainCam;
        }
    }

    public Transform cameraCornerBottomLeft;
    public Transform cameraCornerTopRight;


    public Transform cameraCenter;
    public Transform cameraOffset;
    public Transform cameraOffsetFlat;

    //public bool isRight = true;

    public float edgeScrollMoveSpeed = 8f;
    public float wasdSpeed = 8f;
    public float gamepadMoveSpeed = 5f;
    public float snappedwasdDelay = 0.3f; 
    public float zoomSpeed = 0.004f;
    public float zoomGamepadSpeed = 0.2f;
    public bool canZoom = true;

    public float posLerpSpeed = 10f;
    public float rotationAngleTarget = 50;
    /*public float minAngle = 0;
    public float maxAngle = 120;*/
    //public float snapToDefaultAngleDistance = 5;
    public float rotLerpSpeed = 8f;
    public float clickRotAngle = 45;
    public float middleMoveSpeed = 20f;
    public float rotateSmoothSpeed = 20;
    public float rotateSmoothGamepadSpeed = 20;

    public InputActionReference moveAction;
    public InputActionReference moveGamepadAction;
    public InputActionReference gamepadSnapMoveForward;
    public InputActionReference gamepadSnapMoveBackward;
    public InputActionReference zoomAction;
    public InputActionReference zoomGamepadAction;
    public InputActionReference rotateCameraAction;
    public InputActionReference aimAction;
    public InputActionReference aimGamepadAction;
    public InputActionReference rotateSmoothAction;
    public InputActionReference rotateSmoothGamepadAction;
    

    public float currentZoom = 0;
    public float realZoom = 0f;
    public Vector2 zoomLimit = new Vector2(-2, 2);
    public Vector2 regularZoomLimits = new Vector2(-2, 2);
    public Vector2 mapViewZoomLimits = new Vector2(-2, 2);

    public float snapZoomCutoff = 2f;

    public bool isSnappedToTrain = false;
    public bool isSnappedToTransform = false;
    public Transform snapTarget;
    public float minSnapDistance = 2f;

    public bool canEdgeMove = false;

    protected void OnEnable()
    {
        moveAction.action.Enable();
        moveGamepadAction.action.Enable();
        zoomAction.action.Enable();
        rotateCameraAction.action.Enable();
        aimAction.action.Enable();
        
        gamepadSnapMoveForward.action.Enable();
        gamepadSnapMoveBackward.action.Enable();
        
        zoomGamepadAction.action.Enable();
        aimGamepadAction.action.Enable();
        
        rotateSmoothAction.action.Enable();
        rotateSmoothGamepadAction.action.Enable();
    }


    protected void OnDisable()
    { 
        moveAction.action.Disable();
        moveGamepadAction.action.Disable();
        zoomAction.action.Disable();
        rotateCameraAction.action.Disable();
        aimAction.action.Disable();
        
        gamepadSnapMoveForward.action.Disable();
        gamepadSnapMoveBackward.action.Disable();
        
        zoomGamepadAction.action.Disable();
        aimGamepadAction.action.Disable();
        
        rotateSmoothAction.action.Disable();
        rotateSmoothGamepadAction.action.Disable();
    }

    public GameObject cameraLerpDummy;
    public GameObject cameraShakeDummy;
    private void Start() {
#if UNITY_EDITOR
        edgeScrollMoveSpeed = 0; // we dont want edge scroll in the editor
#endif
        cameraCenter.transform.rotation = Quaternion.Euler(0, rotationAngleTarget, 0);
        SetMainCamPos();
        DisableDirectControl();

        mainCamera.transform.SetParent(transform);
    }

    private void OnDestroy() {
        if (mainCamera != null && SceneLoader.s != null) {
            mainCamera.transform.SetParent(SceneLoader.s.transform);
        }
    }

    private void Update() {
        if (mainCamera.fieldOfView != targetFOV) {
            mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, targetFOV, fovChangeSpeed * Time.unscaledDeltaTime);
        }
    }

    public void MoveToCharSelectArea() {
        cameraCenter.position = Vector3.back*5;
    }

    
    public  bool cannotSelectButCanMoveOverride = false;
    private bool snappedToTrainLastFrame = false;

    public UnityEvent AfterCameraPosUpdate = new UnityEvent();
    private void LateUpdate() {
        if (!Pauser.s.isPaused) {
            if (directControlActive) {
                ProcessDirectControl(aimAction.action.ReadValue<Vector2>(), aimGamepadAction.action.ReadValue<Vector2>());
                if (allowDirectControlFreeLook) {
                    ProcessVelocityPredictionAndAimAssist();
                }
            } else {
                if (PlayerWorldInteractionController.s.canSelect || cannotSelectButCanMoveOverride) {
                    var mousePos = Mouse.current.position.ReadValue();
                    if (canEdgeMove)
                        ProcessScreenCorners(mousePos);

                    if (SettingsController.GamepadMode() && PlayStateMaster.s.isCombatInProgress() && !isSnappedToTrain) {
                        var gamepadPos = PlayerWorldInteractionController.s.GetMousePositionOnPlane();
                        
                        var closestModule = Train.s.carts[0];
                        var closestDistance = 1000f;
                        for (int i = 0; i < Train.s.carts.Count; i++) {
                            var dist = Vector3.Distance(gamepadPos, Train.s.carts[i].transform.position);
                            if (dist < closestDistance) {
                                closestModule = Train.s.carts[i];
                                closestDistance = dist;
                            }
                        }

                        //print(closestDistance);
                        if (closestDistance < 1) {
                            SnapToTrainModule(closestModule);
                        }
                    }

                    if (SettingsController.GamepadMode() && isSnappedToTrain && !PlayStateMaster.s.isCombatInProgress()) {
                        UnSnap();
                    }
                    
                    if (!isSnappedToTransform) {
                        ProcessMovementInput(moveAction.action.ReadValue<Vector2>(), wasdSpeed);
                        ProcessMovementInput(moveGamepadAction.action.ReadValue<Vector2>(), gamepadMoveSpeed);
                        ProcessMovementGamepadJumpForwardAndBack();
                    } else {
                        //ProcessMovementSnapped(moveAction.action.ReadValue<Vector2>(), snappedwasdDelay);
                        if (isSnappedToTrain) {
                            ProcessMovementSnapped(moveGamepadAction.action.ReadValue<Vector2>(), snappedwasdDelay);
                            ProcessMovementSnappedGamepadForwardBackwards();
                        }
                    }

                    if (canZoom)
                        ProcessZoom(zoomAction.action.ReadValue<float>(), zoomGamepadAction.action.ReadValue<float>());

                    ProcessMiddleMouseRotation(rotateCameraAction.action.ReadValue<float>(), mousePos);
                    ProcessSmoothRotationInput(rotateSmoothAction.action.ReadValue<float>(), rotateSmoothSpeed);
                    ProcessSmoothRotationInput(rotateSmoothGamepadAction.action.ReadValue<float>(), rotateSmoothGamepadSpeed);
                }
                LerpCameraTarget();
            }

            SetMainCamPos();
        }
    }

    [Header("Aim Assist")]
    public float maxAimAssistOffset = 0.09f;
    public float maxAimDistance = 15;
    public float aimAssistStrength = 2;
    public bool velocityAdjustment = true;
    public float minVelocityShowDistance = 5;

    public UIElementFollowWorldTarget realLocation;
    public UIElementFollowWorldTarget velocityTrackedLocation;
    public MiniGUI_LineBetweenObjects miniGUILine;

    void ProcessVelocityPredictionAndAimAssist() {
        var allTargets = LevelReferences.s.allTargetValues;
        var allTargetsReal = LevelReferences.s.allTargets;
        var targetCount = LevelReferences.s.targetValuesCount;
 
        var myPosition =  mainCamera.transform.position;
        var myForward = mainCamera.transform.forward;
        
        
        //var center = new Vector3(-20,0,0);
        //print(center);

        var curDistance = maxAimAssistOffset;
        var curDifference = Vector3.zero;
        var curTargetRealLocation = Vector3.zero;
        var curTargetVelocityLocation = Vector3.zero;
        bool hasTarget = false;

        if(targetCount != allTargetsReal.Count)
            return;
        
        for (int i = 0; i < targetCount; i++) {
            if(allTargets[i].type != PossibleTarget.Type.enemy)
                continue;

            var targetDistance = Vector3.Distance(allTargets[i].position, myPosition);
            var targetRealLocation = allTargets[i].position;
            var targetLocation = targetRealLocation;
            if (velocityAdjustment) {
                targetLocation += allTargetsReal[i].velocity * targetDistance * 0.05f;
            }
            
            //Debug.DrawLine( targets[i].targetTransform.position, targetLocation);
            var vectorToEnemy = targetLocation - myPosition;

            var distance = vectorToEnemy.magnitude;
            
            if(distance > maxAimDistance)
                continue;

            vectorToEnemy.Normalize();
 
            // essentially the tangent vector between MyForward and the line to the enemy
            var difference = vectorToEnemy - myForward;
 
            // how big is that offset along the sphere surface
            float vectorOffset = difference.magnitude;

            var distanceAimConeAdjustment = distance.Remap(0.5f, 2f, 0.5f, 1f);
            distanceAimConeAdjustment = Mathf.Clamp(distanceAimConeAdjustment, 0.5f, 1f);
 
            // find the closest target only
            if (vectorOffset/distanceAimConeAdjustment < curDistance) {
                curDistance = vectorOffset;
                curDifference = difference;
                curTargetRealLocation = targetRealLocation;
                curTargetVelocityLocation = targetLocation;
                hasTarget = true;
            }
        }

        // if it is within our auto-aim MaxVectorOffset, we care
        if (hasTarget) {
            ShowVelocityTracking(curTargetRealLocation, curTargetVelocityLocation);

            // do aim assist
            if (SettingsController.GamepadMode()) {
                // transform it to local offset X,Y plane
                var localDifference = mainCamera.transform.InverseTransformDirection(curDifference);

                // normalize it to full deflection
                localDifference /= maxAimAssistOffset;

                // scale it according to conical offset from boresight (strongest in middle)
                float conicalStrength = (maxAimAssistOffset - curDistance) / maxAimAssistOffset;
                localDifference *= conicalStrength;

                // send it to the aim assist injection point
                ProcessDirectControl(localDifference * aimAssistStrength);
            }
        } else { 
            DisableVelocityTracking();   
        }
    }

    void DisableVelocityTracking() {
        realLocation.gameObject.SetActive(false);
        velocityTrackedLocation.gameObject.SetActive(false);
        miniGUILine.gameObject.SetActive(false);
    }

    void ShowVelocityTracking(Vector3 realLoc, Vector3 velocityLoc) {
        realLocation.gameObject.SetActive(true);

        realLocation.UpdateTarget(realLoc);

        var distance = (realLoc - velocityLoc).magnitude;

        //print(distance);
        if (distance > minVelocityShowDistance) {
            velocityTrackedLocation.gameObject.SetActive(true);
            miniGUILine.gameObject.SetActive(true);
            
            velocityTrackedLocation.UpdateTarget(velocityLoc);
            miniGUILine.SetObjects(realLocation.gameObject, velocityTrackedLocation.gameObject);
        } else {
            velocityTrackedLocation.gameObject.SetActive(false);
            miniGUILine.gameObject.SetActive(false);
        }
    }

    public void SetCameraControllerStatus(bool active) {
        enabled = active;
        if (active) {
            SetMainCamPos();
        } 
    }

    private Vector2 mousePosLastFrame;
    private void ProcessMiddleMouseRotation(float click, Vector2 mousePos) {
        if (click > 0.5f) {
            var delta = mousePosLastFrame.x-mousePos.x;
            /*if (!isRight)
                delta = -delta;*/
            rotationAngleTarget += delta * middleMoveSpeed * Time.unscaledDeltaTime;

            //isRight = rotationAngleTarget > 0;

            //rotationAngleTarget = Mathf.Clamp(rotationAngleTarget, minAngle, maxAngle);
        } /*else {
            if (Mathf.Abs(Mathf.Abs(rotationAngleTarget) - rotationAngle) < snapToDefaultAngleDistance) {
                rotationAngleTarget = Mathf.MoveTowards(rotationAngleTarget, isRight? rotationAngle : -rotationAngle, 10 * Time.unscaledDeltaTime);
            }
        }*/

        mousePosLastFrame = mousePos;
    }

    void ProcessSmoothRotationInput(float value, float multiplier) {
        var delta = value*multiplier;
        rotationAngleTarget += delta * Time.unscaledDeltaTime;
    }

    private void LerpCameraTarget() {
        //transform.position = Train.s.trainMiddle.transform.position;

        if (isSnappedToTransform) {
            if (snapTarget == null) {
                UnSnap();
            } else {
                var snapPos = snapTarget.position;
                snapPos.y = cameraCenter.position.y;
                cameraCenter.position = Vector3.Lerp(cameraCenter.position, snapPos, snappedMovementLerp * Time.unscaledDeltaTime);
            }
        }

        //var centerRotTarget = Quaternion.Euler(0, isRight ? -rotationAngleTarget : rotationAngleTarget, 0);
        var centerRotTarget = Quaternion.Euler(0, -rotationAngleTarget, 0);

        cameraCenter.transform.rotation = Quaternion.Lerp(cameraCenter.transform.rotation, centerRotTarget, rotLerpSpeed * Time.unscaledDeltaTime);

        var targetPos = cameraOffset.position + cameraOffset.forward * currentZoom;
        var targetRot = cameraOffset.rotation;
        cameraLerpDummy.transform.position = Vector3.Lerp(cameraLerpDummy.transform.position, targetPos, posLerpSpeed * Time.unscaledDeltaTime);
        cameraLerpDummy.transform.rotation = Quaternion.Lerp(cameraLerpDummy.transform.rotation, targetRot, rotLerpSpeed * Time.unscaledDeltaTime);

        // lerp affected real zoom
        realZoom = Vector3.Distance(cameraLerpDummy.transform.position, cameraOffset.position);
        if (currentZoom < 0)
            realZoom = -realZoom;
    }

    public void SetMainCamPos() {
        mainCamera.transform.position = cameraShakeDummy.transform.position;
        mainCamera.transform.rotation = cameraShakeDummy.transform.rotation;
        AfterCameraPosUpdate?.Invoke();
    }

    void ProcessZoom(float value, float gamepadValue) {
        currentZoom += value * zoomSpeed + gamepadValue*zoomGamepadSpeed;
        currentZoom = Mathf.Clamp(currentZoom, zoomLimit.x, zoomLimit.y);
        if (Mathf.Abs(value) > 0.1) {
            CancelInvoke(nameof(SnapZoom));
            Invoke(nameof(SnapZoom), 0.7f);
        }
    }

    void SnapZoom() {
        if (Mathf.Abs(currentZoom) < 1.25) {
            currentZoom = 0;
        }

        if (Mathf.Abs(currentZoom - snapZoomCutoff) < 1.25) {
            currentZoom = snapZoomCutoff;
        }
    }

    public float cameraScrollEdgePercent = 0.01f;
    void ProcessScreenCorners(Vector2 mousePosition) {
        float xPercent = mousePosition.x / Screen.width;
        float yPercent = mousePosition.y / Screen.height;
        var scrollXReq = cameraScrollEdgePercent;
        var scrollYReq = cameraScrollEdgePercent * Screen.height / Screen.width;

        var output = Vector2.zero;

        if (xPercent < scrollXReq) {
            output.x = -1f;
        }else if (xPercent > 1-scrollXReq) {
            output.x = 1f;
        }
        
        if (yPercent < scrollYReq) {
            output.y = -1f;
        }else if (yPercent > 1-scrollYReq) {
            output.y = 1f;
        }
        
        ProcessMovementInput(output, edgeScrollMoveSpeed);
    }


    public void ResetCameraPos() {
        regularPos = Vector3.zero;
        regularZoom = 0;
        
        cameraCenter.localPosition = regularPos;
        currentZoom = regularZoom;
    }

    [Header("Map Settings")]
    
    public bool isSnappedToMap = false;
    public Transform mapCameraCornerBottomLeft;
    public Transform mapCameraCornerTopRight;
    public Vector3 mapStartPos = new Vector3(100, 0, -100);
    public Vector3 mapPos;
    public Transform mapCameraSnapPos;
    private Vector3 regularPos;
    public float mapZoom;
    private float regularZoom;
    public float mapAngle = 50;
    public float regularAngle;

    public void EnterMapMode() {
        isSnappedToMap = true;
        regularPos = cameraCenter.localPosition;
        cameraCenter.transform.position = mapCameraSnapPos.transform.position;
        regularZoom = currentZoom;
        currentZoom = mapZoom;
        regularAngle = rotationAngleTarget;
        rotationAngleTarget = mapAngle;
        zoomLimit = mapViewZoomLimits;
        /*if (!isRight) {
            FlipCamera(new InputAction.CallbackContext());
        }*/
        
        DepthOfFieldController.s.SetDepthOfField(false);
        SetMainCamPos();
    }

    public void ExitMapMode() {
        isSnappedToMap = false;
        mapPos = cameraCenter.localPosition;
        cameraCenter.localPosition = regularPos;
        mapZoom = currentZoom;
        currentZoom = regularZoom;
        mapAngle = rotationAngleTarget;
        rotationAngleTarget = regularAngle;
        zoomLimit = regularZoomLimits;
        /*if (!isRight) {
            FlipCamera(new InputAction.CallbackContext());
        }*/
        
        DepthOfFieldController.s.SetDepthOfField(true);
        SetMainCamPos();
    }

    void ProcessMovementInput(Vector2 value, float multiplier) {
        var delta = new Vector3(value.x, 0, value.y);
        
        var transformed = cameraOffsetFlat.TransformDirection(delta);
        
        
        var camPos = cameraCenter.position;
        
        camPos += transformed * multiplier * Time.unscaledDeltaTime;

        Vector3 topRight = isSnappedToMap ? mapCameraCornerTopRight.position : cameraCornerTopRight.position;
        Vector3 bottomLeft = isSnappedToMap ? mapCameraCornerBottomLeft.position : cameraCornerBottomLeft.position;
        
        if (!isSnappedToMap) {
            var len = ((Train.s.carts.Count-3) * DataHolder.s.cartLength)/2;
            topRight.z += len;
            bottomLeft.z -= len;
        }
        
        if (camPos.x < bottomLeft.x) {
            camPos = new Vector3(bottomLeft.x, camPos.y, camPos.z);
        }else if (camPos.x > topRight.x) {
            camPos = new Vector3(topRight.x, camPos.y, camPos.z);
        }
        
        if (camPos.z < bottomLeft.z) {
            camPos = new Vector3(camPos.x, camPos.y, bottomLeft.z);
        }else if (camPos.z > topRight.z) {
            camPos = new Vector3(camPos.x, camPos.y, topRight.z);
        }

        cameraCenter.position = camPos;
    }


    Cart GetSnappedCart() {
        if (snappedCartIndex >= 0 && snappedCartIndex < Train.s.carts.Count) {
            return Train.s.carts[snappedCartIndex];
        }

        return null;
    }

    private float snappedMoveTimer = 0;
    public float snappedMovementLerp = 1f;
    private float snappedDetachTimer = 0;
    void ProcessMovementSnapped(Vector2 value, float delay) {
        Cart snappedCart = GetSnappedCart();
        if (snappedCart == null) {
            snapTarget = null;
        } else {
            snapTarget = snappedCart.transform;
        }
        
        if (snapTarget == null) {
            UnSnap();
            return;
        }
        

        if (isSnappedToTrain) {
            if (snappedMoveTimer <= 0) {
                var trainForward = Train.s.GetTrainForward();
                var trainForwardTransformed = cameraCenter.InverseTransformDirection(trainForward);
                var trainForward2d = new Vector2(trainForwardTransformed.x, trainForwardTransformed.z);
                /*if (Mathf.Abs(value.x) > 0.1f) {
                    snappedDetachTimer += Time.deltaTime;

                    if (snappedDetachTimer > 0.2f) {
                        UnSnap();
                        Vector3 delta;
                        if (value.x > 0) {
                            delta = Vector3.right;
                        } else {
                            delta = Vector3.left;
                        }
                        
                        cameraCenter.position += delta*1.5f;
                    }
                } else {
                    snappedDetachTimer = 0;
                }*/

                if (value.magnitude > 0.1f) {

                    var isForward = Vector2.Dot(trainForward2d, value) > 0;
                    
                    var nextBuilding = GetNextCart(isForward, snappedCart);
                    if (nextBuilding != null) {
                        SnapToTrainModule(nextBuilding);
                    } else {
                        UnSnapAndJoltForward(isForward);
                        
                    }
                    
                    PlayerWorldInteractionController.s.MoveSelectedCart(isForward);
                    
                    snappedMoveTimer = delay;
                }
            }
        } else {
            isSnappedToTransform = false;
        }

        if (value.magnitude < 0.05f) {
            snappedMoveTimer = 0;
        }

        snappedMoveTimer -= Time.unscaledDeltaTime;
    }

    void ProcessMovementSnappedGamepadForwardBackwards() {
        Cart snappedCart = GetSnappedCart();
        if (snappedCart == null) {
            snapTarget = null;
        } else {
            snapTarget = snappedCart.transform;
        }
        
        if (snapTarget == null) {
            UnSnap();
            return;
        }

        if (isSnappedToTrain) {
            if (gamepadSnapMoveForward.action.WasPerformedThisFrame()) {
                var nextBuilding = GetNextCart(true, snappedCart);
                if (nextBuilding != null) {
                    SnapToTrainModule(nextBuilding);
                } else {
                    UnSnapAndJoltForward(true);
                }
                PlayerWorldInteractionController.s.MoveSelectedCart(true);
            }

            if (gamepadSnapMoveBackward.action.WasPerformedThisFrame()) {
                var nextBuilding = GetNextCart(false, snappedCart);
                if (nextBuilding != null) {
                    SnapToTrainModule(nextBuilding);
                } else {
                    UnSnapAndJoltForward(false);
                }
                PlayerWorldInteractionController.s.MoveSelectedCart(false);
            }
        } else {
            isSnappedToTransform = false;
        }
    }

    void ProcessMovementGamepadJumpForwardAndBack() {
        if (gamepadSnapMoveForward.action.WasPerformedThisFrame()) {
            Vector3 delta = Vector3.forward * 0.55f;
            var lerpDummyPos = cameraLerpDummy.transform.position;
            cameraCenter.position += delta;
            cameraLerpDummy.transform.position = lerpDummyPos;
        }

        if (gamepadSnapMoveBackward.action.WasPerformedThisFrame()) {
            Vector3 delta = Vector3.back * 0.55f;
            var lerpDummyPos = cameraLerpDummy.transform.position;
            cameraCenter.position += delta;
            cameraLerpDummy.transform.position = lerpDummyPos;
        }
    }

    

    private Cart GetNextCart(bool isForward, Cart currentCart) {
        return Train.s.GetNextBuilding(isForward ? 1 : -1, currentCart);
    }
    

    /*public bool SnapToNearestCart() {
        var carts = Train.s.carts;
        var minDist = float.MaxValue;
        if (!snappedToTrainLastFrame) {
            targetCart = -1;

            for (int i = 0; i < carts.Count; i++) {
                var dist = Vector3.Distance(cameraCenter.position, carts[i].position);

                if (dist < minDist) {
                    targetCart = i;
                    minDist = dist;
                }
            }
        }
        
        return minDist < minSnapDistance;
    }*/

    public int snappedCartIndex;
    public void SnapToTrainModule(Cart module) {
        snappedCartIndex = module.trainIndex;
        snapTarget = module.transform;
        isSnappedToTransform = true;
        isSnappedToTrain = true;
    }
    
    public void SnapToTransform(Transform target) {
        snapTarget = target;
        isSnappedToTransform = true;
        isSnappedToTrain = false;
    }

    public void UnSnap() {
        isSnappedToTransform = false;
        isSnappedToTrain = false;
        snappedDetachTimer = 0;
    }

    public void UnSnapAndJoltForward(bool isForward) {
        isSnappedToTransform = false;
        isSnappedToTrain = false;
        snappedDetachTimer = 0;
        /*var trainForward = Train.s.GetTrainForward();
        var delta = trainForward * (isForward ? 1 : -1);
        cameraCenter.position += delta.normalized*1.5f;*/
    }

    public void ToggleCameraEdgeMove() {
        canEdgeMove = !canEdgeMove;
    }

    [Header("Direct Control Settings")] 
    public bool directControlActive = false;
    public Transform directControlTransform;
    private Vector2 rotTarget;
    private Vector2 freeLookDelta;
    public float mouseSensitivity = 1f;
    public float gamepadSensitivity = 1f;
    public float overallSensitivity = 1f;
    public bool posLerping = true;
    public bool rotLerping = true;
    public bool allowDirectControlFreeLook;
    public float directControlMaxNonFreeLook = 8f;
    public float directControlNonFreeLookSensitivityMultiplier = 0.35f;


    public void ProcessDirectControl(Vector2 mouseInput, Vector2 gamepadInput) {
        var processedInput = mouseInput * mouseSensitivity + (gamepadInput * gamepadSensitivity * 35);
        
        processedInput *= /*Time.unscaledTime **/ (overallSensitivity / 2.5f) / 45f;

        ProcessDirectControl(processedInput);
    }
    
    public void ProcessDirectControl(Vector2 processedInput) {
        if (allowDirectControlFreeLook) {
            rotTarget += processedInput;
        } else {
            processedInput *= directControlNonFreeLookSensitivityMultiplier;
            freeLookDelta += processedInput;
            
            freeLookDelta = Vector2.ClampMagnitude(freeLookDelta, directControlMaxNonFreeLook+20);
            var clampedLookDelta = Vector2.ClampMagnitude(freeLookDelta, directControlMaxNonFreeLook);
            freeLookDelta = Vector2.Lerp(freeLookDelta, clampedLookDelta, 10 * Time.unscaledDeltaTime);
            
            rotTarget = new Vector2(directControlTransform.rotation.eulerAngles.y, -directControlTransform.rotation.eulerAngles.x) + freeLookDelta;
        }
        

        Quaternion xQuaternion = Quaternion.AngleAxis (rotTarget.x, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis (rotTarget.y, -Vector3.right);
        
        var targetPos = directControlTransform.position;
        var targetRot = /*directControlTransform.rotation **/ xQuaternion * yQuaternion;
        
        if (posLerping) {
            cameraLerpDummy.transform.position = Vector3.Lerp(cameraLerpDummy.transform.position, targetPos, posLerpSpeed * Time.unscaledDeltaTime);
            if (Vector3.Distance(cameraLerpDummy.transform.position, targetPos) < 0.1f) {
                posLerping = false;
            }
        } else {
            cameraLerpDummy.transform.position = targetPos;
        }

        if (rotLerping) {
            cameraLerpDummy.transform.rotation = Quaternion.Lerp(cameraLerpDummy.transform.rotation, targetRot, rotLerpSpeed * Time.unscaledDeltaTime);
            if (Quaternion.Angle(cameraLerpDummy.transform.rotation, targetRot) < 10) {
                rotLerping = false;
            }
        } else {
            cameraLerpDummy.transform.rotation = targetRot;
        }
    }
    
    public void ActivateDirectControl(Transform target, bool allowFreeLook, bool lockCursor) {
        //UnSnap();
        directControlTransform = target;
        directControlActive = true;
        rotTarget = new Vector2(target.rotation.eulerAngles.y, -target.rotation.eulerAngles.x);
        freeLookDelta = Vector2.zero;
        rotLerping = true;
        posLerping = true;
        allowDirectControlFreeLook = allowFreeLook;
        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ChangeDirectControlTransformWithoutChangingCurrentRotation(Transform target) {
        directControlTransform = target;
    }

    public void DisableDirectControl() {
        directControlActive = false;
        Cursor.lockState = CursorLockMode.None;
        
        realLocation.gameObject.SetActive(false);
        velocityTrackedLocation.gameObject.SetActive(false);
        miniGUILine.gameObject.SetActive(false);
    }

    public void ManualRotateDirectControl(float amount) {
        rotTarget.x += amount/1.5f;
    }


    [Header("Boost FOV Settings")]
    public float boostFOV = 62;
    public float regularFOV = 60;
    public float slowFOV = 58;
    public float targetFOV = 60;
    public float fovChangeSpeed = 1f;
    public void BoostFOV() {
        targetFOV = boostFOV;
    }

    public void SlowFOV() {
        targetFOV = slowFOV;
    }

    public void ReturnToRegularFOV() {
        targetFOV = regularFOV;
    }
}
