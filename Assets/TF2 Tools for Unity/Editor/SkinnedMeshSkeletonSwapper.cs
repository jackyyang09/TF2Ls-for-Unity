//Copyright(c) 2016 Tim McDaniel (TrickyHandz on forum.unity3d.com)
// Updated by Piotr Kosek 2019 (shelim on forum.unity3d.com)
// Updated by Orochii Zouveleki 2020: Added a couple extra debug info.
// Updated by Jacky Yang (Brogrammist on Github) 2021 - Changed implementation to ScriptableWizard, added support for batch conversion
//
// Adapted from code provided by Alima Studios on forum.unity.com
// http://forum.unity3d.com/threads/prefab-breaks-on-mesh-update.282184/#post-2661445
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to
// the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEditor;
using UnityEngine;

public class SkinnedMeshSkeletonSwapper : ScriptableWizard
{
    public Transform targetSkeleton;
    public SkinnedMeshRenderer[] skinnedMeshes;
    public bool includeInactive;

    bool skipAllMissingBones = false;
    bool enabled = false;

    [MenuItem("Window/TF2Ls for Unity/Skinned Mesh Skeleton Swapper", false, 1)]
    static void CreateWizard()
    {
        DisplayWizard<SkinnedMeshSkeletonSwapper>("Skinned Mesh Skeleton Swapper", "Apply Changes");
    }

    private void OnEnable() { Undo.undoRedoPerformed += OnUndoRedo; }
    private void OnUndoRedo() => GetWindow<SkinnedMeshSkeletonSwapper>().Repaint();
    private void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; }

    void OnWizardCreate()
    {
        if (!enabled)
        {
            EditorUtility.DisplayDialog("Failed!", "Please add a Target Skeleton and at least 1 Skinned Mesh Renderer.", "OK");
            CreateWizard();
            return;
        }

        System.Collections.Generic.List<string> allMissingBones = new System.Collections.Generic.List<string>();
        for (int m = 0; m < skinnedMeshes.Length; m++)
        {
            SkinnedMeshRenderer currentMesh = skinnedMeshes[m];

            // Look for root bone
            string rootName = "";
            if (targetSkeleton != null) rootName = currentMesh.rootBone.name;
            Transform newRoot = null;
            // Reassign new bones
            Transform[] newBones = new Transform[currentMesh.bones.Length];
            Transform[] existingBones = targetSkeleton.GetComponentsInChildren<Transform>(includeInactive);
            System.Collections.Generic.List<string> missingBones = new System.Collections.Generic.List<string>();
            for (int i = 0; i < currentMesh.bones.Length; i++)
            {
                if (currentMesh.bones[i] == null)
                {
                    errorString = System.Environment.NewLine + "WARN: Do not delete the old bones before the skinned mesh is processed!";
                    continue;
                }
                string boneName = currentMesh.bones[i].name;
                bool found = false;
                foreach (var newBone in existingBones)
                {
                    if (newBone.name == rootName) newRoot = newBone;
                    if (newBone.name == boneName)
                    {
                        EditorUtility.DisplayProgressBar("Processing mesh (" + m + "/" + skinnedMeshes.Length + ")", newBone.name + " found!", (float)i / (float)currentMesh.bones.Length);
                        newBones[i] = newBone;
                        found = true;
                    }
                }
                if (!found)
                {
                    if (!skipAllMissingBones)
                    {
                        if (EditorUtility.DisplayDialog("Warning!", boneName + " missing!", "Ignore All Warnings", "Continue")) skipAllMissingBones = true;
                    }
                    missingBones.Add(boneName);
                }
            }
            currentMesh.bones = newBones;
            if (missingBones.Count > 0)
            {
                string missingBonesText = "Finished mesh: " + currentMesh.name + " with missing bones: ";
                for (int i = 0; i < missingBones.Count; i++) missingBonesText += System.Environment.NewLine + missingBones[i];
                EditorUtility.DisplayDialog("Warning", missingBonesText, "OK");
            }
            if (newRoot != null)
            {
                currentMesh.rootBone = newRoot;
            }
            allMissingBones.AddRange(missingBones);
        }
        string finalText = allMissingBones.Count == 0 ? "All Done!" : "Finished with " + allMissingBones.Count + " missing bones: ";
        for (int i = 0; i < allMissingBones.Count; i++) finalText += System.Environment.NewLine + allMissingBones[i];
        EditorUtility.DisplayDialog("Done!", finalText, "OK");
        
        EditorUtility.ClearProgressBar();
    }

    void OnWizardUpdate()
    {
        enabled = (skinnedMeshes != null && targetSkeleton != null);
        errorString = enabled ? "" : "Add a target SkinnedMeshRenderer and a root bone to process.";
    }
}