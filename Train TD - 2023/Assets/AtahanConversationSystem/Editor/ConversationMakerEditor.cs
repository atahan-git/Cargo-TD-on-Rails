using UnityEngine;
using System.Collections;
using UnityEditor;
using ConversationSystem;

[CustomEditor(typeof(ConversationMaker))]
public class ConversationMakerEditor : Editor {
	
	private void OnEnable()
	{
		EditorApplication.update += OnEditorUpdate;
	}

	private void OnDisable()
	{
		EditorApplication.update -= OnEditorUpdate;
	}

	private void OnEditorUpdate()
	{
		ConversationMaker myTarget = (ConversationMaker)target;
		var remarks = myTarget.GetComponentsInChildren<RemarkMono> ();

		myTarget.gameObject.name = "-"+ myTarget.conversationName + "- Conversation";

		for (int i = 0; i < remarks.Length; i++) {
			var myRemark = remarks[i];
			RemarkMonoEditor.ValidateRemark(myRemark);
		}

		// Optionally mark the inspector as dirty to refresh the UI if needed
		//Repaint();
	}
	

	public override void OnInspectorGUI () {
		DrawDefaultInspector ();
		ConversationMaker myTarget = (ConversationMaker)target;


		if (GUILayout.Button ("Add Remark")) {
			AddRemark(myTarget);
		}

		if (GUILayout.Button ("Save Conversation into Asset")) {
			SaveConversationAsset(myTarget);
		}

		if (GUILayout.Button ("Load Conversation from Asset")) {
			LoadConversationAsset(myTarget);
		}


		if (GUILayout.Button ("Clear Conversation Maker")) {
			ClearConversationMaker(myTarget);
		}
	}

	private static void AddRemark(ConversationMaker myTarget) {
		var transform = myTarget.transform;
		GameObject myRemark = new GameObject("new remark");
		myRemark.transform.position = transform.position;
		myRemark.transform.rotation = transform.rotation;
		myRemark.transform.SetParent(transform);
		myRemark.AddComponent<RemarkMono>();
	}

	private static void SaveConversationAsset(ConversationMaker myTarget) {
		ConversationScriptable asset = myTarget.myAsset;
		bool shouldCreateNew = false;
		if (asset == null) {
			asset = ScriptableObject.CreateInstance<ConversationScriptable>();
			shouldCreateNew = true;
		}

		var remarks = myTarget.GetComponentsInChildren<RemarkMono>();

		var conversationName = "-" + myTarget.conversationName + "- Conversation";
		
		asset.myConversation = new Conversation();
		asset.myConversation.remarks = new Remark[remarks.Length];
		asset.name = myTarget.conversationName;

		for (int i = 0; i < remarks.Length; i++) {
			asset.myConversation.remarks[i] = remarks[i].myRemark;
		}

		var assetPath = "Assets/AtahanConversationSystem/Conversations/" + conversationName + ".asset";

		if (shouldCreateNew) {
			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.SaveAssets();
		}

		EditorUtility.FocusProjectWindow();

		Selection.activeObject = asset;

		myTarget.myAsset = asset;
		Debug.Log($"Conversation saved at \"{assetPath}\" with {remarks.Length} remarks");
	}

	private static void LoadConversationAsset(ConversationMaker myTarget) {
		if (myTarget.myAsset != null) {
			var transform = myTarget.transform;
			DeleteAllChildren(transform);

			foreach (Remark remark in myTarget.myAsset.myConversation.remarks) {
				GameObject myRemark = new GameObject("new remark");
				myRemark.transform.position = transform.position;
				myRemark.transform.rotation = transform.rotation;
				myRemark.transform.SetParent(transform);
				myRemark.AddComponent<RemarkMono>();
				myRemark.GetComponent<RemarkMono>().myRemark = remark;
			}
			
			Debug.Log($"Loaded {myTarget.myAsset.myConversation.remarks.Length} remarks from {myTarget.myAsset.name}");
			myTarget.conversationName = myTarget.myAsset.name;
		} else {
			Debug.Log("Trying to load a null asset. Please fill in the asset field");
		}
	}

	private void ClearConversationMaker(ConversationMaker myTarget) {
		myTarget.myAsset = null;
		DeleteAllChildren(myTarget.transform);
		myTarget.conversationName = "New Conversation";
	}

	private static void DeleteAllChildren(Transform target)
	{
		// Iterate over all child objects of this GameObject
		for (int i = target.childCount - 1; i >= 0; i--)
		{
			// Get the child GameObject
			GameObject child = target.GetChild(i).gameObject;

			// Destroy the child object
			DestroyImmediate(child);
		}
	}
}
