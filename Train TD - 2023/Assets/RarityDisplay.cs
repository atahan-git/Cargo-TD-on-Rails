using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RarityDisplay : MonoBehaviour {

    public Material common;
    public Material rare;
    public Material epic;
    public Material special;
    public Material boss;
    
    // Start is called before the first frame update
    void Start() {
        var cart = GetComponentInParent<Cart>();
        var artifact = GetComponentInParent<Artifact>();
        if(cart == null && artifact == null)
            return;

        UpgradesController.CartRarity rarity = UpgradesController.CartRarity.common;
        if (cart)
            rarity = cart.myRarity;
        if (artifact)
            rarity = artifact.myRarity;

        var renderers = GetComponentsInChildren<MeshRenderer>();
        Material material = common;

        switch (rarity) {
            case UpgradesController.CartRarity.epic:
                material = epic;
                break;
            case UpgradesController.CartRarity.rare:
                material = rare;
                break;
            case UpgradesController.CartRarity.special :
                material = special;
                break;
            case UpgradesController.CartRarity.boss:
                material = boss;
                break;
        }

        for (int i = 0; i < renderers.Length; i++) {
            renderers[i].material = material;
        }
    }
}
