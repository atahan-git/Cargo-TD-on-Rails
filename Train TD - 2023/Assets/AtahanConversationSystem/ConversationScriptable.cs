using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConversationSystem;

namespace ConversationSystem {
	public class ConversationScriptable : ScriptableObject {
		public Conversation myConversation;
	}
	
	[System.Serializable]
	public class Conversation {
		public ConversationType myType = ConversationType.none;
		public int conversationUniqueID = -1;
		public string GetDisplayName() {
			return conversationName;
			
		}
		public string conversationName = "new conversation";
		public Remark[] remarks = new Remark[0];
	}

	public enum ConversationType {
		none=0, prologue=1, mainStory=2, miniTutorials=3
	}


	[System.Serializable]
	public class Remark {
		public string tag;

		[TextArea]
		[Tooltip("<delay='waitSeconds'>,\n <wait='click/enabled'>,\n <give='itemType'-'itemId'>,\n <trigger='commandID'>")]
		public string text;
		public ConversationEnumToSprite.Characters portrait;

		public BigSpriteAction[] bigSpriteAction;
		public BigSpriteSlots[] bigSpriteSlot;
		public ConversationEnumToSprite.Characters[] bigSprite;
	}
	
	
	public enum BigSpriteAction { Show, Hide };
	public enum BigSpriteSlots { left = 0, middle = 1, right = 2 };
}
