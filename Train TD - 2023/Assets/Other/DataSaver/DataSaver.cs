using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FullSerializer;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

[Serializable]
public class DataSaver {

	public static DataSaver s;

	private int activeSave = 0;
	public SaveFile[] allSaves = new SaveFile[3];
	public const string saveName1 = "save1.data";
	public const string saveName2 = "save2.data";
	public const string saveName3 = "save3.data";

	public bool loadingComplete = false;

	private static readonly fsSerializer _serializer = new fsSerializer();

	public int ActiveSave {
		get => activeSave;
		private set => activeSave = value;
	}

	public string GetSaveFilePathAndFileName(int index) {
		var saveName = saveName1;
		if (index == 1) {
			saveName = saveName2;
		}

		if (index == 2) {
			saveName = saveName3;
		}

		return Application.persistentDataPath + "/" + saveName;
	}
	
	public SaveFile GetCurrentSave() {
		return allSaves[ActiveSave];
	}

	private float saveStartTime = 0f;

	public void SetActiveSave(int id) {
		allSaves[activeSave].playtime += Time.realtimeSinceStartup - saveStartTime;
		allSaves[activeSave].isActiveSave = false;
		if (allSaves[activeSave].isInARun) {
			allSaves[activeSave].currentRun.playtime += Time.realtimeSinceStartup - saveStartTime;
		}
		
		SaveActiveGame();

		saveStartTime = Time.realtimeSinceStartup;
		ActiveSave = id;
		allSaves[activeSave].isActiveSave = true;
		SaveActiveGame();
	}

	public float GetTimeSpentSinceLastSaving() {
		return Time.realtimeSinceStartup - saveStartTime;
	}

	public void ClearCurrentSave() {
		Debug.Log("Clearing Save");
		allSaves[ActiveSave] = MakeNewSaveFile(ActiveSave);
		saveStartTime = Time.realtimeSinceStartup;
	}

	public SaveFile MakeNewSaveFile(int id) {
		var file = new SaveFile();
		file.isRealSaveFile = true;
		file.saveName = $"Slot {id + 1}";
		return file;
	}


	public bool dontSave = false;

	public float saveLockTimer = 0;

	public bool saveInNextFrame = false;
	[Button]
	public void SaveActiveGame() {
		if (!dontSave) {
			saveInNextFrame = true;
		}
		//Debug.Log("Initiating Save");
	}

	void DoSaveActiveGame() {
		GetCurrentSave().playtime += Time.realtimeSinceStartup - saveStartTime;
		allSaves[activeSave].isActiveSave = false;
		if (allSaves[activeSave].isInARun) {
			allSaves[activeSave].currentRun.playtime += Time.realtimeSinceStartup - saveStartTime;
		}
		GetCurrentSave().runGameVersion = VersionDisplay.s.GetVersionNumber();
		saveStartTime = Time.realtimeSinceStartup;
		Save(ActiveSave);
	}

	public void Update() {
		if (saveInNextFrame && saveLockTimer <= 0) {
			DoSaveActiveGame();
			saveInNextFrame = false;
			saveLockTimer = 2;
		}

		if (saveLockTimer > 0) {
			saveLockTimer -= Time.deltaTime;
		}
	}

	public void CheckAndDoSave() {
		if (saveInNextFrame) {
			saveInNextFrame = false;
			DoSaveActiveGame();
		}
	}

	void Save(int saveId) {
		var path = GetSaveFilePathAndFileName(saveId);

		allSaves[saveId].isRealSaveFile = true;
		SaveFile data = allSaves[saveId];

		WriteFile(path, data);
	}

	public static void WriteFile(string path, object file) {
		Directory.CreateDirectory(Path.GetDirectoryName(path));

		StreamWriter writer = new StreamWriter(path);

		fsData serialized;
		_serializer.TrySerialize(file, out serialized);
		var json = fsJsonPrinter.PrettyJson(serialized);

		writer.Write(json);
		writer.Close();

		Debug.Log($"IO OP: file \"{file.GetType()}\" saved to \"{path}\"");
	}

	[Button]
	public void Load() {
		if (loadingComplete) {
			return;
		}

		for (int i = 0; i < 3; i++) {
			var path = GetSaveFilePathAndFileName(i);
			try {
				if (File.Exists(path)) {
					allSaves[i] = ReadFile<SaveFile>(path);
				} else {
					Debug.Log($"No Data Found on slot: {i}");
					allSaves[i] = MakeNewSaveFile(i);
				}
			} catch {
				File.Delete(path);
				Debug.Log("Corrupt Data Deleted");
				allSaves[i] = MakeNewSaveFile(i);
			}
		}

		ActiveSave = -1;
		for (int i = 0; i < 3; i++) {
			if (allSaves[i].isActiveSave) {
				ActiveSave = i;
			}
		}

		if (ActiveSave == -1) {
			ActiveSave = 0;
			allSaves[ActiveSave].isActiveSave = true;
		}


		saveStartTime = Time.realtimeSinceStartup;
		loadingComplete = true;
	}

