using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryAndTutorialsController : MonoBehaviour {
	public static StoryAndTutorialsController s;

	private void Awake() {
		s = this;
	}

	
	public void OnShopEntered() {
		var currentSave = DataSaver.s.GetCurrentSave();
		var storyProgress = currentSave.storyProgress;
		
		if (storyProgress.firstCityWakeyTextShownCount == 0) {
			ConversationsHolder.s.TriggerConversation(ConversationsIds.Main_Story_1_wakey_wakey,0.5f);
		}else if (currentSave.currentRun.currentAct == 2 && storyProgress.shownAct2weirdText) {
			storyProgress.shownAct2weirdText = true;
			ConversationsHolder.s.TriggerConversation(ConversationsIds.Main_Story_6_act_2_city_is_odd,0.5f);
		}
	}

	[Header("Enemy tracking Info")]
	public bool trackingEnemy;
	public float enemyTrackDistance = 3;
	public EnemyHealth enemyBeingTracked;
	public ConversationsIds conversationToShowWhenEnemyIsNear;
	public bool showEnemyDetailsWhenReached = false;
	
	public void OnSectionWithEnemyStarted() {
		var currentSave = DataSaver.s.GetCurrentSave();
		var tutorialProgress = currentSave.tutorialProgress;
		var storyProgress = DataSaver.s.GetCurrentSave().storyProgress;

		if (!storyProgress.shownFirstBanditText && EnemyWavesController.s.AnyEnemyIsPresent() && EnemyWavesController.s.enemyInSwarms.Count > 0) {
			trackingEnemy = true;
			enemyBeingTracked = EnemyWavesController.s.enemyInSwarms[0].GetComponent<EnemyHealth>();
			conversationToShowWhenEnemyIsNear = ConversationsIds.Main_Story_4_bandits;
			enemyTrackDistance = 5;
		}
		
		if (!storyProgress.shownLootFirstText && EnemyWavesController.s.AnyEnemyIsPresent()) {
			storyProgress.shownLootFirstText = true;
			ConversationsHolder.s.TriggerConversation(ConversationsIds.Main_Story_3_why_close_loot,0.5f);
			return; // override other start convos
		}

		switch (BiomeEffectsController.s.GetCurrentEffects().currentBiome) {
			case BiomeEffectsController.BiomeIdentifier.grasslands:
				// no story
				break;
			case BiomeEffectsController.BiomeIdentifier.drylands:
				if (!tutorialProgress.shownFirstDrylandsText) {
					tutorialProgress.shownFirstDrylandsText = true;
					ConversationsHolder.s.TriggerConversation(ConversationsIds.Tutorial_drylands, 0.5f);
				}
				break;
			case BiomeEffectsController.BiomeIdentifier.snowlands:
				if (!tutorialProgress.shownFirstDrylandsText) {
					tutorialProgress.shownFirstDrylandsText = true;
					ConversationsHolder.s.TriggerConversation(ConversationsIds.Tutorial_snowlands, 0.5f);
				}
				break;
			case BiomeEffectsController.BiomeIdentifier.purplelands:
				break;
			case BiomeEffectsController.BiomeIdentifier.darklands:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public void OnEliteEnemySpawned(Transform enemy) {
		var currentSave = DataSaver.s.GetCurrentSave();
		var tutorialProgress = currentSave.tutorialProgress;
		var storyProgress = DataSaver.s.GetCurrentSave().storyProgress;
		if (!tutorialProgress.shownEliteEnemyText) {
			trackingEnemy = true;
			enemyBeingTracked = enemy.GetComponent<EnemyHealth>();
			conversationToShowWhenEnemyIsNear = ConversationsIds.Tutorial__elite_enemy;
			showEnemyDetailsWhenReached = true;
		}
	}

	public void OnUniqueGearEnemy(Transform enemy) {
		var currentSave = DataSaver.s.GetCurrentSave();
		var tutorialProgress = currentSave.tutorialProgress;
		var storyProgress = DataSaver.s.GetCurrentSave().storyProgress;
		if (!tutorialProgress.shownEnemyWithUniqueGearText) {
			trackingEnemy = true;
			enemyBeingTracked = enemy.GetComponent<EnemyHealth>();
			conversationToShowWhenEnemyIsNear = ConversationsIds.Tutorial__unique_gear;
			showEnemyDetailsWhenReached = true;
		}
	}

	public void OnOpenMap() {
		var currentSave = DataSaver.s.GetCurrentSave();
		var storyProgress = DataSaver.s.GetCurrentSave().storyProgress;
		if (!storyProgress.shownMapFirstText) {
			storyProgress.shownMapFirstText = true;
			ConversationsHolder.s.TriggerConversation(ConversationsIds.Main_Story_2_lets_pick_a_path,0.5f);
		}
	}


	public void OnBossStart() {
		var currentSave = DataSaver.s.GetCurrentSave();
		var storyProgress = currentSave.storyProgress;
		if (!storyProgress.shownBossFirstText) {
			storyProgress.shownBossFirstText = true;
			ConversationsHolder.s.TriggerConversation(ConversationsIds.Main_Story_5_city_in_the_distance_boss_start,5f);
		}
	}

	public void OnTryingPullOutArrow() {
		var currentSave = DataSaver.s.GetCurrentSave();
		var tutorialProgress = currentSave.tutorialProgress;
		var storyProgress = currentSave.storyProgress;
		if (!tutorialProgress.shownArrowPullOutText) {
			tutorialProgress.shownArrowPullOutText = true;
			ConversationsHolder.s.TriggerConversation(ConversationsIds.Tutorial_arrow_damage);
		}
	}

	private void Update() {
		if (trackingEnemy) {
			if (enemyBeingTracked == null) {
				trackingEnemy = false;
				return;
			}
			
			var trainPos = Train.s.trainMiddle.position;
			if (Vector3.Distance(trainPos, enemyBeingTracked.transform.position) < enemyTrackDistance) {
				ConversationsHolder.s.TriggerConversation(conversationToShowWhenEnemyIsNear);
				trackingEnemy = false;

				if (showEnemyDetailsWhenReached) {
					//DirectControlMaster.s.DisableDirectControl();
					//CameraController.s.SnapToTransform(enemyBeingTracked.transform);
					PlayerWorldInteractionController.s.ShownThingInfo(enemyBeingTracked);
				}

				var currentSave = DataSaver.s.GetCurrentSave();
				var tutorialProgress = currentSave.tutorialProgress;
				var storyProgress = currentSave.storyProgress;
				switch (conversationToShowWhenEnemyIsNear) {
					case ConversationsIds.Main_Story_4_bandits:
						storyProgress.shownFirstBanditText = true;
						break;
					case ConversationsIds.Tutorial__elite_enemy:
						tutorialProgress.shownEliteEnemyText = true;
						break;
					case ConversationsIds.Tutorial__unique_gear:
						tutorialProgress.shownEnemyWithUniqueGearText = true;
						break;
				}
			}
		}
	}
}
