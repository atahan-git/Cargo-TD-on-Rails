using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DioramaEffectDescription : MonoBehaviour, IGenericInfoProvider {
    public Sprite icon;
    [Multiline]
    public string myDescription;

    public Sprite GetSprite() {
        return icon;
    }

    public string GetDescription() {
        return myDescription;
    }
}
