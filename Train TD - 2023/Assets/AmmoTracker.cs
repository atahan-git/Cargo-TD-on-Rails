using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoTracker : MonoBehaviour {
    public List<IAmmoProvider> ammoProviders = new List<IAmmoProvider>();
}
