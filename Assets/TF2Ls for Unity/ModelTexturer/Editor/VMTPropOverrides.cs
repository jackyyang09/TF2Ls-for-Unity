using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TF2Ls
{
    [CustomPropertyDrawer(typeof(VMTPropOverrides.ShaderOverride))]
    public class ShaderOverrideDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty tag = property.FindPropertyRelative(nameof(tag));
            SerializedProperty shader = property.FindPropertyRelative(nameof(shader));

            float spacing = 5;

            Rect currentRect = new Rect(position);
            currentRect.width = position.width / 2f - spacing;
            EditorGUI.PropertyField(currentRect, tag, GUIContent.none);

            currentRect.position += new Vector2(currentRect.width + spacing, 0);
            EditorGUI.PropertyField(currentRect, shader, GUIContent.none);
            
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(VMTPropOverrides.PropertyOverride))]
    public class PropertyOverrideDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty tag = property.FindPropertyRelative(nameof(tag));
            SerializedProperty shaderProperty = property.FindPropertyRelative(nameof(shaderProperty));
            SerializedProperty propertyType = property.FindPropertyRelative(nameof(propertyType));

            float spacing = 5;

            Rect currentRect = new Rect(position);
            currentRect.width = position.width / 3f - spacing;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(currentRect, tag, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                var substring = tag.stringValue.Substring(1);
                if (substring.Contains("$"))
                {
                    tag.stringValue = "$" + tag.stringValue.Replace("$", "");
                }
                else if (!tag.stringValue.Contains("$"))
                {
                    tag.stringValue = "$" + tag.stringValue;
                }
            }

            currentRect.position += new Vector2(currentRect.width + spacing, 0);
            EditorGUI.PropertyField(currentRect, shaderProperty, GUIContent.none);

            currentRect.position += new Vector2(currentRect.width + spacing, 0);
            EditorGUI.PropertyField(currentRect, propertyType, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }

    [CreateAssetMenu(fileName = "New VMT Overrides", menuName = "TF2Ls for Unity/VMTPropOverrides")]
    public class VMTPropOverrides : ScriptableObject
    {
        [System.Serializable]
        public struct ShaderOverride
        {
            public string tag;
            public Shader shader;
        }

        [System.Serializable]
        public struct PropertyOverride
        {
            public string tag;
            public string shaderProperty;
            public PropertyType propertyType;
        }

        public enum PropertyType
        {
            Float,
            Colour,
            Texture
        }

        public Material defaultMaterial;
        public List<ShaderOverride> shaderOverrides;
        public List<PropertyOverride> propertyOverrides;
    }
}