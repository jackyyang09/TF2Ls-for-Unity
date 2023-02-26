using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TF2Ls.FaceFlex
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SMRLocker))]
    public class SMRLockerEditor : Editor
    {
        SerializedProperty renderer;
        SerializedObject rendererSO;
        SerializedProperty m_Mesh;

        SMRLocker script;

        private void OnEnable()
        {
            script = target as SMRLocker;

            renderer = serializedObject.FindProperty(nameof(renderer));

            rendererSO = new SerializedObject(renderer.objectReferenceValue);
            m_Mesh = rendererSO.FindProperty(nameof(m_Mesh));

            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        void Update()
        {
            rendererSO.UpdateIfRequiredOrScript();
            if (m_Mesh.objectReferenceValue != script.lockedMesh)
            {
                m_Mesh.objectReferenceValue = script.lockedMesh;
                rendererSO.ApplyModifiedProperties();

                EditorUtility.DisplayDialog("Face Flex Tool Warning",
                    "Please do not modify this SkinnedMeshRenderer component while the " +
                    "Face Flex Tool attached to " + script.parent.name + " is actively previewing " +
                    "it's face flexes.\n" +
                    "Disable the face flex preview first!", "Understood");

                EditorGUIUtility.PingObject(script.parent);
                Selection.activeObject = script.parent;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Do not touch me! Do not touch the SkinnedMeshRenderer component either!",
                MessageType.Warning);
        }
    }
#endif

    [AddComponentMenu("")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SMRLocker : MonoBehaviour
    {
        [SerializeField] new SkinnedMeshRenderer renderer;
        public SkinnedMeshRenderer Renderer => renderer;
        public Mesh backupMesh;
        public Mesh lockedMesh;
        public FaceFlexTool parent;

        private void OnValidate()
        {
            renderer = GetComponent<SkinnedMeshRenderer>();
            if (lockedMesh == null)
            {
                lockedMesh = renderer.sharedMesh;
            }

            if (lockedMesh != renderer.sharedMesh)
            {
                renderer.sharedMesh = lockedMesh;
            }
        }
    }
}