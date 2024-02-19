 using System;
using System.Collections;
using System.Collections.Generic;
 using HighlightPlus;
 using UnityEngine;
using UnityEngine.Events;

public class GateScript : MonoBehaviour, IClickableWorldItem {

    public GameObject readyToGoEffects;
    //public GameObject readyToGoEffectsUI;

    public Transform gate;

    public Transform gateFullOpenPos;
    public Transform gateHalfOpenPos;
    public Transform gateClosePos;

    public float upMoveSpeed = 1f;
    public float downMoveGravity = 10f;
    public float downCurrentSpeed = 0;

    public bool mouseOverAble = true;
    public bool mouseOver;
    public bool canGo;
    public bool stuckInOpenPos = false;

    public Tooltip myTooltip;
    public Color canGoColor= Color.green;
    public Color cannotGoColor = Color.red;

    private HighlightEffect _outline;

    private void Start() {
        _outline = GetComponent<HighlightEffect>();
        _outline.highlighted = false;
        if (stuckInOpenPos) {
            gate.transform.position = gateFullOpenPos.position;
        }
        SetCanGoStatus(canGo, new Tooltip(){text = "Click the gate to start your run."});
    }

    public void _OnMouseEnter() {
        if(!mouseOverAble)
            return;
        mouseOver = true;
        downCurrentSpeed = 0;
        _outline.highlighted = true;
        Invoke(nameof(ShowTooltip), TooltipsMaster.tooltipShowTime);
    }

    public void _OnMouseExit() {
        if(!mouseOverAble)
            return;
        mouseOver = false;
        _outline.highlighted = false;
        CancelInvoke(nameof(ShowTooltip));
        TooltipsMaster.s.HideTooltip();
    }

    void ShowTooltip() {
        TooltipsMaster.s.ShowTooltip(myTooltip);
    }

    [HideInInspector]
    public UnityEvent OnCanLeaveAndPressLeave = new UnityEvent();

    public void _OnMouseUpAsButton() {
        if(!mouseOverAble)
            return;
        if (canGo) {
            OnCanLeaveAndPressLeave?.Invoke();
            _OnMouseExit();
            mouseOver = true;
        }
    }


    public void SetCanGoStatus(bool status, Tooltip tooltip) {
        canGo = status;
        readyToGoEffects.SetActive(canGo);
        downCurrentSpeed = 0;
        _outline.outlineColor = canGo ? canGoColor : cannotGoColor;
        myTooltip = tooltip;
        enabled = true;
    }

    
    private void Update() {
        if(!mouseOverAble)
            return;
        
        if (PlayStateMaster.s.isCombatInProgress()) {
            TooltipsMaster.s.HideTooltip();
            gate.transform.position = Vector3.MoveTowards(gate.transform.position, gateFullOpenPos.position, upMoveSpeed * Time.deltaTime * 3f);
            
        } else {
            if (canGo) {
                if (mouseOver) {
                    gate.transform.position = Vector3.MoveTowards(gate.transform.position, gateFullOpenPos.position, upMoveSpeed * Time.deltaTime);
                } else {
                    gate.transform.position = Vector3.MoveTowards(gate.transform.position, gateHalfOpenPos.position, upMoveSpeed * Time.deltaTime);
                }
            } else {
                gate.transform.position = Vector3.MoveTowards(gate.transform.position, gateClosePos.position, downCurrentSpeed * Time.deltaTime);
                downCurrentSpeed += downMoveGravity * Time.deltaTime;
                downCurrentSpeed = Mathf.Clamp(downCurrentSpeed, 0, 10f);
            }


            if (mouseOver) {
                if (PlayerWorldInteractionController.s.showDetailClick.action.WasPerformedThisFrame()) {
                    CancelInvoke(nameof(ShowTooltip));
                    ShowTooltip();
                }
            }
        }
    }
}

public interface IClickableWorldItem {

    public void _OnMouseEnter();

    public void _OnMouseExit();

    public void _OnMouseUpAsButton();
}