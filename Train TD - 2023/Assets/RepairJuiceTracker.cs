using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class RepairJuiceTracker : MonoBehaviour
{
    [ShowInInspector]
    public List<IRepairJuiceProvider> juiceProviders = new List<IRepairJuiceProvider>();

    public float GetJuicePercent() {
        return Mathf.Clamp01(GetCurrentJuice() / GetJuiceCapacity());
    }
    
    public float GetCurrentJuice() {
        var currentJuice = 0f;

        for (int i = 0; i < juiceProviders.Count; i++) {
            currentJuice += juiceProviders[i].AvailableJuice();
        }

        return currentJuice;
    }

    public float GetJuiceCapacity() {
        var juiceCapacity = 0f;

        for (int i = 0; i < juiceProviders.Count; i++) {
            juiceCapacity += juiceProviders[i].JuiceCapacity();
        }

        return juiceCapacity;
    }


    public void RegisterJuiceProviders() {
        juiceProviders = new List<IRepairJuiceProvider>(GetComponentsInChildren<IRepairJuiceProvider>(true));
    }


    public void FillJuice(float fillAmount) {
        for (int i = 0; i < juiceProviders.Count; i++) {
            fillAmount = juiceProviders[i].FillJuice(fillAmount);
            if (fillAmount <= 0) {
                break;
            }
        }
    }

    public bool HasJuice(float amount) {
        return GetCurrentJuice() >= amount;
    }

    public void UseJuice(float amount) {
        MiniGUI_RepairJuiceBar.s.JuiceChanged();
        for (int i = 0; i < juiceProviders.Count; i++) {
            amount = juiceProviders[i].UseJuice(amount);
            if (amount <= 0) {
                break;
            }
        }
    }
}

public interface IRepairJuiceProvider {
    public float AvailableJuice();
    public float JuiceCapacity();
    public float UseJuice(float amountUsed); // returns remainder if cannot satisfy all juice
    public float FillJuice(float amountToFill); // returns remainder if any
}