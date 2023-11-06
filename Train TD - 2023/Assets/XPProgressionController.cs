using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XPProgressionController : MonoBehaviour {

	public static XPProgressionController s;

	private void Awake() {
		s = this;
	}

	 DataSaver.MetaProgress _metaProgress => DataSaver.s.GetCurrentSave().metaProgress;

	public  bool IsCharacterUnlocked(int id) {
		switch (id) {
			case 0:
				return true;
			case 1:
				if (_metaProgress.castlesTraveled > 5) {
					return true;
				} else {
					return false;
				}
			case 2:
				if (_metaProgress.castlesTraveled > 10) {
					return true;
				} else {
					return false;
				}
				break;
			default:
				return false;
		}
	}
}
