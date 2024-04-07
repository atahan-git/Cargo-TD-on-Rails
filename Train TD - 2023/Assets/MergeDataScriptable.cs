using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu]
public class MergeDataScriptable : ScriptableObject {
    [ValueDropdown("GetAllModuleNames")]
    public string source1;
    [ValueDropdown("GetAllModuleNames")]
    public string source2;
    [Space]
    [ValueDropdown("GetAllModuleNames")]
    public string result;


    public MergeData GetMergeData() {
        var data = new MergeData();
        
        var inputStrings = new List<string>() { DataHolder.PreProcess(source1), DataHolder.PreProcess(source2) };
        inputStrings.Sort();
        
        data.sources = new string[2];
        data.sources[0] = inputStrings[0];
        data.sources[1] = inputStrings[1];

        data.result = result;

        return data;
    }
    
    private static IEnumerable GetAllModuleNames() {
        return GameObject.FindObjectOfType<DataHolder>().GetAllPossibleBuildingNames();
    }
}


public class MergeData {
    public string[] sources;
    public string result;
}
