using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "DynamicMusic", menuName = "FMOD/DynamicMusic")]
public class FMODDynamicMusic : ScriptableObject
{
    [FoldoutGroup("Misc")]
    public EventReference track;
    [FoldoutGroup("Misc")]
    public float phaseChangePosition;

    [FoldoutGroup("Dynamic")]
    [ShowInInspector]
    public DynamicTrack drum;

    [FoldoutGroup("Dynamic")]
    [PropertySpace(30)]
    public DynamicTrack bass, melody, backing;

    public void UpdatePhase(FMODAudioSource speaker)
    {
        int numOfEngagingWave = FMODMusicPlayer.s.numOfEngagingWave;
        //Debug.Log("Update FMOD Phase with NumOfEngagingWave=" +numOfEngagingWave.ToString());

        switch (numOfEngagingWave)
        {
            case 0:
                speaker.SetParamByName(nameof(bass) + "Index", RandomIntFromVector2(bass.enemy0Range));
                speaker.SetParamByName(nameof(drum) + "Index", RandomIntFromVector2(drum.enemy0Range));
                speaker.SetParamByName(nameof(melody) + "Index", RandomIntFromVector2(melody.enemy0Range));
                speaker.SetParamByName(nameof(backing) + "Index", RandomIntFromVector2(backing.enemy0Range));
                break;
            case 1:
                speaker.SetParamByName(nameof(bass) + "Index", RandomIntFromVector2(bass.enemy1Range));
                speaker.SetParamByName(nameof(drum) + "Index", RandomIntFromVector2(drum.enemy1Range));
                speaker.SetParamByName(nameof(melody) + "Index", RandomIntFromVector2(melody.enemy1Range));
                speaker.SetParamByName(nameof(backing) + "Index", RandomIntFromVector2(backing.enemy1Range));
                break;
            case 2:
                speaker.SetParamByName(nameof(bass) + "Index", RandomIntFromVector2(bass.enemy2Range));
                speaker.SetParamByName(nameof(drum) + "Index", RandomIntFromVector2(drum.enemy2Range));
                speaker.SetParamByName(nameof(melody) + "Index", RandomIntFromVector2(melody.enemy2Range));
                speaker.SetParamByName(nameof(backing) + "Index", RandomIntFromVector2(backing.enemy2Range));
                break;
        }
    }

    private int RandomIntFromVector2(Vector2Int vec)
    {
        return Random.Range(vec.x, vec.y + 1);
    }
}

[System.Serializable]
public class DynamicTrack
{
    [MinMaxSlider(0, 6, ShowFields = true)]
    public Vector2Int enemy0Range, enemy1Range, enemy2Range;
}
