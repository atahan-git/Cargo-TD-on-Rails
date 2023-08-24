using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Random = UnityEngine.Random;


[CreateAssetMenu()]
public class LevelArchetypeScriptable : ScriptableObject {

    public bool isBossLevel;

    public LevelSegmentScriptable[] firstLevels;
    public LevelSegmentScriptable[] possibleLevels;
    public LevelSegmentScriptable[] possibleEliteLevels;
    public EncounterTitle[] possibleEncounters;
    public EnemyDynamicSpawnData[] possibleDynamicSpawns;

    // encounter chances
    private NumberWithWeights[] encounterChances = new[] { 
        new NumberWithWeights() { number = 0, weight = 0.15f },
        new NumberWithWeights() { number = 1, weight = 0.75f },
        new NumberWithWeights() { number = 2, weight = 0.1f }
    };
    
    // elite chances
    private NumberWithWeights[] eliteChances = new[] { 
        new NumberWithWeights() { number = 0, weight = 0.15f },
        new NumberWithWeights() { number = 1, weight = 0.75f },
        new NumberWithWeights() { number = 2, weight = 0.1f }
    };
    
    private NumberWithWeights[] enemyDirectionsChances = new[] { 
        new NumberWithWeights() { number = 0, weight = 1 }, // all same direction
        new NumberWithWeights() { number = 1, weight = 3 }, // all random
    };

    public int segmentCount = 4;

    public static bool everyPathEncounterCheat = false;
    
    public ConstructedLevel GenerateLevel() {
        var level = new ConstructedLevel();

        level.isBossLevel = isBossLevel;

        level.mySegmentsA = new LevelSegment[segmentCount];
        level.mySegmentsB = new LevelSegment[segmentCount];

        for (int i = 0; i < segmentCount; i++) {
            if (i == 0 && firstLevels.Length > 0) {
                level.mySegmentsA[0] = GenerateFirstSegment();
                level.mySegmentsB[0] = GenerateFirstSegment();
                continue;
            }


            if (!everyPathEncounterCheat || possibleEncounters.Length == 0) {
                level.mySegmentsA[i] = GenerateRegularSegment();
                level.mySegmentsB[i] = GenerateRegularSegment();
            } else {
                level.mySegmentsA[i] = GenerateEncounterSegment();
                level.mySegmentsB[i] = GenerateEncounterSegment();
            }
        }

        level.dynamicSpawnData = possibleDynamicSpawns[Random.Range(0, possibleDynamicSpawns.Length)];

        if (possibleEncounters!= null && possibleEncounters.Length > 0) {
            var encounterCount = NumberWithWeights.WeightedRandomRoll(encounterChances);
            encounterCount = 0;

            for (int i = 0; i < encounterCount; i++) {
                var mySegment = Random.Range(1, segmentCount); // first one is never an encounter
                
                if (Random.value > 0.5f) {
                    level.mySegmentsA[mySegment] = GenerateEncounterSegment();
                } else {
                    level.mySegmentsB[mySegment] = GenerateEncounterSegment();
                }
            }
        }
        
        if (possibleEliteLevels!= null && possibleEliteLevels.Length > 0) {
            var eliteCount = NumberWithWeights.WeightedRandomRoll(eliteChances);

            for (int i = 0; i < eliteCount; i++) {
                var mySegment = Random.Range(1, segmentCount); // first one is never an elite

                if (Random.value > 0.5f) {
                    level.mySegmentsA[mySegment] = GenerateEliteSegment();
                } else {
                    level.mySegmentsB[mySegment] = GenerateEliteSegment();
                }
            }
        }

        try {
            var doDebugBuggy = EasterEggController.s.GetEasterEggDisplay(EasterEggController.EasterEggChances.rare5);
            if (doDebugBuggy) {
                var mySegment = Random.Range(1, segmentCount); // first one is never debug buggy

                if (Random.value > 0.5f) {
                    level.mySegmentsA[mySegment] = _GenerateSegment(LevelReferences.s.debugBuggyLevel.GetData().Copy());
                } else {
                    level.mySegmentsB[mySegment] = _GenerateSegment(LevelReferences.s.debugBuggyLevel.GetData().Copy());
                }
            }
        }catch{}


        level.levelName = name + " level " + Random.Range(100,1000);
        level.levelNiceName = name;
        return level;
    }


