using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_Pick3CartReward : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public Image icon;

    public string myItemUniqueName;
    public void SetUp(string uniqueName) {
        myItemUniqueName = uniqueName;

        var cart = DataHolder.s.GetCart(myItemUniqueName);
        if (cart.uniqueName[0] == '-') { // gun cart
            var gun = DataHolder.s.GetTier1Gun(myItemUniqueName);

            if (gun == null) {
                gun = DataHolder.s.GetTier2Gun(myItemUniqueName);
            }

            var info = gun.GetComponent<ClickableEntityInfo>();

            title.text = info.info;
            description.text = info.tooltip.text;
            icon.sprite = gun.gunSprite;
        } else {
            title.text = cart.displayName;
            description.text = cart.GetComponentInChildren<ClickableEntityInfo>().tooltip.text;
            icon.sprite = cart.Icon;
        }
        
        
    }


    public void Select() {
        var cart = Instantiate(DataHolder.s.GetCart(myItemUniqueName).gameObject, StopAndPick3RewardUIController.s.instantiatePos).GetComponent<Cart>();
        Train.ApplyStateToCart(cart, new DataSaver.TrainState.CartState(){uniqueName = myItemUniqueName});
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.goodItemSpawnEffectPrefab, StopAndPick3RewardUIController.s.instantiatePos);

        
        cart.GetComponent<Rigidbody>().isKinematic = false;
        cart.GetComponent<Rigidbody>().useGravity = true;
        
        LevelReferences.s.combatHoldableThings.Add(cart);
        
        StopAndPick3RewardUIController.s.RewardWasPicked();
    }
}
