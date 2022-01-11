#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Orientor))]
public class RotationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if(GUILayout.Button("Update!")) {
            ((Orientor)this.target).UpdateRotation();
        }
    }
}

#endif