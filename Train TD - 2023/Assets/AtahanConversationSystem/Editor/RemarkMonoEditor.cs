using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CanEditMultipleObjects]
[CustomEditor(typeof(RemarkMono))]
public class RemarkMonoEditor : Editor
{

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        RemarkMono myRemark = (RemarkMono)target;
        
        ValidateRemark(myRemark);
    }

    public static void ValidateRemark(RemarkMono myRemark) {
        if (!(myRemark.myRemark.tag == "" || myRemark.myRemark.tag == " ")) {
            myRemark.gameObject.name = "Remark " + myRemark.transform.GetSiblingIndex() + " - " + myRemark.myRemark.tag;
        } else {
            var remarkText = myRemark.myRemark.text;
            var len = remarkText.Length;
            if (len > 24) {
                remarkText = remarkText.Substring(0, 17);
                remarkText += "..." + (len-17);
            }
            myRemark.gameObject.name = "Remark " + myRemark.transform.GetSiblingIndex() + " - " + remarkText;
        }

        int min = Mathf.Min(myRemark.myRemark.bigSpriteAction.Length, myRemark.myRemark.bigSpriteSlot.Length, myRemark.myRemark.bigSprite.Length);
        int max = Mathf.Max(myRemark.myRemark.bigSpriteAction.Length, myRemark.myRemark.bigSpriteSlot.Length, myRemark.myRemark.bigSprite.Length);
        if (min != max) {
            int size = myRemark.myRemark.bigSpriteAction.Length;
            SetArraySize(ref myRemark.myRemark.bigSpriteSlot, size);
            SetArraySize(ref myRemark.myRemark.bigSprite, size);
        }
    }

    static void SetArraySize<T> (ref T[] array, int size) {
        T[] temp = new T[array.Length];
        array.CopyTo (temp,0);

        array = new T[size];
        for (int i = 0; i < array.Length; i++) {
            if(i < temp.Length)
                array[i] = temp[i];
        }
    }
}
