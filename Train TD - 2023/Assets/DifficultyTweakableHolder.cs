using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class DifficultyTweakableHolder : ScriptableObject {
    public Tweakables myTweakables;


    public string title;
    [Multiline]
    public string description;
}
