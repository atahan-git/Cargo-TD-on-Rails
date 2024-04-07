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

	public RewardWithWeight[] gemRewards;
	public RewardWithWeight[] cartRewards;
	public RewardWithWeight[] componentRewards;
	
	public float noRewardChance = 0.2f;

	private List<string> recentGemRewards = new List<string>();
	private bool lastRewardWasNoReward = true;

	public string GetGemReward(bool noRewardPossible = true) {
		if (noRewardPossible && !lastRewardWasNoReward) {
			if (Random.value < noRewardChance) {
				lastRewardWasNoReward = true;
				return "";
			}
		}

		lastRewardWasNoReward = false;
		
		var rewardUniqueName = gemRewards.WeightedRandomRoll().uniqueName;

		int n = 0;
		while (recentGemRewards.Contains(rewardUniqueName)) {
			rewardUniqueName = gemRewards.WeightedRandomRoll().uniqueName;

			n++;
			if (n > 5) {
				break;
			}
		}
		
		
		recentGemRewards.Add(rewardUniqueName);
		if (recentGemRewards.Count > 1) {
			recentGemRewards.RemoveAt(0);
		}

		return rewardUniqueName;
	}

	private int timesWithoutEliteRolls = 0;
	public bool GetIfElite(string reward) {
		if (reward.Length <= 0) {
			return false;
		}
		var makeElite = Random.value < timesWithoutEliteRolls*0.07f;
		if (makeElite) {
			timesWithoutEliteRolls = 0;
		} else {
			timesWithoutEliteRolls += 1;
		}

		return makeElite;
	}
}
