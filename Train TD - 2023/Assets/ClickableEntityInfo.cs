using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableEntityInfo : MonoBehaviour, IClickableInfo {

    public Sprite icon;

    public Tooltip tooltip;
    
    [TextArea]
    public string info;


    public bool ShowInfo() {
        return true;
    }

    public string GetInfo() {
        return info;
    }

    public Tooltip GetTooltip() {
        return tooltip;
    }
}

public interface IClickableInfo {
    public bool ShowInfo();
    public string GetInfo();
    public Tooltip GetTooltip();
}