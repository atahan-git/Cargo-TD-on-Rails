using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiniGUI_InfoCardFollowPositionLogic : MonoBehaviour
{
   

    public Transform sourceTransform;

    public bool isSetUp = false;
    public Transform lerpTarget;
    
    public UIElementFollowWorldTarget worldTarget;

    public bool isLerping = false;

    public void Initialize() {
        if (!isSetUp) {
            worldTarget =  GetComponentInParent<UIElementFollowWorldTarget>(true);
            lerpTarget = new GameObject("lerp target").transform;
            lerpTarget.transform.SetParent(transform.parent);
            lerpTarget.transform.position = transform.position;
            lerpTarget.transform.rotation = transform.rotation;
            isSetUp = true;
        }
    }
    public void SetUp(Transform _sourceTransform) {
        Initialize();

        sourceTransform = _sourceTransform;
        worldTarget.SetUp(sourceTransform);
        transform.SetParent(lerpTarget);
        transform.localPosition = Vector3.zero;
        isLerping = false;
    }

    public void Show() {
        gameObject.SetActive(true);
        worldTarget.gameObject.SetActive(true);
    }

    public void Hide() {
        if (!isSetUp) {
            Initialize();
        }
        gameObject.SetActive(false);
        worldTarget.gameObject.SetActive(false);
    }
    
    public RectTransform reticle;
    public List<RectTransform> extraRects = new List<RectTransform>();

    public bool IsMouseOverMenu() {
        if (!gameObject.activeSelf)
            return false;
	    
        Vector2 mousePos = Mouse.current.position.ReadValue();
        var rect = reticle;
        var isOverRect = RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, OverlayCamsReference.s.uiCam);

        for (int i = 0; i < extraRects.Count; i++) {
            if (extraRects[i] != null) {
                var isOverButton = RectTransformUtility.RectangleContainsScreenPoint(extraRects[i], mousePos, OverlayCamsReference.s.uiCam);
                isOverRect = isOverRect || isOverButton;
            }
        }

        return isOverRect;
    }

    private void Update() {
        if (isLerping) {
            transform.position = Vector3.Lerp(transform.position, lerpTarget.position, 2 * Time.deltaTime);
        }
    }
}
