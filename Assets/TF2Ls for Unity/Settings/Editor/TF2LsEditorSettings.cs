using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace TF2Ls
{
    public class TF2LsEditorSettings : ScriptableObject
    {
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
            var guids = AssetDatabase.FindAssets("t:" + nameof(TF2LsEditorSettings).ToLower());
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            packagePath = path.Remove(path.IndexOf("/Settings/Editor/TF2LsSettings.asset"));
        }

        public static string ResourcesPath => "Assets/Resources/TF2Ls Generated";

        [Tooltip("Font size of helper text")]
        [SerializeField] int helpTextSize = 10;
        public int HelpTextSize => helpTextSize;

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

        const string DEVKEY_KEY = nameof(TF2LsEditorSettings) + nameof(DEVKEY_KEY);
        public static string DevKey
        {
            get => EditorPrefs.GetString(DEVKEY_KEY);
            set => EditorPrefs.SetString(DEVKEY_KEY, value);
        }

        static readonly string SETTINGS_PATH = System.Environment.CurrentDirectory + "\\ProjectSettings\\" + nameof(TF2LsEditorSettings) + ".asset";

        static TF2LsEditorSettings settings;
        public static TF2LsEditorSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    Object[] files = new Object[0];
                    if (!File.Exists(SETTINGS_PATH))
                    {
                        var newSettings = CreateInstance<TF2LsEditorSettings>();
                        newSettings.Reset();

                        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(
                            new Object[]{ newSettings }, SETTINGS_PATH, true);

                        files = new Object[] { newSettings };
                    }
                    else
                    {
                        files = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(SETTINGS_PATH);
                    }

                    for (int i = 0; i < files.Length; i++)
                    {
                        settings = files[i] as TF2LsEditorSettings;
                        if (settings) return settings;
                    }
                    
                    if (settings == null)
                    {
                        Debug.LogError(nameof(TF2LsEditorSettings) + "Error: TF2Ls Editor Settings failed to create " + 
                        "persistent asset!");
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

        public void Save()
        {
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(
                            new Object[]{ Settings }, SETTINGS_PATH, true);
        }

        public void Reset()
        {
            Undo.RecordObject(this, "Reset TF2Ls Editor Settings");
            tfPath = "";
            helpTextSize = 10;
        }
    }
}