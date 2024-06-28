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
	    regularBullet = 0, cannonBullet = 1, rocket = 2, lazer = 3, flamethrowerFire = 4, steamFire = 5, scrapBullet = 6, stickyBullet = 7, shotgunPellets = 8, gatlingBullet = 9, railgunBullet = 10,
    }
    
    public enum EnemyProjectileTypes {
	    regularBullet = 0, cannonBullet = 1, rocket = 2, ammoHitter = 3, gigaGatling=4, heal=5, stickyBullet = 7, arrow=8, explodingArrow=9
    }

    
    public ProjectileComboHolder[] projectiles = new ProjectileComboHolder[8];
    public EnemyProjectileComboHolder[] enemyProjectiles = new EnemyProjectileComboHolder[4];

    
    [Serializable]
    public class ProjectileComboHolder {
	    public ProjectileTypes myType;
	    [HorizontalGroup("row1")]
	    public GameObject muzzleFlash;
	    [HorizontalGroup("row1")]
	    public GameObject regularBullet;
	    [HorizontalGroup("row1")]
	    public GameObject fireBullet;

	    [HideInInspector] public ObjectPool muzzlePool;
	    [HideInInspector] public ObjectPool bulletPool;
	    [HideInInspector] public ObjectPool firePool;
    }
    
    [Serializable]
    public class EnemyProjectileComboHolder {
	    public EnemyProjectileTypes myType;
	    [HorizontalGroup("row1")]
	    public GameObject muzzleFlash;
	    [HorizontalGroup("row1")]
	    public GameObject regularBullet;
	    
	    
	    [HideInInspector] public ObjectPool muzzlePool;
	    [HideInInspector] public ObjectPool bulletPool;
    }


    public GameObject poolTemplate;

    private static float muzzleFlashLifeTime = 1f;
    public static float bulletAfterDeathLifetime = 1f;
    
    private void Start() {
	    for (int i = 0; i < projectiles.Length; i++) {
		    var curProjectiles = projectiles[i];
		    var muzzlePool = Instantiate(poolTemplate, transform).GetComponent<ObjectPool>();
		    var bulletPool = Instantiate(poolTemplate, transform).GetComponent<ObjectPool>();
		    var firePool = Instantiate(poolTemplate, transform).GetComponent<ObjectPool>();
		    
		    muzzlePool.RePopulateWithNewObject(curProjectiles.muzzleFlash);
		    curProjectiles.muzzlePool = muzzlePool;
		    muzzlePool.gameObject.name = curProjectiles.myType + " muzzle";
		    
		    bulletPool.RePopulateWithNewObject(curProjectiles.regularBullet);
		    curProjectiles.bulletPool = bulletPool;
		    bulletPool.gameObject.name = curProjectiles.myType + " bullet";
		    
		    firePool.RePopulateWithNewObject(curProjectiles.fireBullet);
		    curProjectiles.firePool = firePool;
		    firePool.gameObject.name = curProjectiles.myType+ " fire";
	    }
	    
	    for (int i = 0; i < enemyProjectiles.Length; i++) {
		    var curProjectiles = enemyProjectiles[i];
		    var muzzlePool = Instantiate(poolTemplate, transform).GetComponent<ObjectPool>();
		    var bulletPool = Instantiate(poolTemplate, transform).GetComponent<ObjectPool>();
		    
		    muzzlePool.RePopulateWithNewObject(curProjectiles.muzzleFlash);
		    curProjectiles.muzzlePool = muzzlePool;
		    muzzlePool.gameObject.name = "enemy " + curProjectiles.myType+ " muzzle";
		    
		    bulletPool.RePopulateWithNewObject(curProjectiles.regularBullet);
		    curProjectiles.bulletPool = bulletPool;
		    bulletPool.gameObject.name = "enemy " + curProjectiles.myType+ " bullet";
	    }
    }

    public GameObject GetProjectile(ProjectileTypes myType, float fire, Vector3 position, Quaternion rotation) {
	    var combo = projectiles[0];
	    for (int i = 0; i < projectiles.Length; i++) {
		    if (projectiles[i].myType == myType) {
			    combo = projectiles[i];
			    break;
		    }
	    }
	    
	    var result = combo.bulletPool;
	    
	    if (fire > 0) {
		    result = combo.firePool;
	    }

	    var myObj = result.Spawn(position, rotation);
	    return myObj;
    }
    
    public GameObject GetMuzzleFlash (ProjectileTypes myType, Vector3 position, Quaternion rotation, Transform parent, VisualEffectsController.EffectPriority priority = VisualEffectsController.EffectPriority.High) {
	    var combo = projectiles[0];
	    for (int i = 0; i < projectiles.Length; i++) {
		    if (projectiles[i].myType == myType) {
			    combo = projectiles[i];
			    break;
		    }
	    }
	    
	    var result = combo.muzzlePool;
	    
		var myObj = result.Spawn(position, rotation);
		myObj.GetComponent<PooledObject>().lifeTime = muzzleFlashLifeTime;

		var oneshotSound = myObj.GetComponentInChildren<FMODOneShotSource>();
		if(oneshotSound)
			oneshotSound.Play();
		
		return myObj;
    }
    
    
    public GameObject GetEnemyProjectile(EnemyProjectileTypes myType, Vector3 position, Quaternion rotation) {
	    var combo = enemyProjectiles[0];
	    for (int i = 0; i < enemyProjectiles.Length; i++) {
		    if (enemyProjectiles[i].myType == myType) {
			    combo = enemyProjectiles[i];
			    break;
		    }
	    }
	    
	    var result = combo.bulletPool;

	    var myObj = result.Spawn(position, rotation);
	    return myObj;
    }
    
    public GameObject GetEnemyMuzzleFlash (EnemyProjectileTypes myType, Vector3 position, Quaternion rotation, Transform parent, VisualEffectsController.EffectPriority priority = VisualEffectsController.EffectPriority.High) {
	    var combo = enemyProjectiles[0];
	    for (int i = 0; i < enemyProjectiles.Length; i++) {
		    if (enemyProjectiles[i].myType == myType) {
			    combo = enemyProjectiles[i];
			    break;
		    }
	    }
	    
	    
	    var result = combo.muzzlePool;
	    
	    var myObj = result.Spawn(position, rotation);
	    myObj.GetComponent<PooledObject>().lifeTime = muzzleFlashLifeTime;
	    
	    var oneshotSound = myObj.GetComponentInChildren<FMODOneShotSource>();
	    if(oneshotSound)
		    oneshotSound.Play();
	    
	    return myObj;
    }
}
