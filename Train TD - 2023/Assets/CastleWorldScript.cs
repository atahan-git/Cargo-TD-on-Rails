using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastleWorldScript : MonoBehaviour {

    public Outline outline;
    
    public Color travelableColor = Color.magenta;
    public Color playerHereColor = Color.cyan;
    public Color defaultColor = Color.cyan;

    public Transform gfxParent;
    
    public StarState myInfo;

    private bool isOutlinePermanentOn = false;
    private float defaultOutlineWidth;
    
    public GameObject playerIndicator;
    public void SetHighlightState(bool isOpen) {
        if (isOutlinePermanentOn) {
            if (isOpen) {
                outline.OutlineWidth = defaultOutlineWidth * 2f;
            } else {
                outline.OutlineWidth = defaultOutlineWidth;
            }
        } else {
            outline.enabled = isOpen;
        }
    }

    public void Refresh() {
        playerIndicator.SetActive(false);
        if(myInfo.isPlayerHere)
            SetPlayerHere();
        
    }
    
    public void Initialize(StarState info) {
        myInfo = info;

        var cityScriptable = DataHolder.s.GetCityScriptable(info.city.uniqueName);
        Instantiate(cityScriptable.worldMapCastle, gfxParent);
        outline = GetComponentInChildren<Outline>();
        
        playerIndicator.SetActive(false);
        
        GetComponentInChildren<MiniGUI_CityIcons>().SetState(cityScriptable.cityData);

        /*if(myInfo.isPlayerHere)
            SetPlayerHere();*/
        
        Refresh();
    }


    public void SetPlayerHere() {
        playerIndicator.SetActive(true);
        outline.OutlineColor = playerHereColor;
        //outline.enabled = true;
        //isOutlinePermanentOn = true;
        defaultOutlineWidth = outline.OutlineWidth;
    }

    public void SetTravelable(bool currentlyTravelable) {
        if (currentlyTravelable) {
            outline.OutlineColor = travelableColor;
            //outline.enabled = true;
            //isOutlinePermanentOn = true;
            defaultOutlineWidth = outline.OutlineWidth;
        }
    }

}
