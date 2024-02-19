using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_ModifyEnemies : ActivateWhenEnemySpawns {
	public float eliteEnemyHealthMultiplier = 1f;
	public float fireDecayRateMultiplier = 1f;

	public override void ModifyEnemy(EnemyHealth enemyHealth) {
		/*if (enemyHealth.rewardArtifactOnDeath) {
			enemyHealth.currentHealth *= eliteEnemyHealthMultiplier;
		}*/

		enemyHealth.burnReduction *= fireDecayRateMultiplier;
	}
}