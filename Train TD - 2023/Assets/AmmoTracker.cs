using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class AmmoTracker : MonoBehaviour {
    
    [ShowInInspector]
    public List<IAmmoProvider> ammoProviders = new List<IAmmoProvider>();

    public float GetAmmoPercent() {
        var currentAmmo = 0f;
        var ammoCapacity = 0f;

        for (int i = 0; i < ammoProviders.Count; i++) {
            currentAmmo += ammoProviders[i].AvailableAmmo();
            ammoCapacity += ammoProviders[i].AmmoCapacity();
        }

        return Mathf.Clamp01(currentAmmo / ammoCapacity);
    }


    public void RegisterAmmoProviders() {
        ammoProviders = new List<IAmmoProvider>(GetComponentsInChildren<IAmmoProvider>(true));
    }
}
