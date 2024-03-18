using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeaponShoot : MonoBehaviour {
    public Transform rotateTransform;
    public Transform barrelEnd;
    public float projectileSpawnOffset = 0.2f;

    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;


    public Transform target;


    public float fireDelay = 2f;
    public int fireBarrageCount = 5;
    public float fireBarrageDelay = 0.1f;


    private void Start() {
        StartCoroutine(ShootCycle());
    }

    private void Update() {
        rotateTransform.LookAt(target, Vector3.up);
    }


    IEnumerator ShootCycle() {
        while (true) {
            StartCoroutine(ShootBarrage());

            yield return new WaitForSeconds(fireDelay);
        }
    }

    IEnumerator ShootBarrage() {
        for (int i = 0; i < fireBarrageCount; i++) {
            var position = barrelEnd.position;
            var rotation = barrelEnd.rotation;
            var bullet = VisualEffectsController.s.SmartInstantiate(bulletPrefab, position + barrelEnd.forward * projectileSpawnOffset, rotation);
            var muzzleFlash = VisualEffectsController.s.SmartInstantiate(muzzleFlashPrefab, position, rotation);
            bullet.GetComponent<Projectile>().myOriginObject = this.transform.root.gameObject;

            yield return new WaitForSeconds(fireBarrageDelay);
        }
    }
}
