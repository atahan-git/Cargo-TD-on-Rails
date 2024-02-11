using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockThingsSlot : MonoBehaviour {

    public Transform artifactParent;
    public Transform cartParent;

    public GameObject noMoreUnlock;
    public GameObject sold;
    public GameObject canNoLongerBuy;
    public GameObject moneyPot;

    private bool isSold = false;
    
    private StarterShopButton _starterShopButton;
    private int curCost = 0;
    private string curUniqueName;
    private bool isCart = true;
    private void Start() {
        _starterShopButton = GetComponent<StarterShopButton>();
        _starterShopButton.OnPress.AddListener(TryPurchase);
    }

    public void SetUp(Artifact artifact) {
        Instantiate(artifact.gameObject, artifactParent);
        //curCost = artifact.buyCost;
        GetComponentInChildren<MoneyUIDisplay>().SetAmount(curCost);
        curUniqueName = artifact.uniqueName;
        _starterShopButton.myTooltip = new Tooltip() { text = $"Buy: {artifact.displayName}" };
        isEmpty = false;
        isCart = false;
    }

    public void SetUp(Cart cart) {
        _starterShopButton = GetComponent<StarterShopButton>();
        Instantiate(cart.gameObject, cartParent);
        //curCost = cart.buyCost;
        GetComponentInChildren<MoneyUIDisplay>().SetAmount(curCost);
        curUniqueName = cart.uniqueName;
        _starterShopButton.myTooltip = new Tooltip() { text = $"Buy: {cart.displayName}" };
        isEmpty = false;
        isCart = true;
    }


    public void TellNoMoreToUnlock() {
        noMoreUnlock.SetActive(true);
        moneyPot.SetActive(false);
    }

    public void Sold() {
        isSold = true;
        sold.SetActive(true);
        GetComponentInParent<UnlockThingShop>().Sold();
    }

    public void CanNoLongerBuy() {
        if (!isSold) {
            canNoLongerBuy.SetActive(true);
            isSold = true;
        }
    }


    void Update()
    {
        _starterShopButton.SetStatus(!isEmpty && !isSold && DataSaver.s.GetCurrentSave().money >= curCost);
    }

    public bool isEmpty = true;
    public void Clear() {
        if (artifactParent.childCount > 0) {
            Destroy(artifactParent.GetChild(0).gameObject);
        }

        if (cartParent.childCount > 0) {
            Destroy(cartParent.GetChild(0).gameObject);
        }

        isEmpty = true;
    }
    
    public void TryPurchase() {
        if (!isSold && !isEmpty) {
            if (curCost <= DataSaver.s.GetCurrentSave().money) {
                DataSaver.s.GetCurrentSave().money -= curCost;
                //DataSaver.s.GetCurrentSave().metaProgress.unlockedThings.Add(curUniqueName);

                /*if (isCart) {
                    DataSaver.s.GetCurrentSave().metaProgress.justBoughtCart = curUniqueName;
                } else {
                    DataSaver.s.GetCurrentSave().metaProgress.justBoughtArtifact = curUniqueName;
                }*/
                
                DataSaver.s.SaveActiveGame();
                
                Sold();
            }
        }
    }
}
