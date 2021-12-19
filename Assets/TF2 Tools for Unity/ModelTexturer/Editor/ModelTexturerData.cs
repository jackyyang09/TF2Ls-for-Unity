using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JackysEditorHelpers;
using Ibasa.Valve.Vmt;

namespace TFTools
{
    public class ModelTexturerData: ScriptableObject
    {
        public string hlExtractPath;
        public string packagePath;
        public string exportPath = "C:/HLExport";
        public string itemToExtract = "root/";

        public string vtfLibPath;
        public string vtfInput;
        public string vtfFolderInput;
        public string textureOutput = "C:/TextureExport";

        public string textureInputFolderPath;
        public string textureOutputFolderPath;
        public string generatedMaterialSavePath;

        public bool showHelpText = true;
        public int helpTextSize = 10;
        public bool unlockSystemObjects;

        public DefaultAsset hlExtractExe;
        public DefaultAsset vtfCmdExe;
        public Material referenceMaterial;
        public Shader nonPaintedShader;
        public Shader paintedShader;
        public string[] normalMapTags = new string[] { "_normal" };
        public string[] specularMapTags = new string[] { "_exp", "_exponent" };
    }

    public class ModelTexturerWindow : TFToolsSerializedEditorWindow<ModelTexturerData, ModelTexturerWindow>
    {
        const string PREF_LOCATION_KEY = "TFTOOLS_PREFPATH";
        static string preferencesPath
        {
            get
            {
                return EditorPrefs.GetString(PREF_LOCATION_KEY);
            }
            set
            {
                EditorPrefs.SetString(PREF_LOCATION_KEY, value);
            }
        }

        const string SCROLL_X_KEY = "TFTOOLS_WINDOWSCROLL_X";
        static float scrollX
        {
            get { return EditorPrefs.GetFloat(SCROLL_X_KEY); }
            set { EditorPrefs.SetFloat(SCROLL_X_KEY, value); }
        }

        const string SCROLL_Y_KEY = "TFTOOLS_WINDOWSCROLL_Y";
        static float scrollY
        {
            get { return EditorPrefs.GetFloat(SCROLL_Y_KEY); }
            set { EditorPrefs.SetFloat(SCROLL_Y_KEY, value); }
        }

        static Vector2 scrollProgress
        {
            get { return new Vector2(scrollX, scrollY); }
            set { scrollX = value.x;  scrollY = value.y; }
        }

        static ModelTexturerData prefData;
        public static ModelTexturerData ModelTexturerData
        {
            get
            {
                if (prefData == null)
                {
                    prefData = AssetDatabase.LoadAssetAtPath<ModelTexturerData>(preferencesPath);
                    if (prefData == null)
                    {
                        var guids = AssetDatabase.FindAssets("t:ModelTexturerData");
                        for (int i = 0; i < guids.Length; i++)
                        {
                            prefData = AssetDatabase.LoadAssetAtPath<ModelTexturerData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                        }
                    }
                }
                return prefData;
            }
        }

        static SerializedObject SerializedObject
        {
            get
            {
                if (serializedObject == null && ModelTexturerData != null) serializedObject = new SerializedObject(ModelTexturerData);
                return serializedObject;
            }
        }

        GUIStyle helpTextStyle { get { return new GUIStyle(EditorStyles.helpBox).SetFontSize(helpTextSize.intValue); } }
        bool queueRefresh = false;

        System.Diagnostics.Process commandShell;

        Action OnShellProcessComplete;

        [MenuItem("Window/TF2Ls for Unity/About", false, 3)]
        public static void About()
        {
            if (EditorUtility.DisplayDialog("TF2Ls for Unity (Version 0.0.2)",
                "This is the 2021 TF2 Winter Jam bug-fix release of TF2Ls! Thanks for the support!" +
                System.Environment.NewLine + "Check me out on Twitter for more TF2 Unity stuff", "Check out Twitter", "Close"))
            {
                Application.OpenURL("https://twitter.com/Brogrammist");
            }
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/TF2Ls for Unity/Model Texturer", false, 2)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            Window.Show();
            window.SetWindowTitle();
            window.Focus();
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Model Texturer");
            window.minSize = new Vector2(400, 300);
        }

