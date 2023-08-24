namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(PathGeneratorWithRandom), true)]
    [CanEditMultipleObjects]
    public class PathGeneratorWithRandomEditor : MeshGenEditor
    {
        protected override void BodyGUI()
        {
            base.BodyGUI();
            PathGeneratorWithRandom pathGenerator = (PathGeneratorWithRandom)target;
            serializedObject.Update();
            SerializedProperty magnitude = serializedObject.FindProperty("_magnitude");
            SerializedProperty frequency = serializedObject.FindProperty("_frequency");
            SerializedProperty slicesX = serializedObject.FindProperty("_slicesX");
            SerializedProperty slicesY = serializedObject.FindProperty("_slicesY");
            SerializedProperty shape = serializedObject.FindProperty("_shape");
            SerializedProperty shapeExposure = serializedObject.FindProperty("_shapeExposure");
            SerializedProperty useShapeCurve = serializedObject.FindProperty("_useShapeCurve");
            SerializedProperty compensateCorners = serializedObject.FindProperty("_compensateCorners");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Randomness", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(magnitude, new GUIContent("Magnitude"));
            EditorGUILayout.PropertyField(frequency, new GUIContent("Frequency"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(slicesX, new GUIContent("SlicesX"));
            EditorGUILayout.PropertyField(slicesY, new GUIContent("SlicesY"));
            EditorGUILayout.PropertyField(compensateCorners, new GUIContent("Compensate Corners"));

            EditorGUILayout.PropertyField(useShapeCurve, new GUIContent("Use Shape Curve"));
            if (useShapeCurve.boolValue)
            {
                if(shape.animationCurveValue == null || shape.animationCurveValue.keys.Length == 0)
                {
                    shape.animationCurveValue = new AnimationCurve();
                    shape.animationCurveValue.AddKey(new Keyframe(0, 0));
                    shape.animationCurveValue.AddKey(new Keyframe(1, 0));
                }
                if (slicesX.intValue == 1) EditorGUILayout.HelpBox("SlicesX are set to 1. The curve shape may not be approximated correctly. You can increase the slices in order to fix that.", MessageType.Warning);
                if (slicesY.intValue == 1) EditorGUILayout.HelpBox("SlicesY are set to 1. The curve shape may not be approximated correctly. You can increase the slices in order to fix that.", MessageType.Warning);
                EditorGUILayout.PropertyField(shape, new GUIContent("Shape Curve"));
                EditorGUILayout.PropertyField(shapeExposure, new GUIContent("Shape Exposure"));
            }
            if (slicesX.intValue < 1) slicesX.intValue = 1;
            if (slicesY.intValue < 1) slicesY.intValue = 1;
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            

            UVControls(pathGenerator);
        }
    }
}
