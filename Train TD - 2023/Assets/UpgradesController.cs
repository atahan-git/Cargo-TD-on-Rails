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
	public RewardWithWeight[] bigGemRewards;
	public RewardWithWeight[] cartRewards;
	public RewardWithWeight[] componentRewards;
	
	public float noRewardChance = 0.2f;


	[ValueDropdown("GetAllRewardNames")] 
	public string potatoGemName;
	
	public List<string> recentGemRewards = new List<string>();
	public List<string> recentBigGemRewards = new List<string>();
	public List<string> recentCartRewards = new List<string>();
	public List<string> allComponentRewards = new List<string>();

	[Serializable]
	public class PathEnemyType {
		public PathType myType = PathType.regular;
		public enum PathType {
			regular=0, boss=1, pitStop=2, empty=3, easy=4, elite=5
		}
	}

	public PathEnemyType GetBossEnemy() {
		return new PathEnemyType() {
			myType = PathEnemyType.PathType.boss
		};
	}

	public PathEnemyType GetPathEnemy() {
		return new PathEnemyType() {
			myType = PathEnemyType.PathType.regular
		};
	}
	
	public PathEnemyType GetEasyEnemy() {
		return new PathEnemyType() {
			myType = PathEnemyType.PathType.easy
		};
	}
	
	public PathEnemyType GetEliteEnemy() {
		return new PathEnemyType() {
			myType = PathEnemyType.PathType.elite
		};
	}

	public PathEnemyType GetEmptyEnemy() {
		return new PathEnemyType() {
			myType = PathEnemyType.PathType.empty
		};
	}
	
	public PathEnemyType GetPitStopEnemy() {
		return new PathEnemyType() {
			myType = PathEnemyType.PathType.pitStop
		};
	}

	public string GetGemReward(bool isBigGem) {
		if (isBigGem) {
			return GetRewards(bigGemRewards, recentBigGemRewards, 2);
		} else {
			return GetRewards(gemRewards, recentGemRewards, 2);
		}
	}

	public string GetCartReward() {
		return GetRewards(cartRewards, recentCartRewards, 2);
	}
	
	public string GetComponentReward() {
		return GetRewards(componentRewards, allComponentRewards, 100);
	}

	string GetRewards(RewardWithWeight[] rewards, List<string> dupeList, int dupeAllowance) {
		var rewardUniqueName = rewards.WeightedRandomRoll().uniqueName;

		int n = 0;
		while (dupeList.Contains(rewardUniqueName)) {
			rewardUniqueName = rewards.WeightedRandomRoll().uniqueName;

			n++;
			if (n > 10) {
				break;
			}
		}
		
		
		dupeList.Add(rewardUniqueName);
		while (dupeList.Count > dupeAllowance) {
			dupeList.RemoveAt(0);
		}

		return rewardUniqueName;
	}

	private int timesWithoutEliteRolls = 0;
	bool GetIfElite(string reward, int distance) {
		if (reward.Length <= 0) {
			return false;
		}

		if (distance < 2) {
			return true;
		}
		
		var makeElite = Random.value < timesWithoutEliteRolls*0.07f;
		if (makeElite) {
			timesWithoutEliteRolls = 0;
		} else {
			timesWithoutEliteRolls += 1;
		}

		return makeElite;
	}
	
	private static IEnumerable GetAllRewardNames() {
		var buildingNames = GameObject.FindObjectOfType<DataHolder>().GetAllPossibleBuildingNames();
		var artifactNames = GameObject.FindObjectOfType<DataHolder>().GetAllPossibleArtifactNames();
		
		buildingNames.Remove("");
		buildingNames.AddRange(artifactNames);
		
		return buildingNames;
	}
}
