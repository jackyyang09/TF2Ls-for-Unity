using UnityEngine;

namespace TF2Ls.FaceFlex
{
    public abstract class BaseQC : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected FaceFlexTool faceFlex;
        public Mesh mesh { get { return renderer.sharedMesh; } }
        protected new SkinnedMeshRenderer renderer => faceFlex.Renderer;

        protected float FlexScale { get { return faceFlex.FlexScale; } }

        public abstract void UpdateBlendShapes();

        private void OnValidate()
        {
            if (!faceFlex) faceFlex = GetComponent<FaceFlexTool>();
        }
    }
}