using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using ConversationSystem;

public class ConversationCacherEditor 
{

    [MenuItem("Tools/Cache Conversations")]
    public static void CacheConversations() {
        LoadConvosAndMakeEnum();
    }
    private static void LoadConvosAndMakeEnum()
    {
        string[] guids = AssetDatabase.FindAssets("t:ConversationScriptable", new[] { "Assets/AtahanConversationSystem/Conversations" });

        StringBuilder enumContent = new StringBuilder();
        enumContent.AppendLine("public enum ConversationsIds");
        enumContent.AppendLine("{");

        int count = guids.Length;
        var allConversations = new ConversationScriptable[count];
        int maxID = 0;
        List<int> seenIds = new List<int>();
        for(int n = 0; n < count; n++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[n]);
            allConversations[n] = AssetDatabase.LoadAssetAtPath<ConversationScriptable>(path);
            var safeName = allConversations[n].myConversation.conversationName
                .Replace(' ', '_').Replace("-", "").Replace(",", "").Replace(".", "_");
            enumContent.AppendLine($"    {safeName}={n},");
            maxID = Mathf.Max(maxID, allConversations[n].myConversation.conversationUniqueID+1);
            if (allConversations[n].myConversation.conversationUniqueID >= 0 && seenIds.Contains(allConversations[n].myConversation.conversationUniqueID)) {
                Debug.LogError($"Conversation with same ID as another! {allConversations[n].name}");
            }
            seenIds.Add(allConversations[n].myConversation.conversationUniqueID);
        }

        for (int n = 0; n < count; n++) {
            if (allConversations[n].myConversation.conversationUniqueID < 0) {
                allConversations[n].myConversation.conversationUniqueID = maxID;
                maxID += 1;
            }
        }

        enumContent.AppendLine("}");

        // Write the enum to a file
        File.WriteAllText("Assets/AtahanConversationSystem/Conversations/ConversationNamesEnum.cs", enumContent.ToString());
        
        
        var holder = GameObject.FindObjectOfType<ConversationsHolder>();
        holder.SetConversations(allConversations);
        
        AssetDatabase.Refresh();
    }
}
