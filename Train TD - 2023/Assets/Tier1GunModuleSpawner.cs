using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tier1GunModuleSpawner : MonoBehaviour {
    public Transform[] spawnLocations = new Transform[2];

    public void SpawnGuns(string gunName) {
        GunModule gunToSpawn = DataHolder.s.GetTier1Gun(gunName);

        if (gunToSpawn != null) {
            for (int i = 0; i < spawnLocations.Length; i++) {
                var gun = Instantiate(gunToSpawn.gameObject, spawnLocations[i]);
                gun.transform.ResetTransformation();
                gun.transform.localScale = Vector3.one*0.8f;
            }
        }

        GetComponentInParent<Cart>().uniqueName = gunToSpawn.gunUniqueName;
        GetComponentInParent<Cart>().displayName = gunToSpawn.GetComponent<ClickableEntityInfo>().info;
    }
}
