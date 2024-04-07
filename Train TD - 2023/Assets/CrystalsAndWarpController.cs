using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrystalsAndWarpController : MonoBehaviour {
    public static CrystalsAndWarpController s;

    public Transform coinGoPosition;
    public TMP_Text coinText;

    public TMP_Text warpNeedsText;
    public TMP_Text warpPrepText;
    public TMP_Text warpingText;
    public Button warpButton;

    public int crystalCount;
    public int maxCrystals;

    public bool warping = false;

    public float warpProgress = 0; // 0-1 pre warp charge, 1-2 linear warp speedup, 2-3 super fast warp

    public int bonusTimeReductionCrystals;

    private void Awake() {
        s = this;
    }

    private void Start() {
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.AddListener(CalculateTotalCrystalStorageAmount);
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.AddListener(UpdateWarpShowStateWhenTrainChanges);
        Train.s.onTrainCartsChanged.AddListener(CrystalCountsUpdated);
        warpPrepText.gameObject.SetActive(false);
        warpingText.gameObject.SetActive(false);
    }

    public void OnCombatStart() {
        crystalCount = 25;
        bonusTimeReductionCrystals = 0;
        CalculateTotalCrystalStorageAmount();
    }


    public void GetCrystal(int count) {
        if (crystalCount < maxCrystals) {
            crystalCount += count;
            crystalCount = Mathf.Clamp(crystalCount, 0, maxCrystals);
        }
        
        CrystalCountsUpdated();
    }

    public int TryUseCrystals(int amount) {
        var usedAmount = Mathf.Min(amount, crystalCount);

        crystalCount -= usedAmount;

        CrystalCountsUpdated();
        return usedAmount;
    }

    private float timeNeeded;
    void CrystalCountsUpdated() {
        coinText.text = $"x{crystalCount}/{maxCrystals}";

        int cartCount = Train.s.carts.Count;
        timeNeeded = cartCount * 15 + 15 - (Mathf.Sqrt(bonusTimeReductionCrystals)*2);
        timeNeeded = Mathf.Clamp(timeNeeded, 60, 600);
        
        warpNeedsText.text = $"Warp time: {ExtensionMethods.FormatTime(timeNeeded)}";
    }


    private bool showingWhatCanWarp = false;
    public void ShowWhatCanWarp() {
        showingWhatCanWarp = true;
        ProcessWhatCanWarp();
        _SetWarpShowState(showingWhatCanWarp);
    }

    public void HideWhatCanWarp() {
        showingWhatCanWarp = false;
        _SetWarpShowState(showingWhatCanWarp);
    }


    void UpdateWarpShowStateWhenTrainChanges() {
        var prevWarpCount = warpableCount;
        ProcessWhatCanWarp();


        if (_actualWarpShowState) {
            _SetWarpShowState(_actualWarpShowState);
        } else {
            if (warping && prevWarpCount != warpableCount) {
                TemporaryShowWhatCanWarp();
            }
        }
    }

    private bool _actualWarpShowState = false;
    
    public Color canWarpColor = Color.cyan;
    public Color cannotWarpColor = Color.black;
    public Color warpStabilizerColor = Color.magenta;
    [Button]
    void _SetWarpShowState(bool isShowing) {
        _actualWarpShowState = isShowing;

        for (int i = 0; i < Train.s.carts.Count; i++) {
            var warpStabilizer = Train.s.carts[i].GetComponentInChildren<WarpStabilizerModule>();
            if (warpStabilizer) {
                Train.s.carts[i].SetHighlightState(_actualWarpShowState, warpStabilizerColor);
            } else {
                Train.s.carts[i].SetHighlightState(_actualWarpShowState, Train.s.carts[i].canWarp ? canWarpColor : cannotWarpColor);
            }
        }
    }

    private IEnumerator _tempShowWarp;
    public void TemporaryShowWhatCanWarp() {
        if(_tempShowWarp != null)
            StopCoroutine(_tempShowWarp);

        ProcessWhatCanWarp();
        _tempShowWarp = _TempShowWarp();
        StartCoroutine(_tempShowWarp);
    }

    IEnumerator _TempShowWarp() {
        _SetWarpShowState(true);
        yield return new WaitForSeconds(3f);
        if(!showingWhatCanWarp)
            _SetWarpShowState(false);
    }

    private int warpableCount;
    void ProcessWhatCanWarp() {
        var carts = Train.s.carts;
        for (int i = 0; i < carts.Count; i++) {
            carts[i].canWarp = false;
        }


        for (int i = 0; i < carts.Count; i++) {
            var warpStabilizer = carts[i].GetComponentInChildren<WarpStabilizerModule>();
            if (warpStabilizer != null && warpStabilizer.enabled) {
                for (int j = 0; j < warpStabilizer.stabilizationRange; j++) {
                    var cartFront = Train.s.GetNextBuilding(j, carts[i]);
                    var cartBack = Train.s.GetNextBuilding(-j, carts[i]);
                    if (cartFront != null) {
                        cartFront.canWarp = true;
                    }

                    if (cartBack != null) {
                        cartBack.canWarp = true;
                    }
                }
            }
        }

        warpableCount = 0;
        for (int i = 0; i < carts.Count; i++) {
            if (carts[i].canWarp) {
                warpableCount += 1;
            }
        }
    }


    public void EngageWarp() {
        warping = !warping;
        bonusTimeReductionCrystals = 0;
        warpPrepText.gameObject.SetActive(false);
        warpingText.gameObject.SetActive(false);
        EnemyWavesController.s.SetWarpingMode(false);

        if (warping) {
            StartCoroutine(DoWarp());
            warpButton.GetComponentInChildren<TMP_Text>().text = "Stop Warp";

        } else {
            StopAllCoroutines();
            SpeedController.s.SetWarpMode(false);
            SpeedController.s.SetBrakingStatus(false);
            
            warpButton.GetComponentInChildren<TMP_Text>().text = "Warp!";
        }
    }
    

    IEnumerator DoWarp() {
        Debug.Log("Doing warp - braking");
        warping = true;
        SpeedController.s.SetWarpMode(true);
        SpeedController.s.SetBrakingStatus(true);

        while (SpeedController.s.IsMoving()) {
            yield return null; // wait
        }
        
        SpeedController.s.SetBrakingStatus(false);

        var stopTime = 0f;

        var crystalUsePerSecond = crystalCount / 5f;
        var curCrystalsUsed = 0f;
        
        warpPrepText.gameObject.SetActive(true);
        
        Debug.Log("Doing warp - prepping");
        bool usedCrystals = false;
        bool usingCrystalsDone = false;
        while (stopTime < 15f) {
            warpProgress = stopTime / 15f;
            warpProgress = Mathf.Clamp01(warpProgress);

            if (stopTime > 5f) {
                curCrystalsUsed += crystalUsePerSecond * Time.deltaTime;
            }

            while (curCrystalsUsed > 0 && crystalCount > 0) {
                bonusTimeReductionCrystals += TryUseCrystals(1);
                curCrystalsUsed -= 1;
                usedCrystals = true;
            }

            if (crystalCount <= 0) {
                usingCrystalsDone = true;
            }

            CrystalCountsUpdated();
            var titleText = "Preparing to Warp!\n";

            if (usedCrystals) {
                titleText = "Using Crystals...\n";
            }

            if (usingCrystalsDone) {
                titleText = "Warp almost ready!\n";
            }
            
            warpPrepText.text = titleText +
                                $"{ExtensionMethods.FormatTime(15f-stopTime)}\n" +
                                $"\n" +
                                $"Crystals: {crystalCount}\n" +
                                $"Total warp time: {ExtensionMethods.FormatTime(timeNeeded)}";
            
            stopTime += Time.deltaTime;
            yield return null;
        }

        TemporaryShowWhatCanWarp();
        EnemyWavesController.s.SetWarpingMode(true);
        
        warpPrepText.gameObject.SetActive(false);
        warpingText.gameObject.SetActive(true);

        var linearIncreaseTime = 0f;
        var endSpeed = 8f;
        var linearTimeNeeded = timeNeeded - 15;
        var acceleration = (endSpeed - SpeedController.s.targetSpeed) / linearTimeNeeded;
        
        Debug.Log("Doing warp - accelerating");
        while (linearIncreaseTime < linearTimeNeeded) {
            warpProgress = 1 + linearIncreaseTime / linearTimeNeeded;

            SpeedController.s.SetWarpTargetSpeed(SpeedController.s.targetSpeed+acceleration*Time.deltaTime);

            warpingText.text = $"Warping: {ExtensionMethods.FormatTime(linearTimeNeeded - linearIncreaseTime + 15)}";

            linearIncreaseTime += Time.deltaTime;
            yield return null;
        }

        var fasterIncreaseTime = 0f;
        endSpeed = 20;
        var fasterIncreaseTimeNeeded = 15f;
        acceleration = (endSpeed - SpeedController.s.targetSpeed) / fasterIncreaseTimeNeeded;

        var startedFading = false;

        Debug.Log("Doing warp - warping");
        TemporaryShowWhatCanWarp();
        while (fasterIncreaseTime < fasterIncreaseTimeNeeded) {
            warpProgress = 2 + fasterIncreaseTime / fasterIncreaseTimeNeeded;
            
            SpeedController.s.SetWarpTargetSpeed(SpeedController.s.targetSpeed+acceleration*Time.deltaTime);

            var fadeToWhite = (fasterIncreaseTime - 10) / 5f;
            fadeToWhite = Mathf.Clamp01(fadeToWhite);
            if (fadeToWhite > 0) {
                ScreenFadeToWhiteController.s.SetFadeToWhite(fadeToWhite);

                if (!startedFading) {
                    startedFading = true;

                    for (int i = 0; i < Train.s.carts.Count; i++) {
                        Train.s.carts[i].GetHealthModule().invincible = true;
                    }
                }
            }
            
            
            warpingText.text = $"Warping: {ExtensionMethods.FormatTime(fasterIncreaseTimeNeeded - fasterIncreaseTime)}";

            fasterIncreaseTime += Time.deltaTime;
            yield return null;
        }

        // leave behind non-warpable carts
        var leaveBehindList = new List<Cart>();
        for (int i = 0; i < Train.s.carts.Count; i++) {
            if (!Train.s.carts[i].canWarp) {
                leaveBehindList.Add(Train.s.carts[i]);
            }
        }

        // ideally we would show a gap here...
        for (int i = 0; i < leaveBehindList.Count; i++) {
            Train.s.RemoveCart(leaveBehindList[i]);
        }
        
        Train.s.SaveTrainState(true);
        
        PlayStateMaster.s.FinishWarpToTeleportBackToShop();
    }

    public void CalculateTotalCrystalStorageAmount() {
        maxCrystals = 50;

        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            if (!cart.isDestroyed) {
                var storageModule = Train.s.carts[i].GetComponentInChildren<CrystalStorageModule>();
                if(storageModule)
                    maxCrystals += storageModule.amount;
            }
        }
        
        CrystalCountsUpdated();
    }

}
