using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TF2Ls.FaceFlex
{
    [System.Serializable]
    public class FlexController
    {
        public string Name;
        public Vector2 Range;
    }

    [System.Serializable]
    public class FacePreset
    {
        public string Name;
        public List<string> FlexNames;
        public List<float> FlexValues;
        public List<float> FlexBalances;
        public List<float> FlexMultiLevels;
    }

    public enum MenuState
    {
        Setup,
        FacePoser
    }

    public class FaceFlexTool : MonoBehaviour
    {
#pragma warning disable 0414 // Ignore value unused warnings
#pragma warning disable 0649 // Ignore value unassigned warnings
        [SerializeField] Vector3 normalizedBoundsSize;
        [SerializeField] new SkinnedMeshRenderer renderer;
        public SkinnedMeshRenderer Renderer => renderer;

        [SerializeField] float flexScale = 1;
        public float FlexScale { get { return flexScale; } }
        [SerializeField] List<string> blendshapeNames;
        [SerializeField] List<string> flexControlNames;
        [SerializeField] List<FlexController> flexControllers;
        public float ProcessValue(float raw, int index)
        {
            var control = flexControllers[index];
            var range = control.Range;
            float value;

            value = Mathf.LerpUnclamped(range.x, range.y, raw);

            return value;
        }

        [SerializeReference] List<FlexPreset> flexPresets;

        [SerializeField] BaseQC qcFile;

        [SerializeField] [HideInInspector] string qcPath;
#pragma warning restore 0649
#pragma warning restore 0414
        public Mesh Mesh
        {
            get
            {
                var m = renderer.sharedMesh;
                return m;
            }
        }

        public bool MeshBlendshapesConverted
        {
            get
            {
                if (!renderer) return false;
                for (int i = 0; i < Mesh.blendShapeCount; i++)
                {
                    if (Mesh.GetBlendShapeName(i).Contains("+")) return false;
                }
                return true;
            }
        }

        private void Awake()
        {
            if (!MeshBlendshapesConverted)
            {
                SplitBlendshapes();
            }
        }

        private void Update()
        {
            UpdateBlendShapes();
        }

        [ContextMenu(nameof(UpdateBlendShapes))]
        public void UpdateBlendShapes()
        {
            if (qcFile) qcFile.UpdateBlendShapes();
        }

#if UNITY_EDITOR
        [SerializeField] bool previewingMesh;
        [SerializeField] MenuState menuState = MenuState.Setup;
        [SerializeField] string backupMeshPath, backupMeshName;
        [HideInInspector] public Mesh backupMesh;

        private void OnValidate()
        {
            if (menuState == MenuState.Setup)
            {
                if (!qcFile) qcFile = GetComponent<BaseQC>();
                if (!renderer) renderer = GetComponent<SkinnedMeshRenderer>();
            }

            if (previewingMesh)
            {
                EditorApplication.update += _OnValidate;
            }
        }

        /// <summary>
        /// Roundabout way of dodging SendMessage warning
        /// https://forum.unity.com/threads/sendmessage-cannot-be-called-during-awake-checkconsistency-or-onvalidate-can-we-suppress.537265/
        /// </summary>
        private void _OnValidate()
        {
            EditorApplication.update -= _OnValidate;

            if (Mesh == null)
            {
                LoadMeshFromBackup();
            }

            if (previewingMesh)
            {
                if (MeshBlendshapesConverted) return;
                CreateMeshInstanceInEditor();
                UpdateBlendShapes();
            }
        }

        public void LoadMeshFromBackup()
        {
            if (Mesh == null)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(backupMeshPath);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i].name != backupMeshName) continue;
                    var meshAsset = assets[i] as Mesh;
                    if (meshAsset)
                    {
                        backupMesh = meshAsset;

                        renderer.sharedMesh = meshAsset;
                    }
                }
            }
        }

        public bool CreateMeshInstanceInEditor()
        {
            SplitBlendshapes();

            var locker = renderer.gameObject.AddComponent<SMRLocker>();
            locker.backupMesh = backupMesh;
            locker.parent = this;
            locker.hideFlags = HideFlags.HideAndDontSave;

            return true;
        }
