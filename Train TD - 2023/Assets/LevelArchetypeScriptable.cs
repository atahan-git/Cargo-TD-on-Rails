using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Random = UnityEngine.Random;


[CreateAssetMenu()]
public class LevelArchetypeScriptable : ScriptableObject {
    public GameObject[] possibleGunCartBattalions;
    public GameObject[] possibleUtilityCartBattalions;
    public GameObject[] possibleGemBattalions;
    public GameObject[] possibleCargoBattalions;
    //public EncounterTitle[] possibleEncounters;
    public GameObject[] possibleDynamicBattalions;

    public GameObject[] possibleBossBattalions;

    public Vector2 segmentLengthsMin = new Vector2(100, 300);
    public Vector2 segmentLengthsRange = new Vector2(50, 200);
    public ConstructedLevel GenerateLevel() {
        var level = new ConstructedLevel();

        level.gunCartBattalions = MakeRandomCollection(possibleGunCartBattalions);
        level.utilityCartBattalions = MakeRandomCollection(possibleUtilityCartBattalions);
        level.gemBattalions = MakeRandomCollection(possibleGemBattalions);
        level.cargoBattalions = MakeRandomCollection(possibleCargoBattalions);
        //level.encounters = MakeRandomCollection(possibleEncounters);
        level.dynamicBattalions = MakeRandomCollection(possibleDynamicBattalions);

        var minSegmentLength = Random.Range(segmentLengthsMin.x, segmentLengthsMin.y);
        var segmentLengthRange = Random.Range(segmentLengthsRange.x, segmentLengthsRange.y);
        level.segmentLengths = new Vector2(minSegmentLength, minSegmentLength + segmentLengthRange);

        level.bossBattalion = possibleBossBattalions[Random.Range(0, possibleBossBattalions.Length)];

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
        indexes.Shuffle();

        for (int i = 0; i < collection.Length; i++) {
            collection[i] = input[indexes[i]];
        }

        return collection;
    }
}



[Serializable]
public class ConstructedLevel {
    public string levelName = "unset";
    
    
    public GameObject[] gunCartBattalions;
    public GameObject[] utilityCartBattalions;
    public GameObject[] gemBattalions;
    public GameObject[] cargoBattalions;
    //public EncounterTitle[] encounters;
    public GameObject[] dynamicBattalions;

    public GameObject bossBattalion;

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
        copy.gunCartBattalions = gunCartBattalions;
        copy.utilityCartBattalions = utilityCartBattalions;
        copy.gemBattalions = gemBattalions;
        copy.cargoBattalions = cargoBattalions;
        //copy.encounters = encounters;
        copy.dynamicBattalions = dynamicBattalions;
        copy.bossBattalion = bossBattalion;
        copy.segmentLengths = segmentLengths;


        return copy;
    }
}


