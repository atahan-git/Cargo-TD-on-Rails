using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericClickable : MonoBehaviour, IPlayerHoldable
{

    public Action select ;
    public void Select() {
        select?.Invoke();
    }

    public Action deselect ;
    public void Deselect() {
        deselect?.Invoke();
    }

    public Action click ;
    public void Click() {
        click?.Invoke();
    }

    public Sprite myIcon;
    public string myDetailsTitle;
    [Multiline]
    public string myDetails;

    public Transform GetUITargetTransform() {
        return transform;
    }

    public void SetHoldingState(bool state) {
        // do nothing. we are not holdable
    }

    public bool CanDrag() {
        return false;
    }

    public DroneRepairController GetHoldingDrone() {
        return null;
    }

    public void SetHoldingDrone(DroneRepairController holder) {
       // cannot be held
    }
}
