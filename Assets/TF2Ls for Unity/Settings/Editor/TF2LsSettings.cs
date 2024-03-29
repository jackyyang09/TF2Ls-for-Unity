using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JackysEditorHelpers;
using System.IO;

namespace TF2Ls
{
    public class TF2LsSettings : ScriptableObject
    {
        [Tooltip("If true, Face Flex tool will prompt you to enable Face Flex previews when " +
            "previewing Animations in the Editor")]
        [SerializeField] bool enableFlexesWhenAnimating;
        public bool EnableFlexesWhenAnimating => enableFlexesWhenAnimating;

        [SerializeField] string packagePath;
        public string PackagePath
        {
            get
            {
                if (!AssetDatabase.IsValidFolder(packagePath))
                {
                    CacheProjectPath();
                }
                return packagePath;
            }
        }

        [ContextMenu(nameof(CacheProjectPath))]
        public void CacheProjectPath()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(TF2LsSettings).ToLower());
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            packagePath = path.Remove(path.IndexOf("/Settings/Editor/TF2LsSettings.asset"));
        }

        public static string ResourcesPath => "Assets/Resources/TF2Ls Generated";

        [Tooltip("Font size of helper text")]
        [SerializeField] int helpTextSize = 10;
        public GUIStyle HelpTextStyle { get { return new GUIStyle(EditorStyles.helpBox).SetFontSize(helpTextSize); } }

        [SerializeField] string tfPath;
        public string TFInstallPath => tfPath;
        public bool TFInstallExists 
        { 
            get 
            { 
                if (Directory.Exists(tfPath))
                {
                    if (File.Exists(Path.Combine(tfPath, ModelTexturerWindow.VTF_VPK_FILENAME))) return true;
                }
                return false;
            }
        }

        [Tooltip("Allows free editing of advanced properties. Un-check this at your own risk.")]
        [SerializeField] bool unlockSystemObjects;

        [SerializeField] UnityEngine.Object hlExtractExe;
        public string HLExtractPath
        {
            get
            {
                string exePath = Path.Combine(System.Environment.CurrentDirectory,
                    AssetDatabase.GetAssetPath(hlExtractExe));
                return exePath;
            }
        }

        [SerializeField] UnityEngine.Object vtfCmdExe;
        public string VTFCmdPath
        { 
            get
            {
                string exePath = Path.Combine(System.Environment.CurrentDirectory,
                    AssetDatabase.GetAssetPath(vtfCmdExe));
                return exePath;
            }
        }

        static TF2LsSettings settings;
        public static TF2LsSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    var guids = AssetDatabase.FindAssets("t:" + nameof(TF2LsSettings));
                    for (int i = 0; i < guids.Length; i++)
                    {
                        settings = AssetDatabase.LoadAssetAtPath<TF2LsSettings>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    }
                }
                return settings;
            }
        }

        static SerializedObject serializedObject;
        public static SerializedObject SerializedObject
        {
            get
            {
                if (serializedObject == null)
                {
                    serializedObject = new SerializedObject(Settings);
                    return serializedObject;
                }
                return serializedObject;
            }
        }

        [MenuItem(AboutEditor.MENU_DIRECTORY + "TF2Ls Settings", priority = 4)]
        public static void Init()
        {
            SettingsService.OpenProjectSettings("Project/TF2Ls");
        }

        public void Reset()
        {
            Undo.RecordObject(this, "Reset TF2LsSettings");
            tfPath = "";
            helpTextSize = 10;
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class TF2LsSettingsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/TF2Ls", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    EditorGUIUtility.labelWidth += 50;

                    var settings = TF2LsSettings.SerializedObject;
                    SerializedProperty enableFlexesWhenAnimating = settings.FindProperty(nameof(enableFlexesWhenAnimating));
                    SerializedProperty helpTextSize = settings.FindProperty(nameof(helpTextSize));
                    SerializedProperty hlExtractExe = settings.FindProperty(nameof(hlExtractExe));
                    SerializedProperty vtfCmdExe = settings.FindProperty(nameof(vtfCmdExe));
                    SerializedProperty tfPath = settings.FindProperty(nameof(tfPath));
                    SerializedProperty unlockSystemObjects = settings.FindProperty(nameof(unlockSystemObjects));

                    EditorGUILayout.LabelField("Face Flex Tool", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(enableFlexesWhenAnimating);

                    EditorGUILayout.Space();

                    // Validate folder
                    EditorHelper.RenderSmartFolderProperty(new GUIContent("tf Path"), tfPath, false, "Select the tf folder within your TF2 installation path");
                    if (unlockSystemObjects.boolValue)
                    {
                        EditorGUILayout.LabelField("Don't touch these files if you don't know what they do. " +
                            "Worst case scenario, you will have to re-import the package.", TF2LsSettings.Settings.HelpTextStyle.ApplyBoldText());
                    }
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
                        TF2LsSettings.Settings.HelpTextStyle);

                    if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        TF2LsSettings.Settings.Reset();
                    }

                    if (settings.hasModifiedProperties)
                    {
                        settings.ApplyModifiedProperties();
                    }

                    EditorGUIUtility.labelWidth -= 50;
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "TF2Ls", "Team Fortress", "Package", "Presets", "Enums" })
            };

            return provider;
        }
    }
}