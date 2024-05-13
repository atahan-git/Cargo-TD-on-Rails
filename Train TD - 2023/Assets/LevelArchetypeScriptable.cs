using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Random = UnityEngine.Random;


[CreateAssetMenu()]
public class LevelArchetypeScriptable : ScriptableObject {
    public GameObject[] formations;
    [Space]
    public GameObject[] bigEnemies;
    public GameObject[] mediumEnemies;
    public GameObject[] smallEnemies;
    public GameObject[] megaEnemies;
    [Space]
    public GameObject[] enemyGuns;
    public GameObject[] uniqueEquipment;
    public GameObject[] eliteEquipment;

    public BossData[] possibleBossDatas;

    public Vector2 segmentLengthsMin = new Vector2(100, 300);
    public Vector2 segmentLengthsRange = new Vector2(50, 200);
    public ConstructedLevel GenerateLevel() {
        var level = new ConstructedLevel();

        var battalions = new List<GameObject>();
        /*battalions.AddRange(MakeRandomCollection(possibleGunCartBattalions));
        battalions.AddRange(MakeRandomCollection(possibleUtilityCartBattalions));
        battalions.AddRange(MakeRandomCollection(possibleGemBattalions));
        battalions.AddRange(MakeRandomCollection(possibleCargoBattalions));

        level.rewardBattalions = battalions.ToArray();
        //level.encounters = MakeRandomCollection(possibleEncounters);
        level.dynamicBattalions = MakeRandomCollection(possibleDynamicBattalions);*/

        level.formations = formations;
        
        level.bigEnemies = bigEnemies;
        level.mediumEnemies = mediumEnemies;
        level.smallEnemies = smallEnemies;
        level.megaEnemies = megaEnemies;
        
        level.enemyGuns = enemyGuns;
        level.uniqueEquipment = uniqueEquipment;
        level.eliteEquipment = eliteEquipment;

        var minSegmentLength = Random.Range(segmentLengthsMin.x, segmentLengthsMin.y);
        var segmentLengthRange = Random.Range(segmentLengthsRange.x, segmentLengthsRange.y);
        level.segmentLengths = new Vector2(minSegmentLength, minSegmentLength + segmentLengthRange);

        level.bossData = possibleBossDatas[Random.Range(0, possibleBossDatas.Length)];

        level.levelName = name;
        return level;
    }

    T[] MakeRandomCollection <T>(T[] input) {
        if (input == null || input.Length <= 0) {
            return new T[0];
        }
        var onethird = Mathf.CeilToInt(input.Length / 3f);
        var collectionSize = Mathf.Min(onethird, 3);
        
        var collection = new T[collectionSize];
        var indexes = new List<int>();
        for (int i = 0; i < input.Length; i++) {
            indexes.Add(i);
        }

        indexes = ExtensionMethods.Shuffle(indexes);

        for (int i = 0; i < collection.Length; i++) {
            collection[i] = input[indexes[i]];
        }

        return collection;
    }
}

[Serializable]
public class BossData {
    public string bossName = "unset";
    public GameObject bossMainPrefab;
    public int bossesToSpawn = 1;
    public int bossNeededKillCount = 1;

    public BossData Copy() {
        return new BossData() {
            bossName = bossName,
            bossMainPrefab = bossMainPrefab,
            bossesToSpawn = bossesToSpawn,
            bossNeededKillCount = bossNeededKillCount,
        };
    }
}



[Serializable]
public class ConstructedLevel {
    public string levelName = "unset";
    
    public GameObject[] formations;
    [Space]
    public GameObject[] bigEnemies;
    public GameObject[] mediumEnemies;
    public GameObject[] smallEnemies;
    public GameObject[] megaEnemies;
    [Space]
    public GameObject[] enemyGuns;
    public GameObject[] uniqueEquipment;
    public GameObject[] eliteEquipment;

    public BossData bossData;
    public Vector2 segmentLengths = new Vector2();

    public float GetRandomSegmentLength() {
        return Random.Range(segmentLengths.x, segmentLengths.y);
    }
    public bool isRealLevel() {
        return levelName != "unset";
    }

    public ConstructedLevel Copy() {
        /*var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
        return SerializationUtility.DeserializeValue<ConstructedLevel>(serialized, DataFormat.Binary);*/
        var copy = new ConstructedLevel();

        copy.levelName = levelName;
        
        copy.formations = formations;
        
        copy.bigEnemies = bigEnemies;
        copy.mediumEnemies = mediumEnemies;
        copy.smallEnemies = smallEnemies;
        copy.megaEnemies = megaEnemies;
        
        copy.enemyGuns = enemyGuns;
        copy.uniqueEquipment = uniqueEquipment;
        copy.eliteEquipment = eliteEquipment;
        
        copy.bossData = bossData.Copy();
        copy.segmentLengths = segmentLengths;

        return copy;
    }
}


