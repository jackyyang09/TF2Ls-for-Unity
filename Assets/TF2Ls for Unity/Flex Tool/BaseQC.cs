using UnityEngine;

namespace TF2Ls.FaceFlex
{
    public abstract class BaseQC : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected FaceFlexTool faceFlex;
        [SerializeField, HideInInspector] protected new SkinnedMeshRenderer renderer;
        public Mesh mesh { get { return renderer.sharedMesh; } }

        protected float FlexScale { get { return faceFlex.FlexScale; } }

        public abstract void UpdateBlendShapes();

        private void OnValidate()
        {
            if (!faceFlex) faceFlex = GetComponent<FaceFlexTool>();
            if (!renderer) renderer = GetComponent<SkinnedMeshRenderer>();
        }
    }
}