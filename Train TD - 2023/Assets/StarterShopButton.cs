using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StarterShopButton : MonoBehaviour, IClickableWorldItem
{
	private Outline _outline;
	
	public Tooltip myTooltip;
	public Color canBuyColor= Color.green;
	public Color cannotBuyColor = Color.red;

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

    [HideInInspector]
    public UnityEvent OnPress = new UnityEvent();

    public void _OnMouseUpAsButton() {
        OnPress?.Invoke();
        _OnMouseExit();
    }

    public void SetStatus(bool canBuy) {
        if (canBuy) {
            _outline.OutlineColor = canBuyColor;
        } else {
            _outline.OutlineColor = cannotBuyColor;
        }
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
