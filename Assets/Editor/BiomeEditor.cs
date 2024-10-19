using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(TileMaterial))]
public class TileMaterialDrawer : PropertyDrawer {
   public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      return  EditorGUIUtility.singleLineHeight;
   }

   public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var firstRect = new Rect(position);
        firstRect.height = EditorGUIUtility.singleLineHeight;

        SerializedProperty level = property.FindPropertyRelative("level");
        
        if ((TileMaterial.Level)level.enumValueIndex == TileMaterial.Level.Land)
            EditorGUI.PropertyField(firstRect, property.FindPropertyRelative("land"), label);
        else
            EditorGUI.PropertyField(firstRect, property.FindPropertyRelative("roof"), label);
    }
}

#endif