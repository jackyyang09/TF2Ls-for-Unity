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

        void TryConvertSelf()
        {
            SplitBlendshapes();
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
        Mesh backupMesh;

        private void OnValidate()
        {
            if (menuState == MenuState.Setup)
            {
                if (!qcFile) qcFile = GetComponent<BaseQC>();
                if (!renderer) renderer = GetComponent<SkinnedMeshRenderer>();
            }

            if (previewingMesh)
            {
                if (backupMesh == null)
                {
                    CreateMeshInstanceInEditor();
                }

                UpdateBlendShapes();
            }
            else if (!MeshBlendshapesConverted) return;
        }

        /// <summary>
        /// This is what we call, a "Pro Gamer Move"
        /// </summary>
        [ContextMenu(nameof(CreateMeshInstanceInEditor))]
        public bool CreateMeshInstanceInEditor()
        {
            if (renderer.sharedMesh == null) return false;

            SplitBlendshapes();

            var locker = renderer.gameObject.AddComponent<SMRLocker>();
            locker.backupMesh = backupMesh;
            locker.parent = this;
            locker.hideFlags = HideFlags.HideAndDontSave;

            return true;
        }

        [ContextMenu(nameof(UnloadEditorMesh))]
        public void UnloadEditorMesh()
        {
            if (!previewingMesh) return;

            previewingMesh = false;

            DestroyImmediate(renderer.sharedMesh);

            renderer.sharedMesh = backupMesh;

            if (renderer.gameObject.TryGetComponent(out SMRLocker locker))
            {
                DestroyImmediate(locker);
            }

            backupMesh = null;
        }
#endif

        const float MaxX = 3.5f;
        const float SmoothedX = 0.5f;
        const float Diff = 0.001f;

        public void SplitBlendshapes()
        {
            if (!renderer) return;
            if (!renderer.sharedMesh) return;

#if UNITY_EDITOR
            bool tainted = AssetDatabase.GetAssetPath(renderer.sharedMesh).Equals("");
            if (tainted) return;

            previewingMesh = true;

            backupMesh = renderer.sharedMesh;
#endif
            Mesh meshClone = Instantiate(renderer.sharedMesh);
            meshClone.hideFlags = HideFlags.HideAndDontSave;

            var v = meshClone.vertices;

            var weightsL = new float[v.Length];
            var weightsR = new float[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.DisplayProgressBar("Face Flex Tool",
                    "Calculating left/right vertex weights", (float)i / (float)v.Length);
                }
#endif
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
                if (Application.isPlaying)
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
            renderer.sharedMesh = meshClone;
        }
    }
}