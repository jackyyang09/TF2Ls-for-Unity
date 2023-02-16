using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField] float flexScale = 1;
        public float FlexScale { get { return flexScale; } }
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

        public Mesh Mesh { get { return renderer.sharedMesh; } }
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

        [SerializeField] MenuState menuState = MenuState.Setup;

        private void OnValidate()
        {
            if (menuState == MenuState.Setup)
            {
                if (!qcFile) qcFile = GetComponent<BaseQC>();
                if (!renderer) renderer = GetComponent<SkinnedMeshRenderer>();
            }
            UpdateBlendShapes();
        }

        [ContextMenu(nameof(UpdateBlendShapes))]
        public void UpdateBlendShapes()
        {
            if (qcFile) qcFile.UpdateBlendShapes();
        }
    }
}