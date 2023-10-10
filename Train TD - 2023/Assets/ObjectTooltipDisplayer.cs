using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTooltipDisplayer : MonoBehaviour, IClickableWorldItem
{
    private Outline _outline;
	
    public Tooltip myTooltip;

    public bool mouseOver;

    private void Start() {
        _outline = GetComponent<Outline>();
        _outline.enabled = false;
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
