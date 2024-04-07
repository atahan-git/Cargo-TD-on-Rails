using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EngineModule : MonoBehaviour, IActiveDuringCombat, IActiveDuringShopping, IResetState {
   public float speedAdd = 6;
   public float extraSpeedAdd = 1;
   public int enginePower = 10;
   public int extraEnginePower = 0;

   public float GetSpeedAdd() {
      return (speedAdd + extraSpeedAdd) * GetEffectivePressure();
   }

   public float GetEnginePower() {
      return (enginePower + extraEnginePower) * GetEffectivePressure();
   }

   public float GetEffectivePressure() {
      if (currentPressure > greenZone[0] && currentPressure < greenZone[1]) {
         return 1;
      } else {
         return currentPressure;
      }
   }

   public float GetPressureUse() {
      var pressureDropIndex = 0;
      for (int i = 0; i < pressureDropRanges.Length; i++) {
         if (currentPressure > pressureDropRanges[i]) {
            pressureDropIndex += 1;
         }
      }

      var pressureDropAmount = pressureDropPerSecond[pressureDropIndex];
      if (isDestroyed) {
         pressureDropAmount *= 5;
      }

      return pressureDropAmount;
   }

   public float GetSelfDamageMultiplier() {
      var damageAmount = 0f;
      if (currentPressure > damageZones[0]) {
         damageAmount = damageAmounts[0];
         if (currentPressure > damageZones[1]) {
            damageAmount = damageAmounts[1];
         }
      }

      return damageAmount;
   }

   public bool isDestroyed = false;

   [HideInInspector]
   public UnityEvent OnEngineStart = new UnityEvent();
   [HideInInspector]
   public UnityEvent OnEngineStop = new UnityEvent();

   [HideInInspector]
   public UnityEvent<bool> OnEngineBoost = new UnityEvent<bool>();
   [HideInInspector]
   public UnityEvent<bool> OnEngineLowPower = new UnityEvent<bool>();


   public float currentPressure = 1.0f;
   public float[] pressureDropPerSecond = {0.01f,0.02f,0.05f,0.1f};
   public float[] pressureDropRanges = { 1.3f, 1.85f, 2.4f };
   public float[] greenZone = { 0.7f, 1.3f };
   public float[] damageZones = { 1.85f, 2.4f };
   public float[] damageAmounts = { 0.5f, 1f };

   public float damage = 100;
   public float damageInterval = 2;
   public float curDamageInterval = 0;
   
   private void OnEnable() {
      SpeedController.s.AddEngine(this);
      OnEngineStart?.Invoke();
   }

   private void OnDisable() {
      if (SpeedController.s != null) {
         SpeedController.s.RemoveEngine(this);
      }
      OnEngineStop?.Invoke();
   }

   private void Update() {
      if (currentPressure > 0 && !SpeedController.s.isWarping) {
         currentPressure -= GetPressureUse() * Time.deltaTime * 0.75f;

         currentPressure = Mathf.Clamp(currentPressure, 0, 3);

         if (currentPressure > damageZones[0]) {
            var damageAmount = damageAmounts[0];
            if (currentPressure > damageZones[1]) {
               damageAmount = damageAmounts[1];
            }

            curDamageInterval += Time.deltaTime;

            if (curDamageInterval > damageInterval) {
               GetComponentInParent<ModuleHealth>().SelfDamage(damage*damageAmount);
               curDamageInterval = 0;
            }


         } else {
            curDamageInterval = 0;
         }
      }
   }

   public void ActivateForCombat() {
      this.enabled = true;
   }

   public void ActivateForShopping() {
      this.enabled = true;
   }

   public void Disable() {
      this.enabled = false;
   }

   public void ResetState() {
      extraSpeedAdd = 0;
      extraEnginePower = 0;
   }
}
