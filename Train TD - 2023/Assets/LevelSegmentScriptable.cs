using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;



[CreateAssetMenu()]
public class LevelSegmentScriptable : ScriptableObject
{
	[SerializeField]
	private LevelSegment myData;

	public LevelSegment GetData() {
		myData.levelName = name;
		return myData;
	}
}


[Serializable]
public class LevelSegment {
	[HideInInspector]
	public string levelName = "unset";

	[HideInInspector]
	public bool isEncounter = false;

	public EnemyOnPathData[] enemiesOnPath;
	
	public bool eliteEnemy {
		get {
			if (enemiesOnPath == null)
				return false;
			
			for (int i = 0; i < enemiesOnPath.Length; i++) {
				if (enemiesOnPath[i].hasReward) {
					return true;
				}
			}
			return false;
		}
	}

	//[Tooltip("This will be expanded by about 100 to make space at the start and the end")]
	[HideInInspector]
	public int segmentLength = 200;
	
	[HideInInspector]
	public string artifactRewardUniqueName;
	public int artifactRewardLevel;
	
	public bool isRealLevel() {
		return levelName != "unset";
	}

	public LevelSegment Copy() {
		var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
		return SerializationUtility.DeserializeValue<LevelSegment>(serialized, DataFormat.Binary);
	}
}


