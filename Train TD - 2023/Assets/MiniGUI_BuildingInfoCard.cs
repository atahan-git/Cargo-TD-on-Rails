using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MiniGUI_BuildingInfoCard : MonoBehaviour
{
    public Image icon;
    public Image armorPenetrationIcon;
    public TMP_Text moduleName;
    [Space] 
    public TMP_Text moduleDescription;
    
    public Transform infoCardsParent;

    public Transform sourceTransform;

    public bool isSetUp = false;
    public Transform lerpTarget;
    
    public UIElementFollowWorldTarget worldTarget;

    public bool isLerping = false;
    private void Start() {
        if (!isSetUp) {
            worldTarget =  GetComponentInParent<UIElementFollowWorldTarget>(true);
            lerpTarget = new GameObject("lerp target").transform;
            lerpTarget.transform.SetParent(transform.parent);
            lerpTarget.transform.position = transform.position;
            lerpTarget.transform.rotation = transform.rotation;
            isSetUp = true;
        }
    }

    public void SetUp(Cart building) {
        Start();
        Show();

        var gunModule = building.GetComponentInChildren<GunModule>();
        if (gunModule != null) {
            armorPenetrationIcon.gameObject.SetActive(gunModule.canPenetrateArmor);
        } else {
            armorPenetrationIcon.gameObject.SetActive(false);
        }

        icon.sprite = building.Icon;
        moduleName.text = building.displayName;

        moduleDescription.text = building.GetComponentInChildren<ClickableEntityInfo>().GetTooltip().text;

        sourceTransform = building.GetUITargetTransform();

        if (building.uniqueName == "scrapPile" || building.uniqueName == "fuelPile") {
            infoCardsParent.gameObject.SetActive(false);
        } else {
            infoCardsParent.gameObject.SetActive(true);
            var infoCards = infoCardsParent.GetComponentsInChildren<IBuildingInfoCard>(true);
            for (int i = 0; i < infoCards.Length; i++) {
                infoCards[i].SetUp(building);
            }
        }
        worldTarget.SetUp(sourceTransform);
        transform.SetParent(lerpTarget);
        transform.localPosition = Vector3.zero;
        isLerping = false;
    }

    public void SetUp(EnemyHealth enemy) {
        Start();
        Show();
        armorPenetrationIcon.gameObject.SetActive(false);

        icon.sprite = enemy.GetComponentInParent<EnemyWave>().GetIcon();
        moduleName.text = "Error!";
        var myInfo = enemy.GetComponentsInChildren<IClickableInfo>();
        moduleDescription.text =myInfo[0].GetInfo();
        
        infoCardsParent.gameObject.SetActive(true);
        var infoCards = infoCardsParent.GetComponentsInChildren<IBuildingInfoCard>(true);
        for (int i = 0; i < infoCards.Length; i++) {
            infoCards[i].SetUp(enemy);
        }

        sourceTransform = enemy.uiTransform;
        worldTarget.SetUp(sourceTransform);
        transform.SetParent(worldTarget.transform.parent);
        transform.position = lerpTarget.position;
        isLerping = true;
    }
    
    
    public void SetUp(Artifact artifact) {
        Start();
        Show();
        armorPenetrationIcon.gameObject.SetActive(false);

        icon.sprite = artifact.mySprite;
        moduleName.text = artifact.displayName;
        moduleDescription.text = artifact.GetComponent<UITooltipDisplayer>().myTooltip.text;

        infoCardsParent.gameObject.SetActive(true);
        var infoCards = infoCardsParent.GetComponentsInChildren<IBuildingInfoCard>(true);
        for (int i = 0; i < infoCards.Length; i++) {
            infoCards[i].SetUp(artifact);
        }

        sourceTransform = artifact.uiTransform;
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
        Start();
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

public interface IBuildingInfoCard {
    public void SetUp(Cart building);
    public void SetUp(EnemyHealth enemy);
    public void SetUp(Artifact artifact);
}