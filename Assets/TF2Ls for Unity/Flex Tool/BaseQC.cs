using UnityEngine;

namespace TF2Ls
{
    public abstract class BaseQC : MonoBehaviour
    {
        [SerializeField] protected FaceFlexTool faceFlex;
        [SerializeField] protected new SkinnedMeshRenderer renderer;
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