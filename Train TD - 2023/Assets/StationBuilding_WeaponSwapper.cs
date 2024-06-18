using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationBuilding_WeaponSwapper : MonoBehaviour, IResetShopBuilding{

    public SnapLocation myTarget;

    public GameObject scrappedPoofPrefab;
    
    private bool isEngaged = false;
    
    public  void CheckIfShouldEnableSelf() {
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct != 1) {
            gameObject.SetActive(false);
        }else {
            gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!isEngaged && !myTarget.IsEmpty() && !PlayerWorldInteractionController.s.isDragging()) {
            CheckAndSwapGun();
        }
    }


    void CheckAndSwapGun() {
        var cart = myTarget.GetComponentInChildren<Cart>();

        if (DataHolder.s.GetTier1Gun(cart.uniqueName) != null) {
            EngageEffect();
            return;
        }
        

        // if cannot upgrade then unsnap the carts
        var thing1 = myTarget.GetSnappedObject().gameObject;
        if (thing1 != null) {
            thing1.transform.SetParent(null);
            thing1.GetComponent<Rigidbody>().isKinematic = false;
            thing1.GetComponent<Rigidbody>().useGravity = true;
            thing1.GetComponent<Rigidbody>().AddForce(SmitheryController.GetRandomYeetForce());
        }
    }

    void EngageEffect() {
        isEngaged = true;
        
        PlayerWorldInteractionController.s.Deselect();
        SetColliderStatus(myTarget.gameObject, false);
        
        StartCoroutine(_Effect());
    }

    IEnumerator _Effect() {
        yield return null;

        var target = myTarget.GetSnappedObject();
        //var scrapable = target.GetComponent<Scrapable>();

        var spawnPos = myTarget.transform.position;

        /*for (int i = 0; i < scrapable.scrapAwards.Count; i++) {
            var scrap = Instantiate(LevelReferences.s.scrapsItemPrefab, spawnPos, Random.rotation);
            scrap.GetComponent<ScrapsItem>().SetScrapsType(scrapable.scrapAwards[i]);

            var randomUpDirection = Random.onUnitSphere;
            randomUpDirection.y = Mathf.Abs(randomUpDirection.y);
            randomUpDirection.y = Mathf.Clamp(randomUpDirection.y, 0.1f,0.6f);
            randomUpDirection.Normalize();
                
            var scrapRB = scrap.GetComponent<Rigidbody>();

            scrapRB.isKinematic = false;
            scrapRB.useGravity = true;
            scrapRB.AddForce(randomUpDirection*Random.Range(50,100));
            scrapRB.AddTorque(Random.onUnitSphere*Random.Range(100,200));
            
        }*/

        var allTier1Guns = DataHolder.s.swappableTier1Guns;
        var myIndex = -1;
        var cart = myTarget.GetComponentInChildren<Cart>();

        for (int i = 0; i < allTier1Guns.Length; i++) {
            if (cart.uniqueName == allTier1Guns[i].gunUniqueName) {
                myIndex = i;
                break;
            }
        }

        var newIndex = (myIndex + 1) % allTier1Guns.Length;
        
        var rewardName = allTier1Guns[newIndex].gunUniqueName;
        var newCart = Instantiate(DataHolder.s.GetCart(rewardName), target.transform.position, target.transform.rotation).GetComponent<Cart>();
        Train.ApplyStateToCart(newCart, new DataSaver.TrainState.CartState(){uniqueName = rewardName});
        ShopStateController.s.AddCartToShop(newCart);
        newCart.GetComponent<Rigidbody>().isKinematic = false;
        newCart.GetComponent<Rigidbody>().useGravity = true;
        newCart.GetComponent<Rigidbody>().AddForce(SmitheryController.GetRandomYeetForce());

        Instantiate(scrappedPoofPrefab, spawnPos, Quaternion.identity);
        
        ShopStateController.s.RemoveCartFromShop(target.GetComponent<Cart>());
        Destroy(target.gameObject);
        EffectDone();
        yield return null;
    }


    void EffectDone() { 
        SetColliderStatus(myTarget.gameObject, true);
        isEngaged = false;
        
    }

    void SetColliderStatus(GameObject target, bool status) {
        var allColliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++) {
            allColliders[i].enabled = status;
        }
    }
}
