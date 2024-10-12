using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Artifact : MonoBehaviour, IPlayerHoldable
{
    public string displayName = "Unnamed But Nice in game name";
    public string uniqueName = "unnamed";

    public bool isComponent;

    public Transform uiTransform;

    public GameObject worldPart;
    public GameObject attachedToCartPart;

    public Sprite mySprite;

    public bool isAttached = false;

    public GameObject cantAffectOverlay;

    public bool canDrag = true;

    public void AttachToSnapLoc(SnapLocation loc, bool doSave=true, bool doTriggerChange = true) {
        ShopStateController.s.AddArtifactToShop(this,  doSave);
        
        loc.SnapObject(gameObject);
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Rigidbody>().useGravity = false;
        
        attachedToCartPart.SetActive(false);
        worldPart.SetActive(true);
        
        var newCart = GetComponentInParent<Cart>();
        
        if (newCart != null) {
            isAttached = true;
            
            ShopStateController.s.RemoveArtifactFromShop(this, doSave);
            
            if(doTriggerChange && newCart.IsAttachedToTrain())
                newCart.GetComponentInParent<Train>().TrainChanged();
            
            attachedToCartPart.SetActive(true);
            worldPart.SetActive(false);
            
            newCart.GetComponent<HighlightEffect>().Refresh();
        }
    }

    public void DetachFromCart( bool doSave = true) {
        ShopStateController.s.AddArtifactToShop(this, doSave);
        var oldCart = GetComponentInParent<Cart>();
        
        attachedToCartPart.SetActive(false);
        worldPart.SetActive(true);
        
        transform.SetParent(null);
        
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().useGravity = true;
        
        if(cantAffectOverlay != null)
            cantAffectOverlay.SetActive(false);

        isAttached = false;

        if (oldCart != null) {
            Train.s.TrainChanged();
            
            oldCart.GetComponent<HighlightEffect>().Refresh();
        }
    }
    
    public Transform GetUITargetTransform() {
        return uiTransform;
    }

    public void SetHoldingState(bool state) {
        if (state) {
            DetachFromCart();
            
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
        } else {
            GetComponent<Rigidbody>().isKinematic = false;
            GetComponent<Rigidbody>().useGravity = true;
        }
    }

    public bool CanDrag() {
        return canDrag && !PlayStateMaster.s.isEndGame();
    }

    private DroneRepairController _holder;
    public DroneRepairController GetHoldingDrone() {
        return _holder;
    }

    public void SetHoldingDrone(DroneRepairController holder) {
        _holder = holder;
    }

    public string GetDescription() {
        if (isComponent) {
            return notAttachedDescription;
        }

        if (!isAttached) {
            return notAttachedDescription;
        }

        var descriptionProvider = GetComponent<IArtifactDescription>();
        if (descriptionProvider != null) {
            return descriptionProvider.GetDescription();
        }
        
        return "not implemented description";
    }

    [Multiline(2)]
    public string notAttachedDescription;
}

public interface IArtifactDescription {
    public string GetDescription();
}