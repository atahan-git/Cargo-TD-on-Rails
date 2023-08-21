using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MiniScript_StarterTrainSection : MonoBehaviour, IClickableWorldItem
{
    public CharacterData myData;

    public TMP_Text charNameText;
    //public TMP_Text charDescText;

    public GameObject lockedOverlay;

    public Train myTrain;

    public bool isLocked = false;
    public void Setup(CharacterData data, bool _isLocked) {
        isLocked = _isLocked;
        _outline = GetComponent<Outline>();
        myData = data;
        charNameText.text = myData.uniqueName;
        if (isLocked) {
            myTooltip.text = "Character Locked";
            _outline.OutlineColor = Color.grey;
        } else {
            myTooltip.text = myData.description;
        }
        //charDescText.text = myData.description;
		
        lockedOverlay.SetActive(isLocked);

        myTrain.DrawTrain(data.starterTrain);
    }
    
    public void Select() {
        StarterTrainSelector.s.SelectSection(this);
    }
    
    
    private Outline _outline;
    public bool mouseOver;
    public Tooltip myTooltip;
    private void Start() {
        _outline = GetComponent<Outline>();
        _outline.enabled = false;
    }
    
    public void _OnMouseEnter() {
        mouseOver = true;
        _outline.enabled = true;
        Invoke(nameof(ShowTooltip), TooltipsMaster.tooltipShowTime);
    }

    public void _OnMouseExit() {
        mouseOver = false;
        _outline.enabled = false;
        CancelInvoke(nameof(ShowTooltip));
        TooltipsMaster.s.HideTooltip();
    }

    void ShowTooltip() {
        TooltipsMaster.s.ShowTooltip(myTooltip);
    }

    [HideInInspector]
    public UnityEvent OnCanLeaveAndPressLeave = new UnityEvent();

    public void _OnMouseUpAsButton() {
        if (!isLocked) {
            OnCanLeaveAndPressLeave?.Invoke();
            Select();
        }
        _OnMouseExit();
        mouseOver = true;
    }
}
