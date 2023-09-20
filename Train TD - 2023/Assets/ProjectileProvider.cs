using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ProjectileProvider : MonoBehaviour {
    public static ProjectileProvider s;

    private void Awake() {
	    s = this;
    }

 
    public enum ProjectileTypes {
	    regularBullet = 0, cannonBullet = 1, rocket = 2, lazer = 3, flamethrowerFire = 4, steamFire = 5, scrapBullet = 6, stickyBullet = 7,
    }

    
    public ProjectileComboHolder[] projectiles = new ProjectileComboHolder[8];

    
    [Serializable]
    public class ProjectileComboHolder {
	    public ProjectileTypes myType;
	    [HorizontalGroup("row1")]
	    public GameObject muzzleFlash;
	    [HorizontalGroup("row1")]
	    public GameObject regularBullet;
	    [HorizontalGroup("row1")]
	    public GameObject gatlingMuzzleFlash;
	    [HorizontalGroup("row2")]
	    public GameObject fireBullet;
	    [HorizontalGroup("row2")]
	    public GameObject stickyBullet;
	    [HorizontalGroup("row2")]
	    public GameObject fireAndStickyBullet;
    }


    public GameObject GetProjectile(ProjectileTypes myType, bool isFire, bool isSticky) {
	    var combo = projectiles[0];
	    for (int i = 0; i < projectiles.Length; i++) {
		    if (projectiles[i].myType == myType) {
			    combo = projectiles[i];
			    break;
		    }
	    }
	    
	    var result = combo.regularBullet;
	    /*if (isFire && isSticky) {
		    result = combo.fireAndStickyBullet;
	    }else if (isFire) {
		    result =  combo.fireBullet;
	    }else if (isSticky) {
		    result =  combo.stickyBullet;
	    } else {
		    result =  combo.regularBullet;
	    }*/

	    if (result == null) {
		    result = combo.regularBullet;
	    }

	    return result;
    }
    
    public GameObject GetMuzzleFlash (ProjectileTypes myType, bool isGatling, bool isFire, bool isSticky) {
	    var combo = projectiles[0];
	    for (int i = 0; i < projectiles.Length; i++) {
		    if (projectiles[i].myType == myType) {
			    combo = projectiles[i];
			    break;
		    }
	    }
	    
	    
	    var result = combo.muzzleFlash;

	    if (isGatling) {
		    result = combo.gatlingMuzzleFlash;
	    }

	    if (result == null)
		    result = combo.muzzleFlash;
	    
	    
	    return result;
    }
}
