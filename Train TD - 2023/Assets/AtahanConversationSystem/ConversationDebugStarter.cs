using System;
using System.Collections;
using System.Collections.Generic;
using ConversationSystem;
using UnityEngine;

public class ConversationDebugStarter : MonoBehaviour {
    public ConversationScriptable asset;

    private void Start() {
        if (Application.isEditor) {
            DebugStartConversation();
        }
    }

    public void DebugStartConversation() {
        ConversationDisplayer.s.StartConversation(asset.myConversation);
    }
}
