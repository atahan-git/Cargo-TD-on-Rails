using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class DioramaScript : MonoBehaviour {
    public bool clickable = false;

    public InfiniteMapController.DioramaHolder myDioramaHolder;
    private HighlightEffect _outline;

    public GameObject elite;
    
    void SetClickable(bool _clickable) {
        clickable = _clickable;
        _outline = GetComponent<HighlightEffect>();
        if (clickable) {
            _outline.outlineColor = Color.green;
        } else {
            _outline.outlineColor = Color.white;
        }
    }

    public void Initialize(InfiniteMapController.DioramaHolder holder) {
        myDioramaHolder = holder;
        myDioramaHolder.myScript = this;
        SetClickable(myDioramaHolder.canGoHere);

        var isElite = holder.myPiece.myEnemyType != null && (holder.myPiece.myEnemyType.myType == UpgradesController.PathEnemyType.PathType.boss || 
                                                             holder.myPiece.myEnemyType.myType == UpgradesController.PathEnemyType.PathType.elite);
        elite.SetActive(isElite);
        
        var genericClickable = GetComponent<GenericClickable>();
        if (isElite) {
            genericClickable.myDetails += "Enemies here are especially strong";
        }
    }

    private void Start() {
        var genericClickable = GetComponent<GenericClickable>();
        genericClickable.click += Click;
        genericClickable.select += Select;
        genericClickable.deselect += Deselect;
    }

    public void Click() {
        if (clickable) {
            InfiniteMapController.s.GotoSection(myDioramaHolder);
        }
    }

    private bool mouseOver;
    public void Select() {
        _outline.highlighted = true;
        mouseOver = true;
        if (!clickable) {
            GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.clickGate);
        }

        if (clickable) {
            var enableOutlinesHolder = myDioramaHolder;
            while (enableOutlinesHolder.connectedPieces.Count > 0) {
                enableOutlinesHolder = enableOutlinesHolder.connectedPieces[0];
                enableOutlinesHolder.myScript.GetComponent<HighlightEffect>().highlighted = true;
            }
        }
    }

    public void Deselect() {
        _outline.highlighted = false;
        mouseOver = false;
        
        if (clickable) {
            var enableOutlinesHolder = myDioramaHolder;
            while (enableOutlinesHolder.connectedPieces.Count > 0) {
                enableOutlinesHolder = enableOutlinesHolder.connectedPieces[0];
                if(enableOutlinesHolder.myScript != null)
                    enableOutlinesHolder.myScript.GetComponent<HighlightEffect>().highlighted = false;
            }
        }
    }
}