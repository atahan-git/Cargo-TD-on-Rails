using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DirectControlMaster : MonoBehaviour {
	public static DirectControlMaster s;

	private void Awake() {
		s = this;
	}

	private void Start() {
		masterDirectControlUI.SetActive(false);
		directControlInProgress = false;
		hitAud = GetComponent<AudioSource>();
	}

	public InputActionReference cancelDirectControlAction;
	public InputActionReference directControlShootAction;

	[Space]
	public IDirectControllable currentDirectControllable;
	public GameObject exitToReload;

	private AudioSource hitAud;
	public Image hitImage;
	public Slider cooldownSlider;
	public Slider ammoSlider;
	public Slider gatlinificationSlider;
	public Image fadeToBlackImage;

	[Space] public GameObject ammo_perfect;
	public GameObject ammo_good;
	public GameObject ammo_full;
	public GameObject ammo_fail;
	
	[Space]
	public GameObject masterDirectControlUI;
	
	public GameObject gunCrosshairsUI;
	public GameObject ammoMinigameUI;
	public GameObject shieldMoveUI;
	public GameObject repairControlUI;
	public GameObject trainEngineControlUI;
	[Space]

	public bool directControlInProgress = false;

	public float directControlFirerateMultiplier = 2f;
	public float directControlDamageMultiplier = 2f;
	public float directControlAmmoUseMultiplier = 0.5f;
	
	private void OnEnable() {
		cancelDirectControlAction.action.Enable();
		directControlShootAction.action.Enable();
		cancelDirectControlAction.action.performed += DisableDirectControl;
	}

	private void OnDisable() {
		cancelDirectControlAction.action.Disable();
		directControlShootAction.action.Disable();
		cancelDirectControlAction.action.performed -= DisableDirectControl;
	}

	public float directControlLock = 0;
	public bool enterDirectControlShootLock = false;

	public GameObject isFire;
	public GameObject isExplosive;
	public GameObject isSticky;
	public void AssumeDirectControl(IDirectControllable source) {
		if (!directControlInProgress && directControlLock <= 0) {
			currentDirectControllable = source;
			PlayerWorldInteractionController.s.canSelect = false;

			enterDirectControlShootLock = true;
			Invoke(nameof(DisableDirectControlEnterShootLock), 0.5f);

			directControlInProgress = true;
			
			
			masterDirectControlUI.SetActive(true);
			
			gunCrosshairsUI.SetActive(false);
			ammoMinigameUI.SetActive(false);
			shieldMoveUI.SetActive(false);
			repairControlUI.SetActive(false);
			trainEngineControlUI.SetActive(false);
			
			currentDirectControllable.ActivateDirectControl();
		}
	}

	void DisableDirectControlEnterShootLock() {
		enterDirectControlShootLock = false;
	}

	private void DisableDirectControl(InputAction.CallbackContext obj) {
		if (directControlInProgress) {
			PlayerWorldInteractionController.s.canSelect = true;
			directControlInProgress = false;
			
			directControlLock = 0.2f;
			CancelInvoke(nameof(DisableDirectControlEnterShootLock));
			
			masterDirectControlUI.SetActive(false);
			
			currentDirectControllable.DisableDirectControl();
		}
	}

	public void DisableDirectControl() {
		DisableDirectControl(new InputAction.CallbackContext());
	}

	public LayerMask lookMask;

	public Image gunCrosshair;
	public GameObject rocketCrosshairEverything;
	public Image rocketCrosshairMain;
	public Image rocketCrosshairLock;
	public TMP_Text rocketLockStatus;
	
	public enum DirectControlMode {
		Gun, LockOn
	}

	public TMP_Text sniperAmount;
	private void Update() {
		if (directControlInProgress && !Pauser.s.isPaused) {
			currentDirectControllable.UpdateDirectControl();

			if (directControlShootAction.action.WasReleasedThisFrame()) {
				enterDirectControlShootLock = false;
			}
		}

		if (directControlLock > 0) {
			directControlLock -= Time.deltaTime;
		}
	}

	public void PlayOnHitSound(float hitPitch) {
		hitAud.pitch = hitPitch;
		hitAud.PlayOneShot(hitAud.clip);
	}
}

public interface IDirectControllable {
	public void ActivateDirectControl();
	public void UpdateDirectControl();
	public void DisableDirectControl();
	public Color GetHighlightColor();

	public GamepadControlsHelper.PossibleActions GetActionKey();
}