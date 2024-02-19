using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tier2GunModuleSpawner : MonoBehaviour {
    public Transform spawnLocation;

    public void SpawnGun(string gunName) {
        GunModule gunToSpawn = DataHolder.s.GetTier1Gun(gunName);

        if (gunToSpawn != null) {
            var gun = Instantiate(gunToSpawn.gameObject, spawnLocation);
            gun.transform.ResetTransformation();
        }

        GetComponentInParent<Cart>().uniqueName = gunToSpawn.gunUniqueName;
        GetComponentInParent<Cart>().displayName = gunToSpawn.GetComponent<ClickableEntityInfo>().info;
    }
}