    const int firstEnemyInSegmentDistance = 50;
    const int lastEnemyAndSegmentEndDistance = 120;
    const int powerUpEnemyDistanceFromLastEnemy = 50;

    LevelSegment GenerateEncounterSegment() {
        var encounter = "e_" + possibleEncounters[Random.Range(0, possibleEncounters.Length)].gameObject.name;
        return new LevelSegment() { isEncounter = true, levelName = encounter, segmentLength = 200 };
    }
    
    LevelSegment GenerateFirstSegment() {
        var segment =  firstLevels[Random.Range(0, firstLevels.Length)].GetData().Copy();
        return _GenerateSegment(segment);
    }
    LevelSegment GenerateRegularSegment() {
        var segment =  possibleLevels[Random.Range(0, possibleLevels.Length)].GetData().Copy();
        return _GenerateSegment(segment);
    }
    
    LevelSegment GenerateEliteSegment() {
        var segment =  possibleEliteLevels[Random.Range(0, possibleEliteLevels.Length)].GetData().Copy();
        return _GenerateSegment(segment);
    }

    LevelSegment _GenerateSegment(LevelSegment segment) { // 0 -> random 1 -> all left 2 -> all right 
        var segmentType = NumberWithWeights.WeightedRandomRoll(enemyDirectionsChances);
        int type = 0;
            
        switch (segmentType) {
            case 0: {
                int direction = Random.value > 0.5f ? 1 : 2;
                type = direction;
                break;
            }
            case 1: {
                type = 0;
                break;
            }
        }
        
        

        var enemyOffset = firstEnemyInSegmentDistance;

        for (int i = 0; i < segment.enemiesOnPath.Length; i++) {
            segment.enemiesOnPath[i].distanceOnPath += enemyOffset;
        }

        if (type == 0) {
            for (int i = 0; i < segment.enemiesOnPath.Length; i++) {
                segment.enemiesOnPath[i].isLeft = Random.value > 0.5f;
            }
            
        } else {
            var isLeft = type == 1;

            for (int i = 0; i < segment.enemiesOnPath.Length; i++) {
                segment.enemiesOnPath[i].isLeft = isLeft;
            }
        }
        
        var furthestEnemyDistance = 0;
        for (int i = 0; i < segment.enemiesOnPath.Length; i++) {
            furthestEnemyDistance = Mathf.Max(furthestEnemyDistance, segment.enemiesOnPath[i].distanceOnPath);
        }

        

        if (segment.eliteEnemy) {
            segment.artifactRewardUniqueName = UpgradesController.s.GetRandomEliteRewardArtifact();
        }

        segment.segmentLength = furthestEnemyDistance + lastEnemyAndSegmentEndDistance;
        
        //segment.segmentLength += segment.segmentLength % HexGrid.s.gridSize.x;
        //segment.segmentLength -= HexGrid.s.gridSize.x / 2;
        
        return segment;
    }
}


[Serializable]
public class ConstructedLevel {
    [HideInInspector]
    public string levelName = "unset";

    [HideInInspector] public string levelNiceName;
    
    // for the first segment Segment A will always be chosen, but for the remaining bits the player can choose which one to go to.
    public LevelSegment[] mySegmentsA; 
    public LevelSegment[] mySegmentsB;

    public EnemyDynamicSpawnData dynamicSpawnData;

    public bool isBossLevel;

    public bool isRealLevel() {
        return levelName != "unset";
    }

    public ConstructedLevel Copy() {
        var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
        return SerializationUtility.DeserializeValue<ConstructedLevel>(serialized, DataFormat.Binary);
    }
}


