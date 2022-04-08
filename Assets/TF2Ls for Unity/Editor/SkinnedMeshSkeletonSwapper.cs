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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkinnedMeshSkeletonSwapper : ScriptableWizard
{
    public Transform targetSkeleton;
    public List<SkinnedMeshRenderer> skinnedMeshes = new List<SkinnedMeshRenderer>();
    [Tooltip("If false, will ignore bones whose gameObjects are disabled")]
    public bool includeInactive = true;
    [Tooltip("Copies all missing bones from meshes to targetSkeleton. " +
        "On by default for compatibility, but may clutter your skeleton hierarchy")]
    public bool addMissingBones = true;

    bool skipAllMissingBones = false;
    bool enabled = false;

    [MenuItem("Tools/TF2Ls for Unity/Skinned Mesh Skeleton Swapper", false, 1)]
    static void CreateWizard()
    {
        DisplayWizard<SkinnedMeshSkeletonSwapper>("Skinned Mesh Skeleton Swapper", "Apply Changes");
    }

    private void OnEnable() { Undo.undoRedoPerformed += OnUndoRedo; }
    private void OnUndoRedo() => GetWindow<SkinnedMeshSkeletonSwapper>().Repaint();
    private void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; }

    private void OnValidate()
    {
        if (!targetSkeleton) return;
        List<Transform> existingBones = new List<Transform>(targetSkeleton.GetComponentsInChildren<Transform>(includeInactive));
        var toBeRemoved = new List<SkinnedMeshRenderer>();
        foreach (var mesh in skinnedMeshes)
        {
            if (existingBones.Contains(mesh.rootBone))
            {
                toBeRemoved.Add(mesh);
            }
        }
        if (toBeRemoved.Count > 0)
        {
            for (int i = 0; i < toBeRemoved.Count; i++)
            {
                EditorUtility.DisplayDialog("Warning!", "SkinnedMeshRenderer " + toBeRemoved[i].name +
                    "'s skeleton hierarchy is a child of the targetSkeleton! Please ensure that all " +
                    "origin skeletons are not children of the targetSkeleton.", "OK");
                skinnedMeshes.Remove(toBeRemoved[i]);
            }
            return;
        }
    }

    void OnWizardCreate()
    {
        if (!enabled)
        {
            EditorUtility.DisplayDialog("Failed!", "Please add a Target Skeleton and at least 1 Skinned Mesh Renderer.", "OK");
            CreateWizard();
            return;
        }

        // Find references of existing bones
        List<Transform> existingBones = new List<Transform>(targetSkeleton.GetComponentsInChildren<Transform>(includeInactive));
        Dictionary<string, Transform> boneDictionary = new Dictionary<string, Transform>();

        foreach (var mesh in skinnedMeshes)
        {
            if (existingBones.Contains(mesh.rootBone))
            {
                EditorUtility.DisplayDialog("Error!", "MeshRenderer " + mesh.name +
                    "'s skeleton hierarchy is a child of the targetSkeleton! Please ensure that all " +
                    "origin skeletons are not children of the targetSkeleton.", "OK");
                return;
            }
        }

        foreach (var b in existingBones)
        {
            boneDictionary.Add(b.name, b);
        }

        List<string> allMissingBones = new List<string>();
        for (int m = 0; m < skinnedMeshes.Count; m++)
        {
            SkinnedMeshRenderer currentMesh = skinnedMeshes[m];

            // Look for root bone
            string rootName = "";
            if (targetSkeleton != null) rootName = currentMesh.rootBone.name;
            Transform newRoot = null;

            // Reassign new bones
            Transform[] newBones = new Transform[currentMesh.bones.Length];

            if (boneDictionary.ContainsKey(rootName)) newRoot = boneDictionary[rootName];

            List<string> missingBones = new List<string>();
            for (int i = 0; i < currentMesh.bones.Length; i++)
            {
                if (currentMesh.bones[i] == null)
                {
                    errorString = System.Environment.NewLine + "WARN: Do not delete the old bones before the skinned mesh is processed!";
                    continue;
                }

                string boneName = currentMesh.bones[i].name;

                if (boneDictionary.ContainsKey(boneName))
                {
                    EditorUtility.DisplayProgressBar(
                        "Processing mesh (" + m + "/" + skinnedMeshes.Count + ")", 
                        boneName + " found!", (float)i / (float)currentMesh.bones.Length);
                    newBones[i] = boneDictionary[boneName];
                }
                else
                {
                    if (addMissingBones) // Don't bother doing this with missing roots
                    {
                        var b = LookForBone(currentMesh.bones[i], boneDictionary);
                        if (!boneDictionary.ContainsKey(b.name))
                        {
                            boneDictionary.Add(b.name, b);
                        }
                        if (boneDictionary.ContainsKey(boneName))
                            newBones[i] = boneDictionary[boneName];
                        else // Likely the root bone
                            newBones[i] = targetSkeleton;
                    }
                    else
                    {
                        if (!skipAllMissingBones)
                        {
                            if (EditorUtility.DisplayDialog("Warning!", boneName + " missing!", "Ignore All Warnings", "Continue")) skipAllMissingBones = true;
                        }
                        missingBones.Add(boneName);
                    }
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

    Transform LookForBone(Transform targetBone, Dictionary<string, Transform> bones)
    {
        if (targetBone.parent == null) return targetSkeleton; // This is the root
        string parentName = targetBone.parent.name;
        if (bones.ContainsKey(parentName))
        {
            var t = new GameObject(targetBone.name).transform;
            t.parent = bones[parentName];
            t.localPosition = targetBone.localPosition;
            t.localRotation = targetBone.localRotation;
            t.localScale = targetBone.localScale;
            return t;
        }
        else return LookForBone(targetBone.parent, bones);
    }
}