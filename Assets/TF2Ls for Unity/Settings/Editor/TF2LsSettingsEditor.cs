using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TF2Ls
{
    [CustomEditor(typeof(TF2LsEditorSettings))]
    public class TF2LsEditorSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Settings"))
            {
                TF2LsSettingsProvider.Init();
            }
        }
    }

    [CustomEditor(typeof(TF2LsRuntimeSettings))]
    public class TF2LsRuntimeSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Settings"))
            {
                TF2LsSettingsProvider.Init();
            }
        }
    }
}