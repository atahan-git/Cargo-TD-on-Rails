using System;
using System.Collections;
using System.Collections.Generic;
using ConversationSystem;
using TMPro;
using UnityEngine;

public class MiniGUI_DisplayConversationsWithTag : MonoBehaviour {

    public ConversationType myType;

    public Transform parent;
    public GameObject perConvoPrefab;


    private void OnEnable() {
	    var correctConvos = new List<Conversation>();
	    var allConvos = ConversationsHolder.s.GetAllConversations();

	    for (int i = 0; i < allConvos.Length; i++) {
		    if (allConvos[i].myConversation.myType == myType) {
			    correctConvos.Add(allConvos[i].myConversation);
		    }
	    }
	    
	    parent.DeleteAllChildren();

	    for (int i = 0; i < correctConvos.Count; i++) {
		    Instantiate(perConvoPrefab, parent).GetComponent<MiniGUI_ConversationEntry>().SetUp(correctConvos[i]);
	    }
    }
}
