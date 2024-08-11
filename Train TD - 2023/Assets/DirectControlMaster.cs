using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

	[Header("Input")]
	public InputActionReference cancelDirectControlAction;
	public InputActionReference shootAction;
	
	public InputActionReference moveAction;
	public InputActionReference flyUpAction;
	public InputActionReference flyDownAction;
	
	public InputActionReference alternativeActiveAction;
	
	public IDirectControllable currentDirectControllable;

	[Header("Shooting")]
	public GameObject exitToReload;

	private AudioSource hitAud;
	public Image hitImage;
	public Slider cooldownSlider;
	public Slider ammoSlider;
	public Slider gatlinificationSlider;
	public Image fadeToBlackImage;
	
	public float directControlFirerateMultiplier = 2f;
	public float directControlDamageMultiplier = 2f;
	public float directControlAmmoUseMultiplier = 0.5f;

	public GameObject isFire;
	public GameObject isExplosive;
	public GameObject isSticky;
	
	public LayerMask gunLookMask;

	public Image gunCrosshair;
	public GameObject rocketCrosshairEverything;
	public Image rocketCrosshairMain;
	public Image rocketCrosshairLock;
	public TMP_Text rocketLockStatus;
	
	
	public enum GunMode {
		Gun, LockOn
	}

	public TMP_Text sniperAmount;
	
	[Header("Ammo")]
	public GameObject ammo_perfect;
	public GameObject ammo_good;
	public GameObject ammo_full;
	public GameObject ammo_fail;

	[Header("Repair")]
	public Image validRepairImage;
	public Image arrowRepairImage;
	public Slider repairingSlider;
	public LayerMask repairLookMask;
	
	public GameObject exitToRecharge;
	public MiniGUI_ShowRepairDroneChargePercent chargePercentUI;

	[Header("Engine Control")] 
	public SpeedometerScript pressureGauge;
	public TMP_Text pressureInfo;
	public TMP_Text engineOverdrive;
	public GameObject brakingIndicators;
	
	[Header("UI")]
	public GameObject masterDirectControlUI;
	
	public GameObject gunCrosshairsUI;
	public GameObject ammoMinigameUI;
	public GameObject shieldMoveUI;
	public GameObject repairControlUI;
	public GameObject trainEngineControlUI;
	public GameObject notImplementedUI;
	
	[Header("Shared")]
	public float directControlLock = 0;
	public bool enterDirectControlShootLock = false;

	public bool directControlInProgress = false;

	[Button]
	private void OnEnable() {
		cancelDirectControlAction.action.Enable();
		shootAction.action.Enable();
		cancelDirectControlAction.action.performed += DisableDirectControl;
		
		moveAction.action.Enable();
		flyUpAction.action.Enable();
		flyDownAction.action.Enable();
		
		alternativeActiveAction.action.Enable();
	}

	private void OnDisable() {
		cancelDirectControlAction.action.Disable();
		shootAction.action.Disable();
		cancelDirectControlAction.action.performed -= DisableDirectControl;
		
		
		moveAction.action.Disable();
		flyUpAction.action.Disable();
		flyDownAction.action.Disable();
		
		alternativeActiveAction.action.Disable();
	}
	
	[HideInInspector]
	public UnityEvent<bool> OnDirectControlStateChange = new UnityEvent<bool>();

	public void AssumeDirectControl(IDirectControllable source) {
		if (!directControlInProgress && directControlLock <= 0) {
			if (((MonoBehaviour)source).GetComponentInParent<Cart>().isDestroyed) {
				return;
			}
			
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
			notImplementedUI.SetActive(false);
			
			currentDirectControllable.ActivateDirectControl();
			
			OnDirectControlStateChange?.Invoke(true);
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
			OnDirectControlStateChange?.Invoke(false);
		}
	}

	public void DisableDirectControl() {
		DisableDirectControl(new InputAction.CallbackContext());
	}

	
	private void Update() {
		if (directControlInProgress && !Pauser.s.isPaused) {
			currentDirectControllable.UpdateDirectControl();

			if (shootAction.action.WasReleasedThisFrame()) {
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