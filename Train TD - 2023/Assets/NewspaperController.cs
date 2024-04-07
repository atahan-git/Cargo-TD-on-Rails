using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NewspaperController : MonoBehaviour {
    public static NewspaperController s;

    private void Awake() {
        s = this;
    }

    public TMP_Text mainText;

    public CanvasGroup newspaperCanvasGroup;

    public bool newspaperOpenState = false;

    public InputActionReference openNewspaperAction;

    public Transform newspaperTrainFront;
    public Transform newspaperTrainBack;

    public Camera newspaperCamera;

    public Slider inkSlider;
    public TMP_Text inkCountText;

    public float lerpInk = 0;

    public GameObject convertButton;
    public GameObject continueButton;

    public string targetText;

    public Vector3 GetNewspaperTrainPosition(float percent, float distanceAddon) {
        var mainCam = MainCameraReference.s.cam;
        var uiCam = OverlayCamsReference.s.uiCam;

        var backPos = uiCam.WorldToScreenPoint(newspaperTrainBack.transform.position);
        backPos.z = 6f + distanceAddon;
        backPos = mainCam.ScreenToWorldPoint(backPos);

        var frontPos = uiCam.WorldToScreenPoint(newspaperTrainFront.transform.position);
        frontPos.z = 6 + distanceAddon;
        frontPos = mainCam.ScreenToWorldPoint(frontPos);

        var pos = Vector3.Lerp(backPos, frontPos, percent);

        var lookDirection = _GetNewspaperTrainPosition(1, distanceAddon) - _GetNewspaperTrainPosition(0, distanceAddon);
        var perpendicularVector = Quaternion.Euler(0, 90, 0) * lookDirection;
        perpendicularVector = Quaternion.AngleAxis(90, lookDirection)*perpendicularVector;
        
        pos += perpendicularVector * Mathf.Sin(percent * 10 + Time.timeSinceLevelLoad*0.4f) * 0.05f;

        return pos;
    }
    
    public Vector3 _GetNewspaperTrainPosition(float percent, float distanceAddon) {
        var mainCam = MainCameraReference.s.cam;
        var uiCam = OverlayCamsReference.s.uiCam;

        var backPos = uiCam.WorldToScreenPoint(newspaperTrainBack.transform.position);
        backPos.z = 5f + distanceAddon;
        backPos = mainCam.ScreenToWorldPoint(backPos);

        var frontPos = uiCam.WorldToScreenPoint(newspaperTrainFront.transform.position);
        frontPos.z = 6 + distanceAddon;
        frontPos = mainCam.ScreenToWorldPoint(frontPos);

        var pos = Vector3.Lerp(backPos, frontPos, percent);
        return pos;
    }
    
    public Quaternion GetNewspaperTrainRotation(float percent, float distanceAddon) {
        var lookDirection = _GetNewspaperTrainPosition(1, distanceAddon) - _GetNewspaperTrainPosition(0, distanceAddon);
        var rotation = Quaternion.LookRotation(lookDirection);
        
        rotation = Quaternion.AngleAxis(70 + Mathf.Sin(percent*10 + Time.timeSinceLevelLoad)*10, lookDirection)*rotation;
        
        return rotation;
    }
    
    
    protected void OnEnable()
    {
        openNewspaperAction.action.Enable();
        openNewspaperAction.action.performed += ToggleNewspaperScreen;
    }

    

    protected void OnDisable()
    {
        openNewspaperAction.action.Disable();
        openNewspaperAction.action.performed -= ToggleNewspaperScreen;
    }
    

    void Start() {
        newspaperOpenState = false;
        newspaperCanvasGroup.alpha = 0;
        mainText.raycastTarget = true;
    }


    public bool inkLerp = false;
    void Update()
    {
        Train.s.SetNewspaperTrainState(newspaperOpenState);

        if (newspaperOpenState) {
            newspaperCanvasGroup.alpha = Mathf.MoveTowards(newspaperCanvasGroup.alpha, 1, Time.deltaTime);
        } else {
            newspaperCanvasGroup.alpha = Mathf.MoveTowards(newspaperCanvasGroup.alpha, 0, Time.deltaTime);
        }
        
        newspaperCanvasGroup.gameObject.SetActive(newspaperCanvasGroup.alpha > 0);
        newspaperCamera.enabled = newspaperCanvasGroup.alpha > 0;

        if (newspaperCanvasGroup.alpha >= 1) {
            inkLerp = true;
        }

        if (inkLerp) {
            lerpInk = Mathf.MoveTowards(lerpInk, DataSaver.s.GetCurrentSave().newspaperUpgradesProgress.inkCount, 5*Time.deltaTime);
        }

        inkSlider.value = lerpInk;
        inkCountText.text = ((int)lerpInk).ToString();
        
        InterpolateMainText(); 
    }



    public void ConvertCarts() {
        var inkCarts = new List<Cart>();
        
        for (int i = 0; i < Train.s.carts.Count; i++) {
            if (Train.s.carts[i].GetComponentInChildren<WarpStabilizerModule>() is { } warpStabilizerModule && warpStabilizerModule != null) {
                if (warpStabilizerModule.inkCount > 0) {
                    inkCarts.Add(Train.s.carts[i]);
                }
            }
        }

        convertButton.SetActive(false);
        StartCoroutine(_ConvertCarts(inkCarts));
    }

    public GameObject convertEffect;
    IEnumerator _ConvertCarts(List<Cart> toConvert) {

        for (int i = 0; i < toConvert.Count; i++) {
            var curConvert = toConvert[i];
            VisualEffectsController.s.SmartInstantiate(convertEffect, curConvert.transform.position, curConvert.transform.rotation, VisualEffectsController.EffectPriority.Always);
            Train.s.RemoveCart(curConvert);
            Destroy(curConvert.gameObject);

            DataSaver.s.GetCurrentSave().newspaperUpgradesProgress.inkCount += 1;
            DataSaver.s.GetCurrentSave().newspaperUpgradesProgress.totalEarnedInk += 1;

            yield return new WaitForSeconds(0.5f);
        }
        
        Train.s.SaveTrainState();


        var hasCrystal = false;
        continueButton.SetActive(!hasCrystal);
        convertButton.SetActive(hasCrystal);

        yield return null;
    }
    
    public void OpenNewspaperScreen() {
        newspaperOpenState = true;
        PlayerWorldInteractionController.s.canSelect = false;
        newspaperCamera.transform.position = MainCameraReference.s.transform.position;
        newspaperCamera.transform.rotation = MainCameraReference.s.transform.rotation;
        lerpInk = 0;
        inkLerp = false;

        var hasCrystal = TrainHasCrystalCarts();
        continueButton.SetActive(!hasCrystal);
        convertButton.SetActive(hasCrystal);
        
        SetMainText(true);
    }


    public void CloseNewspaperScreen() {
        newspaperOpenState = false;
        PlayerWorldInteractionController.s.canSelect = true;
    }

    void ToggleNewspaperScreen(InputAction.CallbackContext context) {
        if (newspaperOpenState) {
            CloseNewspaperScreen();
        }else
        {
            OpenNewspaperScreen();
        }
    }


    public static bool TrainHasCrystalCarts() {
        for (int i = 0; i < Train.s.carts.Count; i++) {
            if (Train.s.carts[i].GetComponentInChildren<WarpStabilizerModule>() is { } warpStabilizerModule && warpStabilizerModule != null) {
                if (warpStabilizerModule.inkCount > 0) {
                    return true;
                }
            }
        }

        return false;
    }


    public void ResetInkDistribution() {
        var unlockedThings = DataSaver.s.GetCurrentSave().newspaperUpgradesProgress;
        unlockedThings.inkCount = unlockedThings.totalEarnedInk;

        unlockedThings.bossUnlocked = false;
        unlockedThings.gatlingUnlock = false;
        unlockedThings.growthUnlock = false;
        unlockedThings.shieldUnlock = false;
        unlockedThings.uraniumUnlock = false;
        unlockedThings.ammoCartLimit = true;
        unlockedThings.gunCartLimit = true;
        unlockedThings.repairCartLimit = true;
        unlockedThings.overallCartLimitIncrease = 0;
        
        SetMainText(false);
    }
    
    public void OnPointerClick(BaseEventData eventData) {
        var pointerEventData = (PointerEventData)eventData;
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(mainText, pointerEventData.position, OverlayCamsReference.s.uiCam);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = mainText.textInfo.linkInfo[linkIndex];
            string linkID = linkInfo.GetLinkID();
            Debug.Log("Clicked on word: " + linkID);
            ProcessWordClick(linkID);
            // Handle click on the specific word here
        } else {
            Debug.Log("Clicked on somewhere without link");
        }
    }

    void ProcessWordClick(string linkID) {
        if(!interpolationComplete)
            return;

        var unlockedThings = DataSaver.s.GetCurrentSave().newspaperUpgradesProgress;
        if (unlockedThings.inkCount >= gunCartLimit.cost && linkID == gunCartLimit.uniqueName) {
            unlockedThings.gunCartLimit = false;
            unlockedThings.inkCount -= gunCartLimit.cost;
        } else if (unlockedThings.inkCount >= ammoCartLimit.cost && linkID == ammoCartLimit.uniqueName) {
            unlockedThings.ammoCartLimit = false;
            unlockedThings.inkCount -= ammoCartLimit.cost;
        } else if (unlockedThings.inkCount >= repairCartLimit.cost && linkID == repairCartLimit.uniqueName) {
            unlockedThings.repairCartLimit = false;
            unlockedThings.inkCount -= repairCartLimit.cost;
        } else if (unlockedThings.inkCount >= totalCartLimit.cost && linkID == totalCartLimit.uniqueName) {
            unlockedThings.overallCartLimitIncrease += 1;
            unlockedThings.inkCount -= totalCartLimit.cost;
        } else if (unlockedThings.inkCount >= gatlingUnlock.cost && linkID == gatlingUnlock.uniqueName) {
            unlockedThings.gatlingUnlock = true;
            unlockedThings.inkCount -= gatlingUnlock.cost;
        } else if (unlockedThings.inkCount >= shieldUnlock.cost && linkID == shieldUnlock.uniqueName) {
            unlockedThings.shieldUnlock = true;
            unlockedThings.inkCount -= shieldUnlock.cost;
        } else if (unlockedThings.inkCount >= growthUnlock.cost && linkID == growthUnlock.uniqueName) {
            unlockedThings.growthUnlock = true;
            unlockedThings.inkCount -= growthUnlock.cost;
        } else if (unlockedThings.inkCount >= uraniumUnlock.cost && linkID == uraniumUnlock.uniqueName) {
            unlockedThings.uraniumUnlock = true;
            unlockedThings.inkCount -= uraniumUnlock.cost;
        } else if (unlockedThings.inkCount >= bossUnlock.cost && linkID == bossUnlock.uniqueName) {
            unlockedThings.bossUnlocked = true;
            unlockedThings.inkCount -= bossUnlock.cost;
        }
        
        SetMainText(false);
    }

    public NewspaperTextHolderScriptable textHolderScriptable;

    private NewspaperUnlocks gunCartLimit = new NewspaperUnlocks() { uniqueName = "gunCart", cost = 1, text = "a gun cart" };
    private NewspaperUnlocks ammoCartLimit = new NewspaperUnlocks() { uniqueName = "ammoCart", cost = 2, text = "an ammo cart" };
    private NewspaperUnlocks repairCartLimit = new NewspaperUnlocks() { uniqueName = "repairCart", cost = 10, text = "a repair cart" };
    private NewspaperUnlocks totalCartLimit = new NewspaperUnlocks() { uniqueName = "cartLimit", cost = 10, text = "a total of up to {0} carts" };
    
    
    private NewspaperUnlocks gatlingUnlock = new NewspaperUnlocks() { uniqueName = "gatlingTech", cost = 1, text = "gatling" };
    private NewspaperUnlocks shieldUnlock = new NewspaperUnlocks() { uniqueName = "shieldTech", cost = 2, text = "shield" };
    
    private NewspaperUnlocks growthUnlock = new NewspaperUnlocks() { uniqueName = "growthTech", cost = 3, text = "growth" };
    private NewspaperUnlocks uraniumUnlock = new NewspaperUnlocks() { uniqueName = "uraniumTech", cost = 5, text = "uranium" };

    private NewspaperUnlocks bossUnlock = new NewspaperUnlocks() { uniqueName = "bossUnlock", cost = 20, text = "failed to return back to our city" };

    public bool interpolationComplete = true;
    void SetMainText(bool isInstant) {
        isInstant = true;
        var unlockedThings = DataSaver.s.GetCurrentSave().newspaperUpgradesProgress;

        var fullText = textHolderScriptable.introText;

        var cartLimitThings = new List<string>();

        if (unlockedThings.gunCartLimit) {
            cartLimitThings.Add(gunCartLimit.MakeText());
        }
        if (unlockedThings.ammoCartLimit) {
            cartLimitThings.Add(ammoCartLimit.MakeText());
        }
        if (unlockedThings.repairCartLimit) {
            cartLimitThings.Add(repairCartLimit.MakeText());
        }

        if (cartLimitThings.Count > 0) {
            fullText += FormatStringWithList(textHolderScriptable.cartTypesTexts[cartLimitThings.Count], cartLimitThings);
        } else {
            fullText += textHolderScriptable.cartTypesTexts[0];
        }

        var curLimitIncrease = unlockedThings.overallCartLimitIncrease;
        if (curLimitIncrease < 4) {
            totalCartLimit.cost = (10 + curLimitIncrease * curLimitIncrease);
            var actualCartLimit = new NewspaperUnlocks() { uniqueName = totalCartLimit.uniqueName, cost = totalCartLimit.cost, text = totalCartLimit.text };
            actualCartLimit.text = string.Format(actualCartLimit.text, 4 + curLimitIncrease);
            fullText += string.Format(textHolderScriptable.totalCartCountText[1], actualCartLimit.MakeText());
        } else {
            fullText += textHolderScriptable.totalCartCountText[0];
        }

        fullText += "\n\n";
        
        
        var cartTechs = new List<string>();
        if (!unlockedThings.gatlingUnlock) {
            cartTechs.Add(gatlingUnlock.MakeText());
        }

        if (!unlockedThings.shieldUnlock) {
            cartTechs.Add(shieldUnlock.MakeText());
        }
        
        if (cartTechs.Count > 0) {
            fullText += FormatStringWithList(textHolderScriptable.cartTechTexts[cartTechs.Count], cartTechs);
        } else {
            fullText += textHolderScriptable.cartTechTexts[0];
        }
        
        var gemTechs = new List<string>();
        if (!unlockedThings.growthUnlock) {
            gemTechs.Add(growthUnlock.MakeText());
        }

        if (!unlockedThings.uraniumUnlock) {
            gemTechs.Add(uraniumUnlock.MakeText());
        }
        
        if (gemTechs.Count > 0) {
            fullText +=  FormatStringWithList(textHolderScriptable.gemTechTexts[gemTechs.Count], gemTechs);
        } else {
            fullText += textHolderScriptable.gemTechTexts[0];
        }
        
        fullText += "\n\n";


        if (!unlockedThings.bossUnlocked) {
            fullText += string.Format(textHolderScriptable.didNotMakeItText, bossUnlock.MakeText());
        } else {
            fullText += textHolderScriptable.didMakeItText;
        }

        interpolationComplete = false;
        charToReplace = 0;
        targetText = fullText;
        
        if (isInstant) {
            mainText.text = targetText;
            interpolationComplete = true;
        }
    }

    private float charToReplace = 0;
    void InterpolateMainText() {
        charToReplace += 10*Time.deltaTime;


        bool madeChange = false;
        bool foundCommand = false;
        var newText = "";
        
        var sourceText = mainText.text;

        if (sourceText.Length > targetText.Length) {
            for (int i = 0; i < sourceText.Length; i++) {
                if (sourceText[i] == '<') {
                    foundCommand = true;
                }

                if (foundCommand) {
                    newText += sourceText[i];
                }else if (i < targetText.Length && sourceText[i] == targetText[i]) {
                    newText += sourceText[i];
                }
                
                
                if (foundCommand && sourceText[i] == '>') {
                    foundCommand = false;
                }

                
            }
        }else if (sourceText.Length < targetText.Length) {
            
        }
        
        if (!madeChange) {
            mainText.text = targetText;
            interpolationComplete = true;
        }
    }
    
    static string FormatStringWithList(string format, List<string> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            format = format.Replace("{" + i + "}", values[i]);
        }

        return format;
    }

    class NewspaperUnlocks {
        public string uniqueName;
        public int cost;
        public string text;

        public string MakeText() {
            return $"<color=purple><link={uniqueName}>{text} ({cost})</link></color>";
        }
    }
}