        protected override void OnEnable()
        {
            DesignateSerializedProperties();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnUndoRedo() => GetWindow<ModelTexturerWindow>().Repaint();

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            OnShellProcessComplete = null;
            TFToolsAssPP.OnFinishImport = null;
        }

        SerializedProperty hlExtractPath;
        SerializedProperty packagePath;
        SerializedProperty exportPath;
        SerializedProperty itemToExtract;

        SerializedProperty vtfInput;
        SerializedProperty vtfFolderInput;

        SerializedProperty renderer;
        Renderer localRenderer;
        GameObject localObject;
        SerializedProperty textureInputFolderPath;
        SerializedProperty textureOutputFolderPath;
        SerializedProperty generatedMaterialSavePath;

        SerializedProperty showHelpText;
        SerializedProperty helpTextSize;
        SerializedProperty unlockSystemObjects;

        SerializedProperty hlExtractExe;
        SerializedProperty vtfCmdExe;
        SerializedProperty referenceMaterial;
        SerializedProperty nonPaintedShader;
        SerializedProperty paintedShader;
        SerializedProperty normalMapTags;
        SerializedProperty specularMapTags;

        protected override void DesignateSerializedProperties()
        {
            hlExtractPath = SerializedObject.FindProperty(nameof(hlExtractPath));
            packagePath = SerializedObject.FindProperty(nameof(packagePath));
            exportPath = SerializedObject.FindProperty(nameof(exportPath));
            itemToExtract = SerializedObject.FindProperty(nameof(itemToExtract));

            vtfInput = SerializedObject.FindProperty(nameof(vtfInput));
            vtfFolderInput = SerializedObject.FindProperty(nameof(vtfFolderInput));

            renderer = SerializedObject.FindProperty(nameof(renderer));
            textureInputFolderPath = SerializedObject.FindProperty(nameof(textureInputFolderPath));
            textureOutputFolderPath = SerializedObject.FindProperty(nameof(textureOutputFolderPath));
            generatedMaterialSavePath = SerializedObject.FindProperty(nameof(generatedMaterialSavePath));

            helpTextSize = SerializedObject.FindProperty(nameof(helpTextSize));
            showHelpText = SerializedObject.FindProperty(nameof(showHelpText));
            unlockSystemObjects = SerializedObject.FindProperty(nameof(unlockSystemObjects));

            hlExtractExe = SerializedObject.FindProperty(nameof(hlExtractExe));
            vtfCmdExe = SerializedObject.FindProperty(nameof(vtfCmdExe));
            referenceMaterial = SerializedObject.FindProperty(nameof(referenceMaterial));
            nonPaintedShader = SerializedObject.FindProperty(nameof(nonPaintedShader));
            paintedShader = SerializedObject.FindProperty(nameof(paintedShader));
            normalMapTags = SerializedObject.FindProperty(nameof(normalMapTags));
            specularMapTags = SerializedObject.FindProperty(nameof(specularMapTags));
        }

