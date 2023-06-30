using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

    public bool isRight = true;

    public float edgeScrollMoveSpeed = 8f;
    public float wasdSpeed = 8f;
    public float gamepadMoveSpeed = 4f;
    public float snappedwasdDelay = 0.5f; 
    public float zoomSpeed = 0.004f;
    public float zoomGamepadSpeed = 1f;
    public bool canZoom = true;
    public float middleMoveSpeed = 2f;

    public float posLerpSpeed = 1f;
    public float rotationAngle = 50;
    public float rotationAngleTarget = 50;
    public float minAngle = 0;
    public float maxAngle = 120;
    public float snapToDefaultAngleDistance = 5;
    public float rotLerpSpeed = 20f;

    public InputActionReference moveAction;
    public InputActionReference moveGamepadAction;
    public InputActionReference rotateAction;
    public InputActionReference zoomAction;
    public InputActionReference zoomGamepadAction;
    public InputActionReference rotateCameraAction;
    public InputActionReference aimAction;
    public InputActionReference aimGamepadAction;
    

    public float currentZoom = 0;
    public float realZoom = 0f;
    public Vector2 zoomLimit = new Vector2(-2, 2);

    public float snapZoomCutoff = 2f;

    public bool isSnappedToTrain = false;
    public bool isSnappedToTransform = false;
    public Transform snapTarget;
    public float minSnapDistance = 2f;

    public bool canEdgeMove = false;

    public bool cannotSelectButCanMoveOverride;

    protected void OnEnable()
    {
        moveAction.action.Enable();
        moveGamepadAction.action.Enable();
        rotateAction.action.Enable();
        zoomAction.action.Enable();
        rotateCameraAction.action.Enable();
        aimAction.action.Enable();
        
        zoomGamepadAction.action.Enable();
        aimGamepadAction.action.Enable();
        
        rotateAction.action.performed += FlipCamera;
    }


    protected void OnDisable()
    { 
        moveAction.action.Disable();
        moveGamepadAction.action.Disable();
        rotateAction.action.Disable();
        zoomAction.action.Disable();
        rotateCameraAction.action.Disable();
        aimAction.action.Disable();
        
        zoomGamepadAction.action.Disable();
        aimGamepadAction.action.Disable();
        
        rotateAction.action.performed -= FlipCamera;
    }

    public GameObject cameraLerpDummy;
    public GameObject cameraShakeDummy;
    private void Start() {
#if UNITY_EDITOR
        edgeScrollMoveSpeed = 0; // we dont want edge scroll in the editor
#endif
        cameraCenter.transform.rotation = Quaternion.Euler(0, isRight ? -rotationAngleTarget : rotationAngleTarget, 0);
        SetMainCamPos();
        DisableDirectControl();
    }

    private void Update() {
        if (mainCamera.fieldOfView != targetFOV) {
            mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
        }
    }

    private bool snappedToTrainLastFrame = false;

    public UnityEvent AfterCameraPosUpdate = new UnityEvent();
    private void LateUpdate() {
        if (!Pauser.s.isPaused) {
            if (directControlActive) {
                ProcessDirectControl(aimAction.action.ReadValue<Vector2>(), aimGamepadAction.action.ReadValue<Vector2>());
                ProcessVelocityPredictionAndAimAssist();
            } else {
                if (PlayerWorldInteractionController.s.canSelect || cannotSelectButCanMoveOverride) {
                    var mousePos = Mouse.current.position.ReadValue();
                    if (canEdgeMove)
                        ProcessScreenCorners(mousePos);

                    if (!isSnappedToTransform) {
                        ProcessMovementInput(moveAction.action.ReadValue<Vector2>(), wasdSpeed);
                        ProcessMovementInput(moveGamepadAction.action.ReadValue<Vector2>(), gamepadMoveSpeed);
                    } else
                        ProcessMovementSnapped(moveAction.action.ReadValue<Vector2>(), snappedwasdDelay);

                    if (canZoom)
                        ProcessZoom(zoomAction.action.ReadValue<float>(), zoomGamepadAction.action.ReadValue<float>());

                    ProcessMiddleMouseRotation(rotateCameraAction.action.ReadValue<float>(), mousePos);

                    LerpCameraTarget();
                }
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
        var targets = EnemyWavesController.s.allEnemyTargetables;
 
        var myPosition =  mainCamera.transform.position;
        var myForward = mainCamera.transform.forward;
        
        
        //var center = new Vector3(-20,0,0);
        //print(center);

        var curDistance = maxAimAssistOffset;
        var curDifference = Vector3.zero;
        var curTargetRealLocation = Vector3.zero;
        var curTargetVelocityLocation = Vector3.zero;
        bool hasTarget = false;

        for (int i = 0; i < targets.Length; i++) {
            if(targets[i] == null || targets[i].gameObject == null || targets[i].myType != PossibleTarget.Type.enemy)
                continue;

            var targetDistance = Vector3.Distance(targets[i].targetTransform.position, myPosition);
            var targetRealLocation = targets[i].targetTransform.position;
            var targetLocation = targetRealLocation;
            if (velocityAdjustment) {
                targetLocation += targets[i].velocity * targetDistance * 0.05f;
            }
            
            Debug.DrawLine( targets[i].targetTransform.position, targetLocation);
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

            isRight = rotationAngleTarget > 0;

            rotationAngleTarget = Mathf.Clamp(rotationAngleTarget, minAngle, maxAngle);
        } else {
            if (Mathf.Abs(Mathf.Abs(rotationAngleTarget) - rotationAngle) < snapToDefaultAngleDistance) {
                rotationAngleTarget = Mathf.MoveTowards(rotationAngleTarget, isRight? rotationAngle : -rotationAngle, 10 * Time.unscaledDeltaTime);
            }
        }

        mousePosLastFrame = mousePos;
    }

    private void LerpCameraTarget() {
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
        
        cameraCenter.position = regularPos;
        currentZoom = regularZoom;
    }

    [Header("Map Settings")]
    
    public bool isSnappedToMap = false;
    public Transform mapCameraCornerBottomLeft;
    public Transform mapCameraCornerTopRight;
    public Vector3 mapStartPos = new Vector3(100, 0, -100);
    public Vector3 mapPos;
    private Vector3 regularPos;
    private float mapZoom;
    private float regularZoom;

    public void ResetMapPos() {
        mapPos = mapStartPos;
        mapZoom = 0;
    }

    public void SetMapPos(Vector3 position) {
        position.y = 0;
        mapPos = position;
    }

    public void EnterMapMode() {
        isSnappedToMap = true;
        regularPos = cameraCenter.position;
        cameraCenter.position = mapPos;
        regularZoom = currentZoom;
        currentZoom = mapZoom;
        if (!isRight) {
            FlipCamera(new InputAction.CallbackContext());
        }
        
        SetMainCamPos();
    }

    public void ExitMapMode() {
        isSnappedToMap = false;
        mapPos = cameraCenter.position;
        cameraCenter.position = regularPos;
        mapZoom = currentZoom;
        currentZoom = regularZoom;
        if (!isRight) {
            FlipCamera(new InputAction.CallbackContext());
        }
        
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
            var len = ((Train.s.cartCount-3) * DataHolder.s.cartLength)/2;
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
    
    

    private float snappedMoveTimer = 0;
    public float snappedMovementLerp = 1f;
    void ProcessMovementSnapped(Vector2 value, float delay) {
        if (snapTarget == null) {
            UnSnap();
            return;
        }
        
        cameraCenter.position = Vector3.Lerp(cameraCenter.position, snapTarget.position + snapOffset, snappedMovementLerp*Time.unscaledDeltaTime);

        if (isSnappedToTrain) {
            if (snappedMoveTimer <= 0) {
                if (Mathf.Abs(value.x) > 0.1f) {
                    //var nextBuilding = GetNextBuilding(value.x > 0, snappedTrainBuilding.mySlot, snappedTrainBuilding.mySlotIndex);
                    var nextBuilding = GetNextBuilding();
                    if (nextBuilding != null) {
                        SnapToTrainModule(nextBuilding);
                        //PlayerModuleSelector.s.ActivateActionDisplayOnTrainBuilding(snappedCart);
                    }
                    snappedMoveTimer = delay;
                }

                if (Mathf.Abs(value.y) > 0.1f) {
                    //var nextBuilding = GetNextBuilding(value.y > 0, snappedTrainBuilding.mySlot, snappedTrainBuilding.mySlot.GetCart().index);
                    var nextBuilding = GetNextBuilding();
                    if (nextBuilding != null) {
                        SnapToTrainModule(nextBuilding);
                        //PlayerModuleSelector.s.ActivateActionDisplayOnTrainBuilding(snappedCart);
                    }
                    
                    snappedMoveTimer = delay;
                }

            }
        } else {
            isSnappedToTransform = false;
        }

        if (value.sqrMagnitude < 0.1f) {
            snappedMoveTimer = 0;
        }

        snappedMoveTimer -= Time.unscaledDeltaTime;
    }

    private Cart GetNextBuilding() {
        throw new NotImplementedException();
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

    public Cart snappedCart;
    public Vector3 snapOffset;
    public void SnapToTrainModule(Cart module) {
        snappedCart = module;
        snapTarget = module.transform;
        snapOffset = Vector3.down * 0.75f;
        isSnappedToTransform = true;
        isSnappedToTrain = true;
    }

    public void SnapToTransform(Transform target) {
        snapTarget = target;
        snapOffset = Vector3.down * 0.5f;
        isSnappedToTransform = true;
        isSnappedToTrain = false;
    }

    public void UnSnap() {
        isSnappedToTransform = false;
        isSnappedToTrain = false;
    }



    public void FlipCamera(InputAction.CallbackContext info) {
        isRight = !isRight;
        rotationAngleTarget = isRight? rotationAngle : -rotationAngle;
    }

    
    public void ToggleCameraEdgeMove() {
        canEdgeMove = !canEdgeMove;
    }

    [Header("Direct Control Settings")] 
    public bool directControlActive = false;
    public Transform directControlTransform;
    private Vector2 rotTarget;
    public float mouseSensitivity = 1f;
    public float gamepadSensitivity = 1f;
    public float overallSensitivity = 1f;
    private bool rotLerping = true;


    public void ProcessDirectControl(Vector2 mouseInput, Vector2 gamepadInput) {
        var processedInput = mouseInput * mouseSensitivity + (gamepadInput * gamepadSensitivity * 35);
        
        processedInput *= /*Time.unscaledTime **/ (overallSensitivity / 2.5f) / 45f;

        ProcessDirectControl(processedInput);
    }
    
    public void ProcessDirectControl(Vector2 processedInput) {
        

        rotTarget += processedInput;

        Quaternion xQuaternion = Quaternion.AngleAxis (rotTarget.x, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis (rotTarget.y, -Vector3.right);
        
        var targetPos = directControlTransform.position;
        var targetRot = directControlTransform.rotation * xQuaternion * yQuaternion;
        cameraLerpDummy.transform.position = Vector3.Lerp(cameraLerpDummy.transform.position, targetPos, posLerpSpeed * Time.unscaledDeltaTime);

        if (rotLerping) {
            cameraLerpDummy.transform.rotation = Quaternion.Lerp(cameraLerpDummy.transform.rotation, targetRot, rotLerpSpeed * Time.unscaledDeltaTime);
            if (Quaternion.Angle(cameraLerpDummy.transform.rotation, targetRot) < 5) {
                rotLerping = false;
            }
        } else {
            cameraLerpDummy.transform.rotation = targetRot;
        }
    }
    
    public void ActivateDirectControl(Transform target) {
        directControlTransform = target;
        directControlActive = true;
        rotTarget = Vector2.zero;
        rotLerping = true;
        Cursor.lockState = CursorLockMode.Locked;
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
