using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MiniGUI_GenericInfoSlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text description;

    public void SetUp(IGenericInfoProvider myGenericInfo) {
        if (myGenericInfo != null) {
            icon.sprite = myGenericInfo.GetSprite();
            description.text = myGenericInfo.GetDescription();
        } 
    }
}