        private void OnGUI()
        {
            scrollProgress = EditorGUILayout.BeginScrollView(new Vector2(scrollX, scrollY));

            if ((SerializedObject == null || ModelTexturerData == null) && Event.current.type == EventType.Repaint)
            {
                if (EditorUtility.DisplayDialog("Heads up!",
                    "This seems to be the first time you're launching this tool in this project. " +
                    "Please specify a safe location in your project folder to store important settings for "
                    + nameof(ModelTexturerWindow) + "!", "OK", "Abort"))
                {
                    string path = EditorHelper.OpenSmartSaveFileDialog(out prefData, "ModelTexturerData");
                    if (path != string.Empty)
                    {
                        preferencesPath = path;
                    }
                }
                else
                {
                    Window.Close();
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                SerializedObject.Update();

                GUIStyle titleStyle = 
                    new GUIStyle(EditorStyles.label)
                    .ApplyTextAnchor(TextAnchor.UpperCenter)
                    .ApplyBoldText()
                    .SetFontSize(14);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("== Model Texturer ==", titleStyle);
                EditorGUILayout.Space();
                RenderModelTexturerTool();

                EditorGUILayout.Space();

                if (Event.current.type == EventType.Used)
                {
                    if (SerializedObject.hasModifiedProperties) SerializedObject.ApplyModifiedProperties();
                    return;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("== Settings ==", titleStyle);
                EditorGUILayout.Space();
                RenderSettings();

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                using (new EditorGUI.DisabledScope(!unlockSystemObjects.boolValue))
                {
                    EditorGUILayout.LabelField("== System Objects ==", titleStyle);
                    EditorGUILayout.Space();

                    RenderSystemObjects();

                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndVertical();

                if (SerializedObject.hasModifiedProperties) SerializedObject.ApplyModifiedProperties();
            }
            if (queueRefresh)
            {
                AssetDatabase.Refresh();
                queueRefresh = false;
            }

            EditorGUILayout.EndScrollView();
        }

        void RenderHLExtractTools()
        {
            EditorGUILayout.BeginHorizontal();
            EditorHelper.SmartFolderField(new GUIContent("HLExtract.exe"), hlExtractPath, false);
            if (EditorHelper.CondensedButton("Browse"))
            {
                hlExtractPath.stringValue = EditorUtility.OpenFilePanel("Find HLExtract.exe", hlExtractPath.stringValue, "exe");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorHelper.SmartFolderField(new GUIContent("VPK Path"), packagePath, false);
            if (EditorHelper.CondensedButton("Browse"))
            {
                packagePath.stringValue = EditorUtility.OpenFilePanel("Select a .vpk Archive to Extract From", packagePath.stringValue, "vpk");
            }
            EditorGUILayout.EndHorizontal();

            EditorHelper.RenderSmartFolderProperty(new GUIContent("Export Path"), exportPath, false);

            EditorGUILayout.PropertyField(itemToExtract);

            EditorGUILayout.Space();

            if (GUILayout.Button("TEST"))
            {
                string filePath = Path.Combine(
                    hlExtractPath.stringValue.Remove(hlExtractPath.stringValue.IndexOf("HLExtract.exe")),
                    "run.bat");

                File.WriteAllText(filePath, string.Empty);
                StreamWriter writer = new StreamWriter(filePath, true);
                writer.WriteLine("\"" + hlExtractPath.stringValue + "\"" +
                " -p \"" + packagePath.stringValue +
                "\" -d \"" + exportPath.stringValue +
                "\" -e \"" + itemToExtract.stringValue + "\"");
                writer.Close();

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(filePath);
                //startInfo.CreateNoWindow = true;
                System.Diagnostics.Process.Start(startInfo);
            }
        }

        void RenderModelTexturerTool()
        {
            if (showHelpText.boolValue) EditorGUILayout.LabelField("Add a MeshRenderer/SkinnedMeshRenderer to apply textures to just that mesh", helpTextStyle);
            localRenderer = EditorGUILayout.ObjectField(new GUIContent("Model"), localRenderer, typeof(Renderer), true) as Renderer;

            EditorGUILayout.LabelField("- or -", new GUIStyle(EditorStyles.label).ApplyTextAnchor(TextAnchor.UpperCenter));
            if (showHelpText.boolValue) EditorGUILayout.LabelField("Add a GameObject to apply textures to all Renderers in that GameObject and it's children", helpTextStyle);

            localObject = EditorGUILayout.ObjectField(new GUIContent("Model Group"), localObject, typeof(GameObject), true) as GameObject;

            EditorGUILayout.Space();

            EditorHelper.RenderSmartFolderProperty(new GUIContent("VTF/VMT Inputs", "Also supports .png/.tga"), textureInputFolderPath, true, "Specify where to pull textures from");
            EditorHelper.RenderSmartFolderProperty(new GUIContent("Texture Outputs"), textureOutputFolderPath, true, "Specify where to save converted textures");
            
            EditorHelper.RenderSmartFolderProperty(new GUIContent("Material Outputs"), generatedMaterialSavePath, true, "Specify where to save generated materials");
            if (showHelpText.boolValue) EditorGUILayout.LabelField("Tip: If your model comes out strangely black, try adjusting the material offsets!", helpTextStyle);

            using (new EditorGUI.DisabledScope(localObject == null && localRenderer == null))
            {
                if (GUILayout.Button("Apply Textures to Model(s)"))
                {
                    if (!Directory.Exists(textureInputFolderPath.stringValue))
                    {
                        EditorUtility.DisplayDialog(
                            "Error: Missing Input Texture Folder",
                            "Directory " + textureInputFolderPath.stringValue + " does not exist! " +
                            "Check to make sure the folder is typed correctly.", "OK");
                        return;
                    }

                    string currentDirectory = System.Environment.CurrentDirectory;
                    string tifp = Path.Combine(currentDirectory, textureInputFolderPath.stringValue).Replace('/', '\\');
                    var files = Directory.GetFiles(tifp);

                    Dictionary<string, FileInfo> existingTextures = new Dictionary<string, FileInfo>();
                    List<FileInfo> existingVTFs = new List<FileInfo>();
                    bool vtfExists = false;
                    for (int i = 0; i < files.Length; i++)
                    {
                        FileInfo file = new FileInfo(files[i]);
                        if (file.Extension == ".vtf")
                        {
                            vtfExists = true;
                            existingVTFs.Add(file);
                        }
                        else if (file.Extension == ".png" || file.Extension == ".tga")
                        {
                            string name = file.Name.Remove(file.Name.IndexOf(file.Extension), file.Extension.Length);
                            existingTextures.Add(name, file);
                        }
                    }

                    bool willDeleteTextures = false;
                    string texturesToOverwrite =
                        "Existing textures found at input texture folder " +
                        textureInputFolderPath.stringValue +
                        " will be overwritten by this process! Affected textures: ";
                    List<string> texturesToDelete = new List<string>();
                    if (vtfExists)
                    {
                        for (int i = 0; i < existingVTFs.Count; i++)
                        {
                            string vtfName = existingVTFs[i].Name.Remove(existingVTFs[i].Name.IndexOf(existingVTFs[i].Extension), existingVTFs[i].Extension.Length);
                            if (existingTextures.ContainsKey(vtfName))
                            {
                                texturesToDelete.Add(existingTextures[vtfName].FullName.Remove(0, currentDirectory.Length + 1));
                                texturesToOverwrite += System.Environment.NewLine + existingTextures[vtfName].Name;
                                willDeleteTextures = true;
                            }
                        }
                    }

                    if (willDeleteTextures)
                    {
                        if (!EditorUtility.DisplayDialog(
                            "Warning!",
                            texturesToOverwrite, "Continue", "Cancel"))
                        {
                            return;
                        }
                    }

                    List<Renderer> allRenderers = new List<Renderer>();
                    if (localObject != null) allRenderers.AddRange(localObject.GetComponentsInChildren<Renderer>());
                    if (localRenderer != null) allRenderers.Add(localRenderer);
                    List<string> materialsToGen = new List<string>();
                    var existingMaterials = EditorHelper.ImportAssetsAtPath<Material>(generatedMaterialSavePath.stringValue);
                    for (int i = 0; i < allRenderers.Count; i++)
                    {
                        for (int j = 0; j < allRenderers[i].sharedMaterials.Length; j++)
                        {
                            var matName = allRenderers[i].sharedMaterials[j].name;
                            if (!materialsToGen.Contains(matName)) materialsToGen.Add(matName);
                        }
                    }

                    bool willOverwriteMats = false;
                    string matsToOverwrite =
                        "Existing materials found at generated material folder " +
                        generatedMaterialSavePath.stringValue +
                        " will be overwritten by this process! Affected materials: ";
                    for (int i = 0; i < existingMaterials.Count; i++)
                    {
                        if (materialsToGen.Contains(existingMaterials[i].name))
                        {
                            matsToOverwrite += System.Environment.NewLine + existingMaterials[i].name;
                            willOverwriteMats = true;
                        }
                    }

                    if (willOverwriteMats)
                    {
                        if (!EditorUtility.DisplayDialog(
                                "Warning!",
                                matsToOverwrite, "Continue", "Cancel"))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < texturesToDelete.Count; i++)
                    {
                        AssetDatabase.DeleteAsset(texturesToDelete[i]);
                    }

                    string vtfo = Path.Combine(
                        System.Environment.CurrentDirectory,
                        textureOutputFolderPath.stringValue.Replace('/', '\\'));

                    var textures = EditorHelper.ImportAssetsAtPath<Texture>(textureInputFolderPath.stringValue);

                    if (vtfExists)
                    {
                        OnShellProcessComplete += () =>
                        {
                            OnShellProcessComplete = null;

                            TFToolsAssPP.OnFinishImport += (Texture[] tex) =>
                            {
                                textures.AddRange(tex);
                                ApplyTexturesToModel(textures);
                                TFToolsAssPP.OnFinishImport = null;
                                queueRefresh = true;
                            };

                            queueRefresh = true;
                        };

                        string exePath = Path.Combine(
                            System.Environment.CurrentDirectory,
                            AssetDatabase.GetAssetPath(vtfCmdExe.objectReferenceValue));
                        exePath = exePath.Replace('/', '\\');

                        string filePath = Path.Combine(
                            exePath.Remove(exePath.IndexOf("VTFCmd.exe")),
                            "run.bat");

                        tifp = Path.Combine(tifp, "*.vtf");

                        File.WriteAllText(filePath, string.Empty);
                        StreamWriter writer = new StreamWriter(filePath, true);
                        writer.WriteLine("\"" + exePath + "\"" +
                            " -folder \"" + tifp + "\""/* +
                        " -output \"" + vtfo + "\""*/);
                        writer.Close();

                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(filePath);
                        startInfo.CreateNoWindow = true;
                        commandShell = new System.Diagnostics.Process();
                        commandShell.StartInfo = startInfo;
                        commandShell.Start();
                    }
                    else ApplyTexturesToModel(textures);
                }
            }
        }

        void ApplyTexturesToModel(List<Texture> tex)
        {
            Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
            for (int i = 0; i < tex.Count; i++)
            {
                if (!textures.ContainsKey(tex[i].name))
                {
                    textures.Add(tex[i].name, tex[i]);
                }
            }

            List<Renderer> allRenderers = new List<Renderer>();
            if (localObject) allRenderers.AddRange(localObject.GetComponentsInChildren<Renderer>());
            if (localRenderer) allRenderers.Add(localRenderer);

            Dictionary<string, Material> generatedMaterials = new Dictionary<string, Material>();
            List<string> problemMaterials = new List<string>();
            for (int x = 0; x < allRenderers.Count; x++)
            {
                SerializedObject meshObject = new SerializedObject(allRenderers[x]);
                SerializedProperty sharedMats = meshObject.FindProperty("m_Materials");
                string[] texKeys = new string[textures.Keys.Count];
                textures.Keys.CopyTo(texKeys, 0);
                for (int i = 0; i < sharedMats.arraySize; i++)
                {
                    var prop = sharedMats.GetArrayElementAtIndex(i);
                    var mat = prop.objectReferenceValue;

                    EditorUtility.DisplayProgressBar(
                        "Applying textures to " + allRenderers[x].name,
                        mat.name, (float)i / (float)sharedMats.arraySize);

                    string baseTextureName = string.Empty, normalName = string.Empty, specularName = string.Empty;
                    string vmtPath = Path.Combine(textureInputFolderPath.stringValue, mat.name + ".vmt");
                    bool hasVMT = false;
                    bool painted = false;
                    float btcob = -1;
                    Color baseColor = Color.clear;
                    if (File.Exists(vmtPath))
                    {
                        hasVMT = true;
                        Vmt vmt = new Vmt();
                        vmt.Load(vmtPath);
                        for (int j = 0; j < vmt.Root.ChildNodes.Count; j++)
                        {
                            var node = vmt.Root.ChildNodes[j];
                            string nodeName = node.Name.ToLower(); // Because Source isn't case-sensitive
                            if (nodeName == "$basetexture" || nodeName == "$iris")
                            {
                                baseTextureName = node.InnerText.Remove(0, node.InnerText.LastIndexOf('/') + 1);
                            }
                            else if (nodeName == "$bumpmap")
                            {
                                normalName = node.InnerText.Remove(0, node.InnerText.LastIndexOf('/') + 1);
                            }
                            else if (nodeName == "$phongexponenttexture")
                            {
                                specularName = node.InnerText.Remove(0, node.InnerText.LastIndexOf('/') + 1);
                            }
                            else if (nodeName == "$blendtintcoloroverbase")
                            {
                                painted = true;
                                btcob = float.Parse(node.InnerText.Remove(0, nodeName.Length));
                            }
                            else if (nodeName == "$colortint_base")
                            {
                                string colorText = node.InnerText.Remove(0, node.InnerText.IndexOf('{') + 1);
                                while (colorText[0] == ' ') colorText = colorText.Remove(0, 1); // Damn spaces
                                colorText = colorText.Remove(colorText.IndexOf('}'));
                                string[] textArray = colorText.Split(new char[] { ' ' });
                                Color32 color = new Color32(
                                    byte.Parse(textArray[0]),
                                    byte.Parse(textArray[1]),
                                    byte.Parse(textArray[2]),
                                    255
                                    );
                                baseColor = color;
                                painted = true;
                            }
                        }
                    }

                    // If material exists, use it!
                    string matPath = Path.Combine(generatedMaterialSavePath.stringValue, mat.name + ".mat");
                    Material newMat = null;
                    int normalID = Shader.PropertyToID("_BumpMap");
                    int specularID = Shader.PropertyToID("_SpecularTex");
                    int btcobID = Shader.PropertyToID("_BlendTintColorOverBase");
                    int paintID = Shader.PropertyToID("_Paint");
                    if (!generatedMaterials.ContainsKey(mat.name))
                    {
                        newMat = new Material(referenceMaterial.objectReferenceValue as Material);
                        newMat.name = mat.name;
                        newMat.shader = painted ? paintedShader.objectReferenceValue as Shader : nonPaintedShader.objectReferenceValue as Shader;

                        if (hasVMT)
                        {
                            if (painted)
                            {
                                if (baseColor.a != 0) newMat.SetColor(paintID, baseColor);
                                if (btcob != -1) newMat.SetFloat(btcobID, btcob);
                            }

                            if (textures.ContainsKey(normalName))
                            {
                                newMat.mainTexture = textures[normalName];
                            }

                            if (textures.ContainsKey(specularName))
                            {
                                newMat.mainTexture = textures[specularName];
                            }

                            if (textures.ContainsKey(baseTextureName))
                            {
                                newMat.mainTexture = textures[baseTextureName];
                            }
                            else if (baseTextureName.ToLower().Contains("color")) // Oh no its one of those
                            {
                                for (int j = 0; j < texKeys.Length; j++)
                                {
                                    bool skip = false;
                                    for (int l = 0; l < specularMapTags.arraySize; l++)
                                    {
                                        if (texKeys[j].Contains(specularMapTags.GetArrayElementAtIndex(l).stringValue))
                                        {
                                            skip = true;
                                            break;
                                        }
                                    }

                                    if (skip) continue;
                                    for (int l = 0; l < normalMapTags.arraySize; l++)
                                    {
                                        if (texKeys[j].Contains(normalMapTags.GetArrayElementAtIndex(l).stringValue))
                                        {
                                            skip = true;
                                            break;
                                        }
                                    }

                                    if (skip) continue;
                                    if (texKeys[j].Contains(mat.name))
                                    {
                                        newMat.mainTexture = textures[texKeys[j]];
                                    }
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog(
                                    "Missing Texture Warning!",
                                    "No main texture found for material " + newMat.name, "OK");
                                problemMaterials.Add(newMat.name);
                            }
                        }
                        else
                        {
                            List<Texture> foundTextures = new List<Texture>();
                            for (int j = 0; j < texKeys.Length; j++)
                            {
                                if (texKeys[j].Contains(mat.name))
                                {
                                    foundTextures.Add(textures[texKeys[j]]);
                                }
                            }

                            bool baseFound = false, normalFound = false, specularFound = false;
                            for (int j = 0; j < foundTextures.Count; j++)
                            {
                                bool textureFound = false;
                                for (int l = 0; l < normalMapTags.arraySize && !normalFound; l++)
                                {
                                    if (foundTextures[j].name.Contains(normalMapTags.GetArrayElementAtIndex(l).stringValue))
                                    {
                                        newMat.SetTexture(normalID, foundTextures[j]);
                                        foundTextures.RemoveAt(j);
                                        normalFound = true;
                                        textureFound = true;
                                        break;
                                    }
                                }

                                if (textureFound) continue;
                                for (int l = 0; l < specularMapTags.arraySize && !specularFound; l++)
                                {
                                    if (foundTextures[j].name.Contains(specularMapTags.GetArrayElementAtIndex(l).stringValue))
                                    {
                                        newMat.SetTexture(specularID, foundTextures[j]);
                                        foundTextures.RemoveAt(j);
                                        specularFound = true;
                                        textureFound = true;
                                        break;
                                    }
                                }

                                if (textureFound) continue;
                                if (!baseFound) // Assume the names are identical
                                {
                                    newMat.mainTexture = foundTextures[j];
                                    baseFound = true;
                                }
                            }

                            if (!baseFound)
                            {
                                EditorUtility.DisplayDialog(
                                    "Missing Texture Warning!",
                                    "No main texture found for material " + newMat.name, "OK");
                                problemMaterials.Add(newMat.name);
                            }
                        }

                        if (AssetDatabase.LoadAssetAtPath<Material>(matPath))
                        {
                            AssetDatabase.DeleteAsset(matPath);
                        }
                        AssetDatabase.CreateAsset(newMat, matPath);
                        generatedMaterials.Add(newMat.name, newMat);
                    }
                    else
                    {
                        newMat = generatedMaterials[mat.name];
                    }

                    prop.objectReferenceValue = newMat;
                }

                meshObject.ApplyModifiedProperties();
            }

            if (problemMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog("Finished!", "Textures applied successfully!", "OK");
            }
            else
            {
                string missingMatText = "Missing textures found in materials:";
                for (int i = 0; i < problemMaterials.Count; i++) missingMatText += System.Environment.NewLine + problemMaterials[i];
                EditorUtility.DisplayDialog(
                    "Finished with " + problemMaterials.Count + " warnings",
                    missingMatText, "OK");
            }

            EditorUtility.ClearProgressBar();
        }

        void RenderSettings()
        {
            EditorGUILayout.PropertyField(showHelpText);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(helpTextSize, new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
            if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                helpTextSize.intValue--;
            }
            else if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                helpTextSize.intValue++;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(unlockSystemObjects);
        }

        void RenderSystemObjects()
        {
            if (unlockSystemObjects.boolValue && showHelpText.boolValue)
            {
                EditorGUILayout.LabelField("Don't touch these files if you don't know what they do. " +
                    "Worst case scenario, you will have to re-import the package.", helpTextStyle.ApplyBoldText());
            }

            EditorGUILayout.PropertyField(hlExtractExe, new GUIContent("HLExtract.exe"));
            EditorGUILayout.PropertyField(vtfCmdExe, new GUIContent("VTFCmd.exe"));
            EditorGUILayout.PropertyField(referenceMaterial, new GUIContent("Reference Material", "The base material all generated materials will use."));
            EditorGUILayout.PropertyField(nonPaintedShader, new GUIContent("TF2 Shader"));
            EditorGUILayout.PropertyField(paintedShader, new GUIContent("TF2 Paint Shader"));
            EditorGUILayout.PropertyField(normalMapTags);
            EditorGUILayout.PropertyField(specularMapTags);
        }

        private void OnInspectorUpdate()
        {
            if (commandShell != null)
            {
                if (commandShell.HasExited)
                {
                    commandShell = null;
                    OnShellProcessComplete?.Invoke();
                }
            }
        }
    }

    // haha
    class TFToolsAssPP : AssetPostprocessor
    {
        public static System.Action<Texture[]> OnFinishImport;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            Texture[] loadedTextures = new Texture[importedAssets.Length];
            for (int i = 0; i < importedAssets.Length; i++)
            {
                loadedTextures[i] = AssetDatabase.LoadAssetAtPath<Texture>(importedAssets[i]);
            }
            OnFinishImport?.Invoke(loadedTextures);
        }
    }
}
