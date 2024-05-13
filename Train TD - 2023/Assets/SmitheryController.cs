using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class SmitheryController : MonoBehaviour
{
    
    public SnapLocation location1;
    public SnapLocation location2;

    public GameObject allParent;

    public Mini_Smithery smithery;

    public ScrapPaymentSlot paymentSlot;

    public bool upgradeUsed = false;

    private void Start() {
        smithery.OnStuffCollided.AddListener(UpgradeDone);
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isDragging() && !upgradeUsed) {
            if (!location1.IsEmpty() && !location2.IsEmpty() /*&& paymentSlot.AllSlotsFull()*/)
                CheckAndDoUpgrade();
        }

        location1.allowSnap = true;
        location2.allowSnap = true;
    }


    private bool isCartUpgrade = false;
    void CheckAndDoUpgrade() {
        var cart1 = location1.GetComponentInChildren<Cart>();
        var cart2 = location2.GetComponentInChildren<Cart>();

        

        if (cart1 != null && cart2 != null) {
            var upgradeResult = DataHolder.s.GetMergeResult(cart1.uniqueName, cart2.uniqueName);
            if ( DataHolder.s.IsLegalMergeResult(upgradeResult)) {
                EngageUpgrade(true);
                return;
            }
        }
        

        // if cannot upgrade then unsnap the carts
        
        if (cart1 != null) {
            cart1.transform.SetParent(null);
            cart1.GetComponent<Rigidbody>().isKinematic = false;
            cart1.GetComponent<Rigidbody>().useGravity = true;
            cart1.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());
        }
        if (cart2 != null) {
            cart2.transform.SetParent(null);
            cart2.GetComponent<Rigidbody>().isKinematic = false;
            cart2.GetComponent<Rigidbody>().useGravity = true;
            cart2.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());
        }
    }

    Vector3 GetRandomYeetForce() {
        var randomForceDirection = Random.onUnitSphere;
        if (randomForceDirection.y < 0) {
            randomForceDirection.y = -randomForceDirection.y;
        }

        randomForceDirection.y = randomForceDirection.y.Remap(0, 1, 0.7f, 1f);
        randomForceDirection.Normalize();

        return randomForceDirection * Random.Range(1, 1.5f)*1500;
    }
    
    
    
    public float rotateSpeed = 20;
    public float rotateAcceleration = 20;

    private bool isEngaged = false;
    
    void EngageUpgrade(bool _isCartUpgrade) {
        isCartUpgrade = _isCartUpgrade;
        isEngaged = true;
        
        PlayerWorldInteractionController.s.Deselect();
        SetColliderStatus(allParent, false);
        smithery.EngageAnim();
        
        //paymentSlot.DoPayment();
    }

    void UpgradeDone() {
        SetColliderStatus(allParent, true);
        isEngaged = false;
        var cart1 = location1.GetComponentInChildren<Cart>();
        var cart2 = location2.GetComponentInChildren<Cart>();
        
        var upgradeResult = DataHolder.s.GetMergeResult(cart1.uniqueName, cart2.uniqueName);

        var newCart = Instantiate(DataHolder.s.GetCart(upgradeResult).gameObject, cart1.transform.position, cart1.transform.rotation).GetComponent<Cart>();
        Train.ApplyStateToCart(newCart, new DataSaver.TrainState.CartState(){uniqueName = upgradeResult});
        ShopStateController.s.AddCartToShop(newCart);
        newCart.GetComponent<Rigidbody>().isKinematic = false;
        newCart.GetComponent<Rigidbody>().useGravity = true;
        newCart.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());
        
        ShopStateController.s.RemoveCartFromShop(cart1);
        ShopStateController.s.RemoveCartFromShop(cart2);
        Destroy(cart1);
        Destroy(cart2);
        
        MakeCannotMergeAnymore();

        Train.s.TrainChanged();
    }


    void MakeCannotMergeAnymore() {
        Destroy(location1.gameObject);
        Destroy(location2.gameObject);
        this.enabled = false;
    }

    void SetColliderStatus(GameObject target, bool status) {
        var allColliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++) {
            allColliders[i].enabled = status;
        }
    }
    
    private static IEnumerable GetAllModuleNames() {
        var buildings = GameObject.FindObjectOfType<DataHolder>().buildings;
        var buildingNames = new List<string>();
        buildingNames.Add("");
        for (int i = 0; i < buildings.Length; i++) {
            buildingNames.Add(buildings[i].uniqueName);
        }
        return buildingNames;
    }
    
}
