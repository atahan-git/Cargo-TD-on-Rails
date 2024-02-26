using System;
using System.Collections;
using System.Collections.Generic;
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

    public int range = 0;
    
    public void ResetState() {
        var artifactRangeBooster = GetComponentInParent<Cart>()?.GetComponentInChildren<GemBooster>();
        if (artifactRangeBooster) {
            range = artifactRangeBooster.GetRange();
        }
        
        var modulesWithResetStates = GetComponentsInChildren<IResetStateArtifact>();
        for (int i = 0; i < modulesWithResetStates.Length; i++) {
            modulesWithResetStates[i].ResetState(0); // level goes 0, 1, 2
        }
        
        ApplyToTarget.RemoveAllListeners();
    }

    public void _ApplyToTarget(Cart target) {
        ApplyToTarget?.Invoke(target);
    }

    
    
    [HideInInspector]
    public UnityEvent<Cart> ApplyToTarget = new UnityEvent<Cart>();

    public void AttachToSnapLoc(SnapLocation loc, bool doSave=true, bool doTriggerChange = true) {
        UpgradesController.s.AddArtifactToShop(this,  doSave);
        
        loc.SnapObject(gameObject);
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Rigidbody>().useGravity = false;
        
        attachedToCartPart.SetActive(false);
        worldPart.SetActive(true);
        
        var newCart = GetComponentInParent<Cart>();
        
        if (newCart != null) {
            UpgradesController.s.RemoveArtifactFromShop(this, doSave);
            
            if(doTriggerChange && newCart.IsAttachedToTrain())
                newCart.GetComponentInParent<Train>().TrainChanged();
            
            
            attachedToCartPart.SetActive(true);
            worldPart.SetActive(false);
            
            isAttached = true;
        }
    }

    public void DetachFromCart( bool doSave = true) {
        UpgradesController.s.AddArtifactToShop(this, doSave);
        var oldCart = GetComponentInParent<Cart>();
        
        attachedToCartPart.SetActive(false);
        worldPart.SetActive(true);
        
        transform.SetParent(null);
        
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().useGravity = true;


        if (oldCart != null) {
            Train.s.TrainChanged();
        }

        isAttached = false;
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
}


public interface IResetStateArtifact {
    public void ResetState(int level);
}