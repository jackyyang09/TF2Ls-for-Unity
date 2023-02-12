using System.Collections.Generic;
using UnityEngine;

namespace TF2Ls
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class FaceFlexTool : MonoBehaviour
    {
        [System.Serializable]
        public class FlexController
        {
            public string Name;
            public Vector2 Range;
            public bool IsGmod;
        }

        [System.Serializable]
        public class FlexPreset
        {
            public string Name;
            public List<string> FlexNames;
            public List<float> FlexValues;
            public List<float> FlexBalances;
            public List<float> FlexMultiLevels;
        }

        [SerializeField] new SkinnedMeshRenderer renderer;

        [SerializeField] float flexScale = 1;
        public float FlexScale { get { return flexScale; } }
        [SerializeField] List<string> flexControlNames;
        [SerializeField] List<FlexController> flexControllers;
        public float ProcessValue(float raw, int index)
        {
            var control = flexControllers[index];
            var range = control.Range;
            float value;
            
            if (control.IsGmod)
            {
                value = Mathf.LerpUnclamped(range.x, range.y, raw);
            }
            else
            {
                value = raw;
            }

            return value;
        }

        [SerializeReference] List<FlexPreset> flexPresets;

        [SerializeField] BaseQC qcFile;

        [SerializeField] [HideInInspector] string qcPath;

        public Mesh Mesh { get { return renderer.sharedMesh; } }

        [SerializeField] bool gmodMode = false;
        [SerializeField] bool unclampSliders = false;

        private void OnValidate()
        {
            if (!qcFile) qcFile = GetComponent<BaseQC>();
            if (!renderer) renderer = GetComponent<SkinnedMeshRenderer>();
            UpdateBlendShapes();
        }

        [ContextMenu(nameof(UpdateBlendShapes))]
        public void UpdateBlendShapes()
        {
            if (qcFile) qcFile.UpdateBlendShapes();
        }
    }
}