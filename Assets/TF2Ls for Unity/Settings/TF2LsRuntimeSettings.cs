using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TF2Ls
{
    public class TF2LsRuntimeSettings : ScriptableObject
    {
        [Tooltip("If true, Face Flex tool will prompt you to enable Face Flex previews when " +
            "previewing Animations in the Editor")]
        [SerializeField] bool enableFlexesWhenAnimating = true;
        public bool EnableFlexesWhenAnimating => enableFlexesWhenAnimating;

#if UNITY_EDITOR
        static TF2LsRuntimeSettings settings;
        public static TF2LsRuntimeSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    var asset = Resources.Load(nameof(TF2LsRuntimeSettings));
                    settings = asset as TF2LsRuntimeSettings;
#if UNITY_EDITOR
                    if (settings == null) TryCreateNewSettingsAsset();
#endif
                }
                return settings;
            }
        }

        static readonly string SETTINGS_PATH = "Assets/Settings/Resources/" + nameof(TF2LsRuntimeSettings) + ".asset";

        public static void TryCreateNewSettingsAsset()
        {
            if (!EditorUtility.DisplayDialog(
                "TF2Ls First Time Setup",
                "In order to function, TF2Ls needs a place to store settings. By default, a " +
                "Settings asset will be created at Assets/Settings/Resources/, but you may move it " +
                "elsewhere, so long as it's in a Resources folder.\n" +
                "Moving it out of the Resources folder will prompt this message to appear again erroneously!",
                "Ok Create It.", "Not Yet!")) return;

            var asset = CreateInstance<TF2LsRuntimeSettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Resources")) AssetDatabase.CreateFolder("Assets/Settings", "Resources");
            AssetDatabase.CreateAsset(asset, SETTINGS_PATH);
            asset.Reset();

            settings = asset;
        }

        static SerializedObject serializedObject;
        public static SerializedObject SerializedObject
        {
            get
            {
                if (serializedObject == null)
                {
                    if (!Settings) return null;
                    serializedObject = new SerializedObject(Settings);
                    return serializedObject;
                }
                return serializedObject;
            }
        }

        public void Reset()
        {
            Undo.RecordObject(this, "Reset TF2Ls Runtime Settings");
            enableFlexesWhenAnimating = true;
        }
#endif
    }
}