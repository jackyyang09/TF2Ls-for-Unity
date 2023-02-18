using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TF2Ls.FaceFlex
{
    public class ConvertedMeshList : ScriptableObject
    {
        [SerializeField] List<string> list = new List<string>();
        public static List<string> List => Instance.list;

        const string folder = "Assets/Resources/TF2LsGenerated/";

        static ConvertedMeshList instance;
        public static ConvertedMeshList Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ConvertedMeshList>("TF2LsGenerated/" + nameof(ConvertedMeshList));
                    if (instance == null)
                    {
                        var asset = CreateInstance<ConvertedMeshList>();
#if UNITY_EDITOR
                        if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                        {
                            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "TF2LsGenerated");
                        }
                        var path = folder + nameof(ConvertedMeshList) + ".asset";
                        UnityEditor.AssetDatabase.CreateAsset(asset, path);
#endif
                        instance = asset;
                    }
                }
                return instance;
            }
        }
    }
}