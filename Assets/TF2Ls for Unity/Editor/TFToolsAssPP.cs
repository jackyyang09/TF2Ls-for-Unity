using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TF2Ls
{
    public class TFToolsAssPP : AssetPostprocessor
    {
        public static System.Action<Texture2D[]> OnTexturesImported;
        public static System.Action<MonoScript> OnMonoScriptImported;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (OnTexturesImported != null)
            {
                List<Texture2D> loadedTextures = new List<Texture2D>();
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(importedAssets[i]);
                    if (t) loadedTextures.Add(t);
                }
                OnTexturesImported?.Invoke(loadedTextures.ToArray());
            }

            if (OnMonoScriptImported != null)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    var s = AssetDatabase.LoadAssetAtPath<MonoScript>(importedAssets[i]);
                    if (s)
                    {
                        OnMonoScriptImported?.Invoke(s);
                    }
                }
            }
        }
    }
}