using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TF2Ls.FaceFlex
{
    [InitializeOnLoad]
    public class Startup
    {
        static Startup()
        {
            EditorApplication.update += RunOnStartup;
        }

        private static void RunOnStartup()
        {
            // Check if started up this session
            if (!SessionState.GetBool("Startup", false))
            {
                //Init();

                // Set true if not
                SessionState.SetBool("Startup", true);
            }
        }
        
        static void Init()
        {
            var so = new SerializedObject(ConvertedMeshList.Instance);
            so.FindProperty("list").ClearArray();
            so.ApplyModifiedProperties();

            EditorApplication.update -= RunOnStartup;

            var flexers = Object.FindObjectsOfType<FaceFlexTool>();
            for (int i = 0; i < flexers.Length; i++)
            {
                flexers[i].SplitBlendshapes();
                ConvertedMeshList.List.Add(flexers[i].Mesh.name);
            }
        }
    }

    public class Example : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject g)
        {
            Debug.Log(g.name);
            Debug.Log(assetPath);
            if (g.name == "sniperHWM")
            {
                var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                Debug.Log(assets.Length);
                for (int i = 0; i < assets.Length; i++)
                {
                    Debug.Log(assets[i].name);
                }
            }
        }
    }

    [CustomEditor(typeof(ConvertedMeshList))]
    public class ConvertedMeshListEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("TEST"))
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Test Assets/sniperHWM.fbx");
                for (int i = 0; i < assets.Length; i++)
                {
                    var go = assets[i] as GameObject;
                    if (go == null) continue;
                    var renderer = (assets[i] as GameObject).GetComponent<SkinnedMeshRenderer>();
                    assets[i].name = "Benis" + i;
                    EditorUtility.SetDirty(assets[i]);
                    if (renderer)
                    {
                        if (renderer.sharedMesh.name == "sniper_morphs_high")
                        {
                            var array = new Vector3[renderer.sharedMesh.vertexCount];
                            renderer.sharedMesh.AddBlendShapeFrame("TEST", 1, array, array, array);
                            Debug.Log("SUCCESS");
                            EditorUtility.SetDirty(assets[i]);
                        }
                    }
                }
                EditorUtility.SetDirty(assets[0]);
                Debug.Log(assets[0]);
                AssetDatabase.ImportAsset("Assets/Test Assets/sniperHWM.fbx");
            }
            

            base.OnInspectorGUI();
        }
    }
}