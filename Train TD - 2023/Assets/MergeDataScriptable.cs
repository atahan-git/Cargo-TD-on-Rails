using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu]
public class MergeDataScriptable : ScriptableObject {
    [ValueDropdown("GetAllModuleNames")]
    public string source1;
    [ValueDropdown("GetAllModuleNamesWithRandomArtifact")]
    public string source2;
    [Space]
    [ValueDropdown("GetAllModuleNames")]
    public string result;

    public bool hasBonusGem = false;
    [ValueDropdown("GetAllGemNames")]
    [ShowIf("hasBonusGem")]
    public string bonusGem;


    public MergeData GetMergeData() {
        var data = new MergeData();
        
        var inputStrings = new List<string>() { DataHolder.PreProcess(source1), DataHolder.PreProcess(source2) };
        inputStrings.Sort();
        
        data.sources = new string[2];
        data.sources[0] = inputStrings[0];
        data.sources[1] = inputStrings[1];

        data.result = result;

        data.hasBonusGem = hasBonusGem;
        data.bonusGem = bonusGem;

        return data;
    }
    
    private static IEnumerable GetAllGemNames() {
        var allBuildings =  GameObject.FindObjectOfType<DataHolder>().GetAllPossibleArtifactNames();
        return allBuildings;
    }
    private static IEnumerable GetAllModuleNames() {
        var allBuildings =  GameObject.FindObjectOfType<DataHolder>().GetAllPossibleBuildingNames();
        return allBuildings;
    }
    
    private static IEnumerable GetAllModuleNamesWithRandomArtifact() {
        var allBuildings =  GameObject.FindObjectOfType<DataHolder>().GetAllPossibleBuildingNames();
        allBuildings.Add(DataHolder.anyArtifact);
        return allBuildings;
    }
}


public class MergeData {
    public string[] sources;
    public string result;
    public bool hasBonusGem;
    public string bonusGem;
}
