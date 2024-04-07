using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationBuilding_Scrapper : MonoBehaviour {

    public SnapLocation myTarget;

    public GameObject scrappedPoofPrefab;
    
    
    private bool isEngaged = false;

    void Update()
    {
        if (!isEngaged && !myTarget.IsEmpty() && !PlayerWorldInteractionController.s.isDragging()) {
            EngageEffect();
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
        var scrapable = target.GetComponent<Scrapable>();

        var spawnPos = myTarget.transform.position;

        for (int i = 0; i < scrapable.scrapAwards.Count; i++) {
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
            
        }

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
