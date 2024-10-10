using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class SmitheryController : MonoBehaviour, IResetShopBuilding
{
    
    public SnapLocation location1;
    public SnapLocation location2;

    public GameObject allParent;

    public Mini_Smithery smithery;

    public ScrapPaymentSlot paymentSlot;

    private void Start() {
        smithery.OnStuffCollided.AddListener(UpgradeDone);
    }

    public void CheckIfShouldEnableSelf() {
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
            gameObject.SetActive(false);
        }else {
            gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isDragging()) {
            if (!location1.IsEmpty() && !location2.IsEmpty() && paymentSlot.AllSlotsFull())
                CheckAndDoUpgrade();
        }

        location1.allowSnap = true;
        location2.allowSnap = true;
    }


    private bool isCartUpgrade = false;
    private MergeData mergeData;
    void CheckAndDoUpgrade() {
        var cart1 = location1.GetComponentInChildren<Cart>();
        var cart2 = location2.GetComponentInChildren<Cart>();

        mergeData = null;

        if (cart1 != null && cart2 != null) {
            mergeData = DataHolder.s.GetMergeData(cart1.uniqueName, cart2.uniqueName);
            if ( mergeData != null) {
                EngageUpgrade(true);
                return;
            }
        }

        var artifact1 = location1.GetComponentInChildren<Artifact>();
        var artifact2 = location2.GetComponentInChildren<Artifact>();

        if (artifact1 != null || artifact2 != null) {
            if (cart1 != null) {
                mergeData = DataHolder.s.GetMergeWithAnyGemData(cart1.uniqueName);
                if ( mergeData != null) {
                    EngageUpgrade(false);
                    return;
                }
            }

            if (cart2 != null) {
                mergeData = DataHolder.s.GetMergeWithAnyGemData(cart2.uniqueName);
                if ( mergeData != null) {
                    EngageUpgrade(false);
                    return;
                }
            }
        }
        

        // if cannot upgrade then unsnap the carts

        GameObject thing1 = null;
        if (cart1 != null) {
            thing1 = cart1.gameObject;
        } else if(artifact1 != null) {
            thing1 = artifact1.gameObject;
        }

        GameObject thing2 = null;
        if (cart2 != null) {
            thing2 = cart2.gameObject;
        } else if(artifact2 != null) {
            thing2 = artifact2.gameObject;
        }
        
        if (thing1 != null) {
            thing1.transform.SetParent(null);
            thing1.GetComponent<Rigidbody>().isKinematic = false;
            thing1.GetComponent<Rigidbody>().useGravity = true;
            thing1.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());
        }
        if (thing2 != null) {
            thing2.transform.SetParent(null);
            thing2.GetComponent<Rigidbody>().isKinematic = false;
            thing2.GetComponent<Rigidbody>().useGravity = true;
            thing2.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());
        }
    }

    public static Vector3 GetRandomYeetForce() {
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
        
        paymentSlot.DoPayment();
    }

    void UpgradeDone() {
        isEngaged = false;
        var thing1 = location1.GetComponentInChildren<IPlayerHoldable>() as MonoBehaviour;
        var thing2 = location2.GetComponentInChildren<IPlayerHoldable>() as MonoBehaviour;

        var cartState = new DataSaver.TrainState.CartState(){uniqueName = mergeData.result};
        var newCart = Train.InstantiateCartFromState(cartState, thing1.transform.position, thing1.transform.rotation);
        ShopStateController.s.AddCartToShop(newCart);
        newCart.GetComponent<Rigidbody>().isKinematic = false;
        newCart.GetComponent<Rigidbody>().useGravity = true;
        newCart.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());

        if (mergeData.hasBonusGem) {
            var artifactState = new DataSaver.TrainState.ArtifactState() { uniqueName = mergeData.bonusGem };
            var newArtifact = Train.InstantiateArtifactFromState(artifactState, thing1.transform.position, thing1.transform.rotation);
            ShopStateController.s.AddArtifactToShop(newArtifact);
            newArtifact.GetComponent<Rigidbody>().isKinematic = false;
            newArtifact.GetComponent<Rigidbody>().useGravity = true;
            newArtifact.GetComponent<Rigidbody>().AddForce(GetRandomYeetForce());
        }

        if (thing1 is Cart) {
            ShopStateController.s.RemoveCartFromShop(thing1 as Cart);

            foreach (var artifactLocation in (thing1 as Cart).myArtifactLocations) {
                if (!artifactLocation.IsEmpty()) {
                    SetColliderStatus(artifactLocation.gameObject, true);
                    artifactLocation.GetComponentInChildren<Artifact>().DetachFromCart();
                }
            }
            
        }else if (thing1 is Artifact) {
            ShopStateController.s.RemoveArtifactFromShop(thing1 as Artifact);
        }
        if (thing2 is Cart) {
            ShopStateController.s.RemoveCartFromShop(thing2 as Cart);
            
            foreach (var artifactLocation in (thing2 as Cart).myArtifactLocations) {
                if (!artifactLocation.IsEmpty()) {
                    SetColliderStatus(artifactLocation.gameObject, true);
                    artifactLocation.GetComponentInChildren<Artifact>().DetachFromCart();
                }
            }
            
        }else if (thing2 is Artifact) {
            ShopStateController.s.RemoveArtifactFromShop(thing2 as Artifact);
        }
        Destroy(thing1.gameObject);
        Destroy(thing2.gameObject);
        
        //MakeCannotMergeAnymore();

        Invoke(nameof(CanCollideAgain), 0.2f);
    }

    void CanCollideAgain() {
        Train.s.TrainChanged();
        SetColliderStatus(allParent, true);
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
