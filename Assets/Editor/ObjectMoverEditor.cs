using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectMover))]
public class ObjectMoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Campos generales
        EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startActive"));

        EditorGUILayout.Space();

        ObjectMover.MoverType type = (ObjectMover.MoverType)serializedObject.FindProperty("type").enumValueIndex;

        switch (type)
        {
            case ObjectMover.MoverType.Linear:
                EditorGUILayout.LabelField("Linear", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("movementCurve"));
                break;

            case ObjectMover.MoverType.Pendulum:
                EditorGUILayout.LabelField("Pendulum", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivotAxis"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pendulumCurve"));
                break;

            case ObjectMover.MoverType.PendulumBob:
                EditorGUILayout.LabelField("Pendulum Bob", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivotOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bobSwingAxis"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pendulumCurve"));
                break;

            case ObjectMover.MoverType.Vanishing:
                EditorGUILayout.LabelField("Vanishing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("vanishDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("respawnTime"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
