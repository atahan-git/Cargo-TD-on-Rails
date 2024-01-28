using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalRangeShower : MonoBehaviour {
    public static PhysicalRangeShower s;

    private void Awake() {
        s = this;
    }


    public GameObject canAffectPrefab;
    
    public GameObject[] cartEffectRanges;

    public GameObject[] artifactEffectRanges;

    [ColorUsageAttribute(true, true)] public Color artifactGoodColor = Color.magenta;
    [ColorUsageAttribute(true, true)] public Color artifactBadColor = Color.magenta;
    

    public Cart artifactFrontCart;
    public Cart artifactBackCart;
    public Cart cartFrontCart;
    public Cart cartBackCart;
    public bool isArtifact;
    public bool isBooster;

    public void ShowArtifactRange(Artifact artifact, bool resetPos) {
        return;
        var cart = artifact.GetComponentInParent<Cart>();
        if (cart != null) {
            isArtifact = true;
        } else {
            isArtifact = false;
            return;
        }
        
        if (artifact.range <= 0) {
            isArtifact = false;
        }

        var artifactColor = Color.white;
        if (isArtifact) {
            //artifactColor = artifact.isGoodEffect ? artifactGoodColor : artifactBadColor;
            var range = artifact.range;
            
            artifactFrontCart = Train.s.GetNextBuilding(range, cart);
            artifactBackCart = Train.s.GetNextBuilding(-range, cart);
            while (artifactFrontCart == null || artifactBackCart == null) {
                range -= 1;
                if(artifactFrontCart == null)
                    artifactFrontCart =Train.s.GetNextBuilding(range, cart);
                if(artifactBackCart == null)
                    artifactBackCart = Train.s.GetNextBuilding(-range, cart);
            }
        }
        
        var cartBasePos = cart.uiTargetTransform.position;
        for (int i = 0; i < artifactEffectRanges.Length; i++) {
            if(resetPos)
                artifactEffectRanges[i].transform.position = cartBasePos;

            artifactEffectRanges[i].SetActive(isArtifact);
            artifactEffectRanges[i].GetComponent<MeshRenderer>().material.SetColor(Emission, artifactColor);
        }
    }
    public void ShowCartRange(Cart cart, bool resetPos) {
        if (cart.GetComponentInParent<Train>() == null) {
            HideRange();
            return;
        }
        
        var booster = cart.GetComponentInChildren<IBooster>();
        if (booster != null) {
            isBooster = true;
        }

        var boosterColor = Color.white;
        if (isBooster) {
            boosterColor = booster.GetColor();
            var range = booster.GetRange();
            cartFrontCart = Train.s.GetNextBuilding(range, cart);
            cartBackCart = Train.s.GetNextBuilding(-range, cart);
            while (cartFrontCart == null || cartBackCart == null && range >= 0) {
                range -= 1;
                if(cartFrontCart == null)
                    cartFrontCart =Train.s.GetNextBuilding(range, cart);
                if(cartBackCart == null)
                    cartBackCart = Train.s.GetNextBuilding(-range, cart);
            }
        }

        var artifact = cart.GetComponentInChildren<Artifact>();
        if (artifact != null) {
            ShowArtifactRange(artifact, resetPos);
        } else {
            isArtifact = false;
        }


        var cartBasePos = cart.uiTargetTransform.position;
        for (int i = 0; i < cartEffectRanges.Length; i++) {
            if(resetPos)
                cartEffectRanges[i].transform.position = cartBasePos;
            cartEffectRanges[i].SetActive(isBooster);
            cartEffectRanges[i].GetComponent<MeshRenderer>().material.SetColor(Emission, boosterColor);
        }

        
    }


    public void HideRange() {
        isArtifact = false;
        isBooster = false;
        for (int i = 0; i < cartEffectRanges.Length; i++) {
            cartEffectRanges[i].SetActive(isBooster);
        }

        for (int i = 0; i < artifactEffectRanges.Length; i++) {
            artifactEffectRanges[i].SetActive(isArtifact);
        }
    }

    public float boosterLerpSpeed = 5f;
    public float artifactLerpSpeed = 3f;
    private static readonly int Emission = Shader.PropertyToID("_Emission");

    private void Update() {
        if (isBooster) {
            var cartFrontPos = cartFrontCart.uiTargetTransform.position + (Vector3.forward * (cartFrontCart.length / 2f));
            var cartBackPos = cartBackCart.uiTargetTransform.position + (-Vector3.forward * (cartBackCart.length / 2f));
            cartEffectRanges[0].transform.position = Vector3.Lerp(cartEffectRanges[0].transform.position, cartFrontPos, boosterLerpSpeed*Time.deltaTime);
            cartEffectRanges[1].transform.position = Vector3.Lerp(cartEffectRanges[1].transform.position, cartBackPos, boosterLerpSpeed*Time.deltaTime);
        }

        if (isArtifact) {
            var artifactFrontPos = artifactFrontCart.uiTargetTransform.position + (Vector3.forward * (artifactFrontCart.length / 2f));
            var artifactBackPos = artifactBackCart.uiTargetTransform.position + (-Vector3.forward * (artifactBackCart.length / 2f));
            artifactEffectRanges[0].transform.position = Vector3.Lerp(artifactEffectRanges[0].transform.position, artifactFrontPos, artifactLerpSpeed*Time.deltaTime);
            artifactEffectRanges[1].transform.position = Vector3.Lerp(artifactEffectRanges[1].transform.position, artifactBackPos, artifactLerpSpeed*Time.deltaTime);
        }
    }
}


