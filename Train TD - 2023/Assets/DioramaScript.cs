using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class DioramaScript : MonoBehaviour, IClickableWorldItem {
    public bool selectable = false;

    public InfiniteMapController.DioramaHolder myDioramaHolder;
    private HighlightEffect _outline;
    void SetSelectable(bool _selectable) {
        selectable = _selectable;
        _outline = GetComponent<HighlightEffect>();
        if (selectable) {
            _outline.outlineColor = Color.green;
        } else {
            _outline.outlineColor = Color.white;
        }
    }
    
    public bool CanClick() {
        return true;
    }

    public void Initialize(InfiniteMapController.DioramaHolder holder) {
        myDioramaHolder = holder;
        myDioramaHolder.myScript = this;
        SetSelectable(myDioramaHolder.canGoHere);
    }

    public Transform GetUITargetTransform() {
        return transform;
    }

    public void SetHoldingState(bool state) {
        // do nothing
    }
    
    public void Click() {
        if (selectable) {
            InfiniteMapController.s.GotoSection(myDioramaHolder);
        }
    }

    private bool mouseOver;
    public void _OnMouseEnter() {
        _outline.highlighted = true;
        Invoke(nameof(ShowTooltip), TooltipsMaster.tooltipShowTime);
        mouseOver = true;
        if (!selectable) {
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.clickGate);
        }

        if (selectable) {
            var enableOutlinesHolder = myDioramaHolder;
            while (enableOutlinesHolder.connectedPieces.Count > 0) {
                enableOutlinesHolder = enableOutlinesHolder.connectedPieces[0];
                enableOutlinesHolder.myScript.GetComponent<HighlightEffect>().highlighted = true;
            }
        }
    }

    public void _OnMouseExit() {
        _outline.highlighted = false;
        CancelInvoke(nameof(ShowTooltip));
        TooltipsMaster.s.HideTooltip();
        mouseOver = false;
        
        if (selectable) {
            var enableOutlinesHolder = myDioramaHolder;
            while (enableOutlinesHolder.connectedPieces.Count > 0) {
                enableOutlinesHolder = enableOutlinesHolder.connectedPieces[0];
                if(enableOutlinesHolder.myScript != null)
                    enableOutlinesHolder.myScript.GetComponent<HighlightEffect>().highlighted = false;
            }
        }
    }

    void ShowTooltip() {
        TooltipsMaster.s.ShowTooltip(new Tooltip(){text="Different biomes will eventually have different effects on gameplay"});
    }
    public void _OnMouseUpAsButton() {
        Click();
        _OnMouseExit();
    }

    private void Update() {
        if (mouseOver) {
            if (PlayerWorldInteractionController.s.showDetailClick.action.WasPerformedThisFrame()) {
                CancelInvoke(nameof(ShowTooltip));
                ShowTooltip();
            }
        }
    }
}