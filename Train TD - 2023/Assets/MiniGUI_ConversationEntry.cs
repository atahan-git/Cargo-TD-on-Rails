using System.Collections;
using System.Collections.Generic;
using ConversationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_ConversationEntry : MonoBehaviour {
    public TMP_Text nameText;

    public Conversation myConvo;

    public void SetUp(Conversation conversation) {
        myConvo = conversation;
        var seenDialogBefore = false;
        if (DataSaver.s.GetCurrentSave().storyProgress.seenConversations.Contains(conversation.conversationUniqueID)) {
            seenDialogBefore = true;
        }

        if (seenDialogBefore) {
            nameText.text = conversation.GetDisplayName();
            GetComponentInChildren<Button>().interactable = true;
        } else {
            nameText.text = "???";
            GetComponentInChildren<Button>().interactable = false;
        }
    }

    public void ViewConversation() {
        ConversationDisplayer.s.StartConversation(myConvo);
    }
}
