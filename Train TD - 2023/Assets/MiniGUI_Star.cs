using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_Star : MonoBehaviour {

	public Image starImg;
	public Image glowImg;

	public Color regularColor = Color.magenta;
	public Color playerHereColor = Color.cyan;

	public GameObject gfx;
	public float randomOffsetMagnitude = 0.5f;
    
	public StarState myInfo;
    
	public void Initialize(StarState info) {
		myInfo = info;
		
		gfx.transform.localPosition = Random.insideUnitCircle * randomOffsetMagnitude;

		starImg.sprite = DataHolder.s.GetCityScriptable(info.city.uniqueName).sprite;
        
		if(myInfo.isPlayerHere)
			SetPlayerHere();
        
		if(myInfo.isBoss)
			SetBossStar();
        
		if(myInfo.isShop)
			SetShopStar();
        
	}

	public GameObject playerIndicator;
	public Sprite shopIndicator;
	public Sprite bossIndicator;


	public bool currentlyPlayerTravelable = false;

	private void SetPlayerHere() {
		playerIndicator.SetActive(true);
		glowImg.color = playerHereColor;
	}
    
	private void SetShopStar() {
		starImg.sprite = shopIndicator;
	}

	private void SetBossStar() {
		//starImg.sprite = bossIndicator;
		starImg.color = Color.red;
	}

	public void ClickOnStar() {
		if (currentlyPlayerTravelable) {
			MapController.s.ShowStarInfo(myInfo);
		}
	}
}