#endif
        float ModelScaleFactor => Mesh.bounds.size.x / normalizedBoundsSize.x;
        float MaxX => 0.035f * 3.5f * ModelScaleFactor;
        float SmoothedX => 0.005f * ModelScaleFactor;
        const float Diff = 0.001f;

        public void SplitBlendshapes()
        {
            if (!renderer) return;
            if (!renderer.sharedMesh) return;

#if UNITY_EDITOR
            var assetPath = AssetDatabase.GetAssetPath(Mesh);
            bool tainted = string.IsNullOrEmpty(assetPath);
            if (tainted) return;

            EditorUtility.DisplayProgressBar("Face Flex Tool",
                    "Generating Face Flexes for " + Mesh.name, 0);

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                var meshAsset = assets[i] as Mesh;
                if (meshAsset) // Was cast successful?
                {
                    if (meshAsset == Mesh)
                    {
                        var so = new SerializedObject(this);
                        so.FindProperty(nameof(backupMeshPath)).stringValue = assetPath;
                        so.FindProperty(nameof(backupMeshName)).stringValue = Mesh.name;

                        so.ApplyModifiedProperties();
                    }
                }
            }

            backupMesh = renderer.sharedMesh;
#endif
            Mesh meshClone = Instantiate(renderer.sharedMesh);
            meshClone.name = renderer.sharedMesh.name;
            meshClone.hideFlags = HideFlags.HideAndDontSave;

            var v = meshClone.vertices;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayProgressBar("Face Flex Tool",
                "Calculating left/right vertex weights", 0.25f);
            }
#endif
            var weightsL = new float[v.Length];
            var weightsR = new float[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                if (Mathf.Abs(v[i].x) < MaxX)
                {
                    if (v[i].x < 0)
                    {
                        weightsL[i] = Mathf.Lerp(1, 0.5f, Mathf.InverseLerp(-SmoothedX, 0, v[i].x));
                        weightsR[i] = Mathf.Lerp(0, 0.5f, Mathf.InverseLerp(-SmoothedX, 0, v[i].x));
                    }
                    else
                    {
                        weightsL[i] = Mathf.Lerp(0.5f, 0, Mathf.InverseLerp(0, SmoothedX, v[i].x));
                        weightsR[i] = Mathf.Lerp(0.5f, 1, Mathf.InverseLerp(0, SmoothedX, v[i].x));
                    }
                }
            }

            List<Vector3[]> blendShapeDeltas = new List<Vector3[]>();
            List<Vector3[]> blendShapeNormals = new List<Vector3[]>();
            List<Vector3[]> blendShapeTangents = new List<Vector3[]>();

            for (int i = 0; i < meshClone.blendShapeCount; i++)
            {
                var verts = new Vector3[meshClone.vertexCount];
                var normals = new Vector3[meshClone.vertexCount];
                var tangents = new Vector3[meshClone.vertexCount];

                meshClone.GetBlendShapeFrameVertices(i, 0, verts, normals, tangents);

                blendShapeDeltas.Add(verts);
                blendShapeNormals.Add(normals);
                blendShapeTangents.Add(tangents);
            }

            meshClone.ClearBlendShapes();

            for (int i = 0; i < blendshapeNames.Count; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.DisplayProgressBar("Face Flex Tool",
                    "Generating BlendShape " + blendshapeNames[i], (float)i / (float)blendshapeNames.Count);
                }
#endif

                if (!blendshapeNames[i].Contains("+"))
                {
                    meshClone.AddBlendShapeFrame(blendshapeNames[i], 1, blendShapeDeltas[i], blendShapeNormals[i], blendShapeTangents[i]);
                }
                else
                {
                    var nameSplits = blendshapeNames[i].Split('+');
                    var leftDeltas = new Vector3[v.Length];
                    var leftNormals = new Vector3[v.Length];
                    for (int j = 0; j < leftDeltas.Length; j++)
                    {
                        leftDeltas[j] = blendShapeDeltas[i][j] * weightsL[j];
                        leftNormals[j] = blendShapeNormals[i][j] * weightsL[j];
                    }
                    meshClone.AddBlendShapeFrame(nameSplits[0], 1, leftDeltas, leftNormals, blendShapeTangents[i]);

                    var rightDeltas = new Vector3[v.Length];
                    var rightNormals = new Vector3[v.Length];
                    for (int j = 0; j < leftDeltas.Length; j++)
                    {
                        rightDeltas[j] = blendShapeDeltas[i][j] * weightsR[j];
                        rightNormals[j] = blendShapeNormals[i][j] * weightsR[j];
                    }
                    meshClone.AddBlendShapeFrame(nameSplits[1], 1, rightDeltas, rightNormals, blendShapeTangents[i]);
                }
            }

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
            if (!previewingMesh) Undo.RecordObject(this, "Changed Mesh Preview State");
            previewingMesh = true;

            renderer.sharedMesh = meshClone;
        }
    }
}