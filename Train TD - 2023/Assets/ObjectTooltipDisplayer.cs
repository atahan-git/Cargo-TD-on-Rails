using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class ObjectTooltipDisplayer : MonoBehaviour, IClickableWorldItem
{
    private HighlightEffect _outline;
	
    public Tooltip myTooltip;

    public bool mouseOver;

    private void Start() {
        _outline = GetComponent<HighlightEffect>();
        _outline.enabled = false;
    }

    public bool CanClick() {
        return !PlayStateMaster.s.isCombatInProgress();
    }

    public void _OnMouseEnter() {
        _outline.enabled = true;
        Invoke(nameof(ShowTooltip), TooltipsMaster.tooltipShowTime);
        mouseOver = true;
    }

    public void _OnMouseExit() {
        _outline.enabled = false;
        CancelInvoke(nameof(ShowTooltip));
        TooltipsMaster.s.HideTooltip();
        mouseOver = false;
    }

    void ShowTooltip() {
        TooltipsMaster.s.ShowTooltip(myTooltip);
    }

    public void _OnMouseUpAsButton() {
        _OnMouseExit();
    }

    private void Update() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            TooltipsMaster.s.HideTooltip();
        } else {
            if (mouseOver) {
                if (PlayerWorldInteractionController.s.showDetailClick.action.WasPerformedThisFrame()) {
                    CancelInvoke(nameof(ShowTooltip));
                    ShowTooltip();
                }
            }
        }
    }
}
