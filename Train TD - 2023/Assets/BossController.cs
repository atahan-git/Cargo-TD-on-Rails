using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class BossController : MonoBehaviour {

    public static BossController s;

    private void Awake() {
        s = this;
    }

    public GameObject bossFightUI;

    public TMP_Text bossKilledText;
    public GameObject continueButton;

    public bool isBoss;
    
    private bool bossStarted = false;
    private float bossUIFadeInTimer;

    public BossData myBoss => PlayStateMaster.s.currentLevel.bossData;

    private void Start() {
        bossFightUI.SetActive(false);
    }

    public void SetBossComing(bool _isBoss) {
        isBoss = _isBoss;
    }

    private EnemyTrain boss;
    public void NewPathEnteredWithBoss(float segmentStartDistance, float segmentLength) {
        if (!isBoss) {
            return;
        }
        
        if (!bossStarted) {
            MiniGUI_BossNameUI.s.ShowBossName("Le boss");
            MiniGUI_BossNameUI.s.ShowBossName("Le boss");
            bossStarted = true;
        }

        var distance = Random.Range(segmentLength / 10f, segmentLength / 3f);
        
        boss = Instantiate(myBoss.bossMainPrefab).GetComponent<EnemyTrain>();
        boss.SetUpEnemyTrain();
        //EnemyWavesController.s.SpawnEnemy(myBoss.bossMainPrefab, segmentStartDistance + distance, false, Random.value < 0.5f);
        
        continueButton.SetActive(false);
        continueButton.GetComponent<CanvasGroup>().alpha = 0;
        
        StoryAndTutorialsController.s.OnBossStart();
    }


    // Update is called once per frame
    void Update()
    {
        if (bossStarted) {
            if (bossUIFadeInTimer < 10) {
                bossUIFadeInTimer += Time.deltaTime;

                if (bossUIFadeInTimer > 2) {
                    var fade = bossUIFadeInTimer - 2;
                    fade = Mathf.Clamp01(fade);
                    bossFightUI.SetActive(true);
                    bossFightUI.GetComponent<CanvasGroup>().alpha = fade;
                }
            }

            UpdateBossKilledTextAndContinueButton();
        }
    }

    public bool IsBossAlive() {
        if (isBoss) {
            if (boss != null) {
                return boss.IsAlive();
            }
        } 
        
        return false;
    }
    void UpdateBossKilledTextAndContinueButton() {
        if (EnemyWavesController.s.AnyEnemyIsPresent()) {
            bossKilledText.text = $"Defeat the boss";
        } else {
            bossKilledText.text = $"Congrats!";
            continueButton.SetActive(true);
            continueButton.GetComponent<CanvasGroup>().alpha = Mathf.MoveTowards(continueButton.GetComponent<CanvasGroup>().alpha, 1, 1 * Time.deltaTime);
        }
    }
    
}
