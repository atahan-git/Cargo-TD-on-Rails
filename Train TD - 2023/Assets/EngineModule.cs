using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EngineModule : MonoBehaviour, IActiveDuringCombat, IActiveDuringShopping, IResetState {
   public float speedAdd = 6;
   public float extraSpeedAdd = 1;
   public int enginePower = 100;
   public int extraEnginePower = 0;

   public bool isHalfPower = false;

   public UnityEvent OnEngineStart = new UnityEvent();
   public UnityEvent OnEngineStop = new UnityEvent();

   public UnityEvent<bool> OnEngineBoost = new UnityEvent<bool>();
   public UnityEvent<bool> OnEngineLowPower = new UnityEvent<bool>();
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

   private bool lastSelfDamageAmount = false;
   public void SetSelfDamageState(bool doSelfDamage) {
      if (doSelfDamage != lastSelfDamageAmount) {
         lastSelfDamageAmount = doSelfDamage;
         GetComponent<ModuleHealth>().selfDamage = doSelfDamage;
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
