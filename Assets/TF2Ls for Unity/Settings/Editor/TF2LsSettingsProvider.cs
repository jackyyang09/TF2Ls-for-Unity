using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using JackysEditorHelpers;

namespace TF2Ls
{
        // Register a SettingsProvider using IMGUI for the drawing framework:
    class TF2LsSettingsProvider : SettingsProvider
    {
        SerializedObject editorSO;
        SerializedProperty helpTextSize;
        SerializedProperty hlExtractExe;
        SerializedProperty vtfCmdExe;
        SerializedProperty tfPath;
        SerializedProperty unlockSystemObjects;

        SerializedObject runtimeSO;
        SerializedProperty enableFlexesWhenAnimating;

        public TF2LsSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) 
        { 
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            editorSO = TF2LsEditorSettings.SerializedObject;
            helpTextSize = editorSO.FindProperty(nameof(helpTextSize));
            hlExtractExe = editorSO.FindProperty(nameof(hlExtractExe));
            vtfCmdExe = editorSO.FindProperty(nameof(vtfCmdExe));
            tfPath = editorSO.FindProperty(nameof(tfPath));
            unlockSystemObjects = editorSO.FindProperty(nameof(unlockSystemObjects));

            runtimeSO = TF2LsRuntimeSettings.SerializedObject;
            enableFlexesWhenAnimating = runtimeSO.FindProperty(nameof(enableFlexesWhenAnimating));
        }

        public override void OnGUI(string searchContext)
        {
            // This makes prefix labels larger
            EditorGUIUtility.labelWidth += 50;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Runtime Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableFlexesWhenAnimating);

            EditorGUILayout.Space();

            if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                TF2LsRuntimeSettings.Settings.Reset();
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);
            // Validate folder
            EditorHelper.RenderSmartFolderProperty(new GUIContent("tf Path"), tfPath, false, "Select the tf folder within your TF2 installation path");
            if (unlockSystemObjects.boolValue)
            {
                EditorGUILayout.LabelField("Don't touch these files if you don't know what they do. " +
                    "Worst case scenario, you will have to re-import the package.", TF2LsStyles.HelpTextStyle.ApplyBoldText());
            }

            EditorGUILayout.BeginHorizontal();
            TF2LsEditorSettings.DevKey = EditorGUILayout.TextField("Steam Web API Key", TF2LsEditorSettings.DevKey);
            if (GUILayout.Button(new GUIContent("Get API Key", "Login on Steam Community to get your API Key.")))
            {
                Application.OpenURL("https://steamcommunity.com/dev/apikey");
            }
            EditorGUILayout.EndHorizontal();

            // TODO: Is this needed?
            using (new EditorGUI.DisabledScope(!unlockSystemObjects.boolValue))
            {
                EditorGUILayout.PropertyField(hlExtractExe, new GUIContent("HLExtract.exe"));
                EditorGUILayout.PropertyField(vtfCmdExe, new GUIContent("VTFCmd.exe"));
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(helpTextSize, new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
            if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                helpTextSize.intValue--;
            }
            else if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                helpTextSize.intValue++;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("This is an example of Help Text. " +
                "Mouse over open windows to see font changes reflected.",
                TF2LsStyles.HelpTextStyle);

            EditorGUILayout.Space();

            if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                TF2LsEditorSettings.Settings.Reset();
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            runtimeSO.ApplyModifiedProperties();
            if (editorSO.hasModifiedProperties) 
            {
                editorSO.ApplyModifiedProperties();
                TF2LsEditorSettings.Settings.Save();
            }

            EditorGUIUtility.labelWidth -= 50;
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new TF2LsSettingsProvider(TF2LsConstants.Paths.PROJECT_SETTINGS, SettingsScope.Project);
            provider.keywords = GetSearchKeywordsFromSerializedObject(TF2LsEditorSettings.SerializedObject);

            return provider;
        }

        [MenuItem(TF2LsConstants.Paths.SETTINGS, priority = 4)]
        public static void Init()
        {
            SettingsService.OpenProjectSettings(TF2LsConstants.Paths.PROJECT_SETTINGS);
        }
    }
}