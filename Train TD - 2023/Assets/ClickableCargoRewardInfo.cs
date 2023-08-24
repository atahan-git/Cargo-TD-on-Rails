using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableCargoRewardInfo : MonoBehaviour, IClickableInfo {
    private CargoModule myModule;
    
    private void Start() {
        myModule = GetComponent<CargoModule>();
    }

    public bool ShowInfo() {
        return true;
    }

    public string GetInfo() {
        return $"reward: +{myModule.GetState().cargoReward}";
    }

    public Tooltip GetTooltip() {
        return new Tooltip() { text = $"Bring this cargo safely to the next city to earn {myModule.GetState().cargoReward} at level {myModule.GetState().cargoLevel}. You will also get a surprise relic." };;
    }

}
