using System;
using System.Collections;
using System.Collections.Generic;
using ConversationSystem;
using UnityEngine;

public class ConversationsHolder : MonoBehaviour {

    public static ConversationsHolder s;

    private void Awake() {
        s = this;
    }

    [SerializeField]
    private ConversationScriptable[] allConversations;

    /// Name is case sensitive
    public void TriggerConversation(ConversationsIds convoId, float initialDelay = 0) {
        var index = (int)convoId;

        if (index >= 0 && index < allConversations.Length) {
            ConversationDisplayer.s.StartConversation(allConversations[index].myConversation, initialDelay);
            return;
        }
        
        Debug.LogError($"Cant find conversation with name {convoId}"); // should not be possible!
    }

    public ConversationScriptable[] GetAllConversations() {
        return allConversations;
    }
    
#if UNITY_EDITOR
public void SetConversations(ConversationScriptable[] convos){
    allConversations=convos;
}
#endif
}
