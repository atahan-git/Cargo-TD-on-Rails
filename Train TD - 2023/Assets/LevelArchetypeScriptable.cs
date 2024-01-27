using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Random = UnityEngine.Random;


[CreateAssetMenu()]
public class LevelArchetypeScriptable : ScriptableObject {
    public GameObject[] possibleFirstBattalions;
    public GameObject[] possibleBattalions;
    public GameObject[] possibleEliteBattalions;
    public EncounterTitle[] possibleEncounters;
    public GameObject[] possibleDynamicBattalions;

    public GameObject[] possibleBossBattalions;

    public Vector2 segmentLengthsMin = new Vector2(100, 300);
    public Vector2 segmentLengthsRange = new Vector2(50, 200);
    public ConstructedLevel GenerateLevel() {
        var level = new ConstructedLevel();

        level.firstBattalions = MakeRandomCollection(possibleFirstBattalions);
        level.battalions = MakeRandomCollection(possibleBattalions);
        level.eliteBattalions = MakeRandomCollection(possibleEliteBattalions);
        level.encounters = MakeRandomCollection(possibleEncounters);
        level.dynamicBattalions = MakeRandomCollection(possibleDynamicBattalions);

        var minSegmentLength = Random.Range(segmentLengthsMin.x, segmentLengthsMin.y);
        var segmentLengthRange = Random.Range(segmentLengthsRange.x, segmentLengthsRange.y);
        level.segmentLengths = new Vector2(minSegmentLength, minSegmentLength + segmentLengthRange);

        level.bossBattalion = possibleBossBattalions[Random.Range(0, possibleBossBattalions.Length)];

        level.levelName = name;
        return level;
    }

    T[] MakeRandomCollection <T>(T[] input) {
        var collection = new T[Mathf.CeilToInt(input.Length / 3f)];
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
    
    public GameObject[] firstBattalions;
    public GameObject[] battalions;
    public GameObject[] eliteBattalions;
    public EncounterTitle[] encounters;
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
        var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
        return SerializationUtility.DeserializeValue<ConstructedLevel>(serialized, DataFormat.Binary);
    }
}


