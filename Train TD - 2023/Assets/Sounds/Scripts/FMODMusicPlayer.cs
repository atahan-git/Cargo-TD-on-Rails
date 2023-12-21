using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using Sirenix.Serialization;

public class FMODMusicPlayer : MonoBehaviour
{
    public static FMODMusicPlayer s;

    #region General
    [FoldoutGroup("General")]
    [Header("Speaker")]
    public FMODAudioSource speaker;

    [HorizontalGroup("General/timeLine")]
    public bool isPaused;

    [HorizontalGroup("General/timeLine", width:0.75f)]
    [ShowInInspector]
    private float timelinePosition { get { return Application.isPlaying ? speaker.TimelinePosotion() / 1000f : 0; } }
    #endregion

    #region Music Tracks
    [FoldoutGroup("Music Tracks")]
    public EventReference menuMusicTracks;

    [FoldoutGroup("Music Tracks")]
    [AssetList]
    public List<FMODDynamicMusic> dynamicGameMusicList = new List<FMODDynamicMusic>();
    private FMODDynamicMusic currentDynamicGameMusic;

    private EventReference currentTracks;   // the current track that is loaded
    #endregion

    #region Misc
    private float targetVolume = 1;
    #endregion

    #region Dynamics
    [FoldoutGroup("Dynamics")]
    public int numOfEngagingWave;

    [FoldoutGroup("Dynamics")]
    [ShowInInspector]
    public float bassIndex { get { return speaker.GetParamByName("bassIndex"); } }
    [FoldoutGroup("Dynamics")]
    [ShowInInspector]
    public float drumIndex { get { return speaker.GetParamByName("drumIndex"); } }
    [FoldoutGroup("Dynamics")]
    [ShowInInspector]
    public float melodyIndex { get { return speaker.GetParamByName("melodyIndex"); } }
    [FoldoutGroup("Dynamics")]
    [ShowInInspector]
    public float backingIndex { get { return speaker.GetParamByName("backingIndex"); } }

    private void PreparePhaseChange()
    {
        currentDynamicGameMusic.UpdatePhase(speaker);
    }

    private void InitPhase()
    {
        currentDynamicGameMusic.UpdatePhase(speaker);
    }
    private float phaseT, lastT;

    private List<EnemyWave> enemyWaves = new List<EnemyWave>();
    #endregion

    #region Procedural Music

    public Queue<int> drum1Sheet, drum2Sheet, bassSheet, melody1Sheet, melody2Sheet, backingSheet;

    #endregion

    #region Enemy Engine SFX
    private Dictionary<string, int> enemyCount = new Dictionary<string, int>
    {
        {"Army", 0 },
        {"Ballista", 0 },
        {"Biker", 0 },
        {"Teleporter", 0 },
        {"Buggy", 0 },
        {"Disabler", 0 },
        {"Drone", 0 },
        {"Gatling", 0 },
        {"Healer", 0 },
        {"Nuker", 0 },
        {"Slower", 0 },
        {"Supplier", 0 },
        {"Truck", 0 }
    };
    #endregion

    private void Awake()
    {
        if (s != null)
            Debug.LogError("FMODMusicPlayer should be a singleton class, but multiple instances are found!");
        s = this;

        currentDynamicGameMusic = dynamicGameMusicList[Random.Range(0, dynamicGameMusicList.Count + 1)];
    }

    private void Start()
    {
        SwapMusicTracksAndPlay(false);
        enemyWaves = EnemyWavesController.s.waves;
    }

    #region Track Handling
    /// <summary>
    /// Based on "isGame" parameter, load game/menu tracks, and play them automatically
    /// </summary>
    /// <param name="isGame"></param>
    public void SwapMusicTracksAndPlay(bool isGame)
    {
        var changeMade = false;
        if (isGame)
        {
            if (!currentTracks.Equals(currentDynamicGameMusic.track))
            {
                currentTracks = currentDynamicGameMusic.track;
                changeMade = true;
            }
        }
        else
        {
            if (!currentTracks.Equals(menuMusicTracks))
            {
                currentTracks = menuMusicTracks;
                changeMade = true;
            }
        }

        if (changeMade)
        {
            PlayTracks();
        }
    }

    public void PlayMenuMusic()
    {
        SwapMusicTracksAndPlay(false);
    }

    public void PlayCombatMusic()
    {
        InitPhase();

        SwapMusicTracksAndPlay(true);
    }

    /// <summary>
    /// Stop the currently-playing track. Start playing the tracks loaded in the "currentTracks" variable.
    /// </summary>
    private void PlayTracks()
    {
        speaker.LoadClip(currentTracks, true);
    }

    #endregion

    #region Pause/Unpause Handling
    public void PauseMusic()
    {
        if (!isPaused)
        {
            speaker.Pause();
            isPaused = true;
        }
    }

    public void UnpauseMusic()
    {
        if (isPaused)
        {
            speaker.UnPause();
            isPaused = false;
        }
    }

    void PauseUnPauseOnGamePause(bool paused)
    {
        if (!paused)
        {
            UnpauseMusic();
        }
        else
        {
            PauseMusic();
        }
    }
    #endregion

    #region Temporary Volume Reduce
    public void TemporaryVolumeReduce(float time)
    {
        CancelInvoke();
        targetVolume = 0.7f;
        Invoke("ResetVolume", time);
    }

    private void ResetVolume() 
    {
        targetVolume = 1;
    }
    #endregion

    private void Update()
    {
        #region update battle status
        // count how many waves are engaging
        int tmpCount;
        tmpCount = 0;

        int highestCount = 0;
        List<string> highestType = new List<string>();
        foreach (EnemyWave wave in enemyWaves)
        {
            // if a wave is close enough and is not leaving combat, count it as "engaging"
            if(wave.distance < 20 && !wave.isLeaving)
            {
                tmpCount += 1;
            }
        }

        numOfEngagingWave = tmpCount;

        #endregion

        #region PhaseChange
        if (currentTracks.Equals(currentDynamicGameMusic.track))
        {
            phaseT = (timelinePosition + 0.5f) % currentDynamicGameMusic.phaseChangePosition;
            if (phaseT < lastT)
                PreparePhaseChange();
            lastT = phaseT;
        }


        #endregion

        // pause/unpause control
        if (TimeController.s != null && PlayStateMaster.s.isCombatInProgress())
        {
            if (TimeController.s.isPaused && !isPaused)
            {
                PauseUnPauseOnGamePause(TimeController.s.isPaused);
            }
            else if (!TimeController.s.isPaused && isPaused)
            {
                PauseUnPauseOnGamePause(TimeController.s.isPaused);
            }
        }

        speaker.volume = Mathf.Lerp(speaker.volume, isPaused ? 0 : targetVolume, Time.unscaledDeltaTime * 8f);
    }
}

