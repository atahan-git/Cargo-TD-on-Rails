using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorAnimator : MonoBehaviour {

	public Vector2 cursorHotspot = new Vector2(1, 1);
    public Texture2D cursorIdle;
    public Texture2D cursorClick;
    void Start()
    {
		Cursor.SetCursor(cursorIdle, cursorHotspot, CursorMode.Auto);   
    }

    private bool lastMouseState;
    void Update() {
	    if(Cursor.lockState == CursorLockMode.Locked)
		    return;
	    
	    
	    var currentMouseState = Mouse.current.leftButton.isPressed;
	    if (currentMouseState != lastMouseState) {
		    if (currentMouseState) {
			    Cursor.SetCursor(cursorClick, cursorHotspot, CursorMode.Auto);
			    SpawnParticles();
		    } else {
			    Cursor.SetCursor(cursorIdle, cursorHotspot, CursorMode.Auto);
		    }
	    }

	    lastMouseState = currentMouseState;
    }


    void SpawnParticles() {
	    RaycastHit hit;
	    Ray ray = GetRay();
	    if (Physics.Raycast(ray, out hit, 100f)) {
		    var pos = hit.point;
		    var rot = Quaternion.LookRotation(hit.normal);
		    var type = CommonEffectsProvider.CommonEffectType.dirtHit;
		    
		    var cart = hit.collider.gameObject.GetComponentInParent<ModuleHealth>();
		    if (cart != null) {
			    type = CommonEffectsProvider.CommonEffectType.trainHit;
		    }
		    var enemy = hit.collider.gameObject.GetComponentInParent<EnemyHealth>();
		    if (enemy != null) {
			    type = CommonEffectsProvider.CommonEffectType.enemyHit;
		    }
		    CommonEffectsProvider.s.SpawnEffect(type, pos,rot, VisualEffectsController.EffectPriority.Always);
	    }
    }
    
    public Ray GetRay() {
	    if (SettingsController.GamepadMode()) {
		    return GamepadControlsHelper.s.GetRay();
	    } else {
		    return LevelReferences.s.mainCam.ScreenPointToRay(GetMousePos());
	    }
    }
    
    Vector2 GetMousePos() {
	    return Mouse.current.position.ReadValue();
    }
}