	public static T ReadFile<T>(string path) where T : class, new() {
		StreamReader reader = new StreamReader(path);
		var json = reader.ReadToEnd();
		reader.Close();

		fsData serialized = fsJsonParser.Parse(json);

		T file = new T();
		_serializer.TryDeserialize(serialized, ref file).AssertSuccessWithoutWarnings();

		Debug.Log($"IO OP: file \"{file.GetType()}\" read from \"{path}\"");
		return file;
	}


	[Serializable]
	public class SaveFile {
		public string saveName = "unnamed";
		public bool isActiveSave = false;
		public bool isRealSaveFile = false;
		
		public float playtime;
		
		public string runGameVersion;
		
		public bool isInARun = false;
		public RunState currentRun = new RunState("0.0.0.a"); // assumed to be never null

		public bool showWakeUp = false;

		public bool instantRestart = false;

		public int castlesTraveled = 0;
		public int runsMade = 0;
		public int money = 0;

		public int lastDifficultySelected = 0;
		
		public TutorialProgress tutorialProgress = new TutorialProgress();
	}
	
	[Serializable]
	public class  RunState {
		public int difficulty = 0;
		
		public CharacterData character = new CharacterData();

		public TrainState myTrain = new TrainState();

		public int currentAct = 1; // 1,2,3

		public float playtime;

		public bool shopInitialized = false;
		public ShopStateController.ShopState shopState;

		public string runGameVersion;

		public RunState(string version) {
			runGameVersion = version;
		}
		
		
		public void SetCharacter(CharacterData characterData) {
			character = characterData;
			myTrain = characterData.starterTrain.Copy();

			shopInitialized = false;

			for (int i = 0; i < myTrain.myCarts.Count; i++) {
				var build = myTrain.myCarts[i];
				//build.ammo = -2;
			}
		}
	}
	
	

	[Serializable]
	public class TutorialProgress {
		public bool prologueDone = false;
		public int runsMadeAfterTutorial = 0;
		public bool showTutorials = true;
	}

	[Serializable]
	public class TrainState {
		public List<CartState> myCarts = new List<CartState>();

		public List<CartState> myHoldingCarts = new List<CartState>();
		public List<ArtifactState> myHoldingArtifacts = new List<ArtifactState>();

		[Serializable]
		public class CartState {
			[ValueDropdown("GetAllModuleNames")]
			public string uniqueName = "";

			[HideInInspector]
			public float health = -1;
			[HideInInspector]
			public float maxHealthReduction = -1;

			public List<ArtifactState> attachedArtifacts = new List<ArtifactState>();
			
			public void EmptyState() {
				uniqueName = "";
				health = -1;
				maxHealthReduction = -1;
				attachedArtifacts = new List<ArtifactState>();
			}
			
			private static IEnumerable GetAllModuleNames() {
				return GameObject.FindObjectOfType<DataHolder>().GetAllPossibleBuildingNames();
			}
			
			private static IEnumerable GetAllArtifactNames() {
				var artifacts = GameObject.FindObjectOfType<DataHolder>().artifacts;
				var artifactNames = new List<string>();
				artifactNames.Add("");
				for (int i = 0; i < artifacts.Length; i++) {
					artifactNames.Add(artifacts[i].uniqueName);
				}
				return artifactNames;
			}
			
			public CartState Copy() {
				var copyState = new CartState();
				copyState.uniqueName = uniqueName;
				copyState.health = health;
				copyState.maxHealthReduction = maxHealthReduction;

				for (int i = 0; i < attachedArtifacts.Count; i++) {
					copyState.attachedArtifacts.Add(attachedArtifacts[i].Copy());
				}
				
				return copyState;
			}
		}

		[Serializable]
		public class ArtifactState {
			[ValueDropdown("GetAllArtifactNames")]
			public string uniqueName = "";
			private static IEnumerable GetAllArtifactNames() {
				var artifacts = GameObject.FindObjectOfType<DataHolder>().artifacts;
				var artifactNames = new List<string>();
				artifactNames.Add("");
				for (int i = 0; i < artifacts.Length; i++) {
					artifactNames.Add(artifacts[i].uniqueName);
				}
				return artifactNames;
			}
			
			public void EmptyState() {
				uniqueName = "";
			}
			
			public ArtifactState Copy() {
				var copyState = new ArtifactState();
				copyState.uniqueName = uniqueName;
				return copyState;
			}
		}
		
		public TrainState Copy() {
			var copyState = new TrainState();

			for (int i = 0; i < myCarts.Count; i++) {
				copyState.myCarts.Add(myCarts[i].Copy());
			}

			return copyState;
		}
	}
}

[Serializable]
public enum ResourceTypes {
	scraps
}
