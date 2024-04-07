using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NewspaperTextHolderScriptable : ScriptableObject
{
    [ Multiline(10)]
    public string introText;

    [Multiline(9)] public string[] cartTypesTexts = new string[3];
    [Multiline(9)] public string[] totalCartCountText = new string[2];

	[Multiline(4)] public string[] cartTechTexts = new string[3];
	[Multiline(4)] public string[] gemTechTexts = new string[3];
	
	[ Multiline(10)]
	public string didNotMakeItText;
	[ Multiline(10)]
	public string didMakeItText;
}
