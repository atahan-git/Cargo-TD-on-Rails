using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UpgradesController : MonoBehaviour {
	public static UpgradesController s;
	private void Awake() {
		s = this;
	}

	public RewardWithWeight[] rewards;
	public float noRewardChance = 0.2f;

	private List<string> recentRewards = new List<string>();

	public string GetRandomReward(bool noRewardPossible = true) {
		if (noRewardPossible) {
			if (Random.value < noRewardChance) {
				return "";
			}
		}
		
		var rewardUniqueName = rewards[RewardWithWeight.WeightedRandomRoll(rewards)].uniqueName;

		int n = 0;
		while (recentRewards.Contains(rewardUniqueName)) {
			rewardUniqueName = rewards[RewardWithWeight.WeightedRandomRoll(rewards)].uniqueName;

			n++;
			if (n > 5) {
				break;
			}
		}
		
		
		recentRewards.Add(rewardUniqueName);
		if (recentRewards.Count > 7) {
			recentRewards.RemoveAt(0);
		}

		return rewardUniqueName;
	}
}
