using Ibasa.Valve.Vmt;
using JackysEditorHelpers;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TF2Ls
{
    [CustomEditor(typeof(ModelTexturerData))]
    public class ModelTexturerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(
                "You can also open up the Model Texturer by mousing over the top Toolbar and " +
                "navigating to Tools -> TF2Ls for Unity -> Model Texturer"
                , TF2LsStyles.HelpTextStyle);

            if (GUILayout.Button("Open Model Texturer"))
            {
                if (ModelTexturerWindow.IsOpen) EditorWindow.FocusWindowIfItsOpen<ModelTexturerWindow>();
                else ModelTexturerWindow.Init();
            }
        }
    }

    public class ModelTexturerWindow : TFToolsSerializedEditorWindow<ModelTexturerData, ModelTexturerWindow>
    {
        #region Preferences
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

        const string SCROLL_X_KEY = "TFTOOLS_MODELTEXTURER_WINDOWSCROLL_X";
        static float scrollX
        {
            get { return EditorPrefs.GetFloat(SCROLL_X_KEY); }
            set { EditorPrefs.SetFloat(SCROLL_X_KEY, value); }
        }

        const string SCROLL_Y_KEY = "TFTOOLS_MODELTEXTURER_WINDOWSCROLL_Y";
        static float scrollY
        {
            get { return EditorPrefs.GetFloat(SCROLL_Y_KEY); }
            set { EditorPrefs.SetFloat(SCROLL_Y_KEY, value); }
        }

        const string SHOW_IMPORTOPTIONS = "TFTOOLS_MODELTEXTURER_SHOW_IMPORTOPTIONS";
        static bool showImportOptions
        {
            get { return EditorPrefs.GetBool(SHOW_IMPORTOPTIONS); }
            set { EditorPrefs.SetBool(SHOW_IMPORTOPTIONS, value); }
        }
        #endregion

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
                        var guids = AssetDatabase.FindAssets("t:" + nameof(ModelTexturerData));
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
                if (serializedObject == null && ModelTexturerData != null)
                {
                    serializedObject = new SerializedObject(ModelTexturerData);
                }
                return serializedObject;
            }
        }

        static Vector2 scrollProgress
        {
            get { return new Vector2(scrollX, scrollY); }
            set { scrollX = value.x; scrollY = value.y; }
        }

        string CurrentDirectory { get { return System.Environment.CurrentDirectory; } }

        const int MAX_ARGS_LENGTH = 16384;
        const int MAX_WARNING_LENGTH = 10;

        bool skipWarnings = false;

        public static List<ItemData> Items = new List<ItemData>();
        public static ItemData ActiveItem;
        public static Dictionary<string, string> EnglishDictionary = new Dictionary<string, string>();
        static string PLACEHOLDER_GRAPHIC_PATH =>
            Path.Combine(TF2LsEditorSettings.Settings.PackagePath, "ModelTexturer", "outback-hat.png");
        public static Texture2D PlaceHolderGraphic;

        public const string VMT_VPK_FILENAME = "tf2_misc_dir.vpk";
        public const string VTF_VPK_FILENAME = "tf2_textures_dir.vpk";
        List<string> vmtsToImport = new List<string>();

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            var loadedAsset = AssetDatabase.LoadAssetAtPath<ModelTexturerData>(assetPath);
            if (loadedAsset)
            {
                Init();
                return true;
            }
            return false;
        }

        [MenuItem(TF2LsConstants.Paths.MODEL_TEXTURER, false, 1)]
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
            if (SerializedObject != null) DesignateSerializedProperties();

            PlaceHolderGraphic = AssetDatabase.LoadAssetAtPath<Texture2D>(PLACEHOLDER_GRAPHIC_PATH);

            Undo.undoRedoPerformed += OnUndoRedo;
            TFToolsAssPP.OnTexturesImported = null;

            if (!TF2LsEditorSettings.Settings.TFInstallExists)
            {
                if (EditorUtility.DisplayDialog("Missing TF2 Install!", "Please set your TF2 install " +
                    "location before using this tool!", "OK", "No, later"))
                {
                    TF2LsSettingsProvider.Init();
                }
                //Window.Close();
                //GUIUtility.ExitGUI();
            }
        }

        private void OnUndoRedo() => GetWindow<ModelTexturerWindow>().Repaint();

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            TFToolsAssPP.OnTexturesImported = null;
        }

        #region SerializedProperties & Variables
        Renderer localRenderer;
        GameObject localObject;

        SerializedProperty modelType;
        SerializedProperty characterClass;
        SerializedProperty team;

        SerializedProperty searchTF2Install;

        SerializedProperty vmtPath;
        SerializedProperty vtfPath;
        SerializedProperty textureOutputFolderPath;
        SerializedProperty generatedMaterialSavePath;

        SerializedProperty overrideAsset;

        SerializedProperty showHelpText;

        protected override void DesignateSerializedProperties()
        {
            modelType = FindProp(nameof(modelType));
            characterClass = FindProp(nameof(characterClass));
            team = FindProp(nameof(team));

            overrideAsset = FindProp(nameof(overrideAsset));
            FindOverrideAssetProperties();

            searchTF2Install = FindProp(nameof(searchTF2Install));

            vmtPath = FindProp(nameof(vmtPath));
            vtfPath = FindProp(nameof(vtfPath));

            textureOutputFolderPath = FindProp(nameof(textureOutputFolderPath));
            generatedMaterialSavePath = FindProp(nameof(generatedMaterialSavePath));

            showHelpText = FindProp(nameof(showHelpText));
        }
        #endregion

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Model Texturer", TF2LsStyles.CenteredTitle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Automatically texture models ripped from Valve games",
                TF2LsStyles.CenteredLabel);

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
                SerializedObject.UpdateIfRequiredOrScript();

                // Target Models
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("1. Assign Meshes", TF2LsStyles.CenteredBoldHeader);
                EditorGUILayout.Space();
                if (showHelpText.boolValue) EditorGUILayout.LabelField("Add a MeshRenderer/SkinnedMeshRenderer to apply textures to just that mesh", TF2LsStyles.HelpTextStyle);
                localRenderer = EditorGUILayout.ObjectField(new GUIContent("Model"), localRenderer, typeof(Renderer), true) as Renderer;

                EditorGUILayout.LabelField("- or -", new GUIStyle(EditorStyles.label).ApplyTextAnchor(TextAnchor.UpperCenter));
                if (showHelpText.boolValue) EditorGUILayout.LabelField("Add a GameObject to apply textures to all Renderers in that GameObject and it's children", TF2LsStyles.HelpTextStyle);

                localObject = EditorGUILayout.ObjectField(new GUIContent("Model Group"), localObject, typeof(GameObject), true) as GameObject;
                EditorGUILayout.EndVertical();

                // Model Texturer
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("2. Set Up Reference Model", TF2LsStyles.CenteredBoldHeader);
                EditorGUILayout.Space();
                RenderHLExtractTools();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("3. Export Paths", TF2LsStyles.CenteredBoldHeader);
                EditorGUILayout.Space();
                RenderExportPaths();
                EditorGUILayout.EndVertical();

                if (Event.current.type == EventType.Used)
                {
                    SerializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                // VMT Overrides
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("VMT Import Options", TF2LsStyles.CenteredBoldHeader);
                EditorGUILayout.Space();
                RenderOverrideProperties();

                EditorGUILayout.EndVertical();

                // Settings
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Settings", TF2LsStyles.CenteredBoldHeader);
                EditorGUILayout.Space();
                RenderSettings();

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                SerializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
        }

        void RenderHLExtractTools()
        {
            EditorGUILayout.PropertyField(modelType);
            if (modelType.enumValueIndex != (int)ModelTexturerData.ModelType.Map)
            {
                if (modelType.enumValueIndex != (int)ModelTexturerData.ModelType.Item)
                    EditorGUILayout.PropertyField(characterClass);
                EditorGUILayout.PropertyField(team);
            }

            if (modelType.enumValueIndex == 1)
            {
                if (GUILayout.Button("Select Item", GUILayout.ExpandWidth(false)))
                {
                    ItemViewer.InitUtility();
                }

                //if (showHelpText.boolValue) EditorGUILayout.LabelField("Sorry, weapon and cosmetic importing is " +
                //    "currently not ready to use.", TF2LsStyles.HelpTextStyle);

                var rect = EditorGUILayout.GetControlRect(true, 128);
                var graphic = new GUIContent(ActiveItem == null ? PlaceHolderGraphic : ActiveItem.loadedImage);
                GUI.Box(rect, new GUIContent(), EditorStyles.helpBox.ApplyTextAnchor(TextAnchor.LowerCenter));
                var imageRect = new Rect(rect);
                imageRect.x += rect.width / 2 - 64;
                imageRect.y += rect.height / 2 - 64 + 10;
                GUI.Box(imageRect, graphic, new GUIStyle());

                rect.y += 5;
                EditorGUI.LabelField(rect, ActiveItem == null ? "None" : ActiveItem.name, TF2LsStyles.CenteredBoldHeader);

                return;
            }
        }

        void RenderExportPaths()
        {
            EditorGUILayout.PropertyField(searchTF2Install);

            if (showHelpText.boolValue) EditorGUILayout.LabelField("Choose folders for your model's " +
                "materials and textures to go. You are recommended to make dedicated folders for " +
                "characters/weapons/cosmetics and to share folders for maps.", TF2LsStyles.HelpTextStyle);

            EditorHelper.RenderSmartFolderProperty(new GUIContent("VMT Path"), vmtPath, true, "Specify the location for VMT files");
            EditorHelper.RenderSmartFolderProperty(new GUIContent("VTF Path"), vtfPath, true, "Specify the location for VTF files");
            EditorHelper.RenderSmartFolderProperty(new GUIContent("Texture Output"), textureOutputFolderPath, true, "Specify where to save converted textures");

            EditorHelper.RenderSmartFolderProperty(new GUIContent("Material Output"), generatedMaterialSavePath, true, "Specify where to save generated materials");
            if (showHelpText.boolValue) EditorGUILayout.LabelField("Tip: If your model comes out strangely black, try adjusting the material offsets!", TF2LsStyles.HelpTextStyle);

            bool cantRun = !AssetDatabase.IsValidFolder(vmtPath.stringValue) ||
                            !AssetDatabase.IsValidFolder(vtfPath.stringValue) ||
                            !AssetDatabase.IsValidFolder(textureOutputFolderPath.stringValue) ||
                            !AssetDatabase.IsValidFolder(generatedMaterialSavePath.stringValue);

            if (cantRun)
                EditorGUILayout.HelpBox("One or more of the above folder paths are invalid!", MessageType.Error);

            using (new EditorGUI.DisabledScope(cantRun))
            {
                if (GUILayout.Button("Apply Textures to Model"))
                {
                    if (searchTF2Install.boolValue) ExtractVMTsFromMeshes();
                    else
                    {
                        LoadVMTs();
                        ConvertVTFsToTextures();
                    }
                }
            }
        }

        void ExtractVMTsFromMeshes()
        {
            string vmtFolder = Path.Combine(new string[] { "root", "materials" });

            if (localObject) allRenderers.AddRange(localObject.GetComponentsInChildren<Renderer>());
            if (localRenderer) allRenderers.Add(localRenderer);

            List<string> materialPaths = new List<string>();
            vmtsToImport = new List<string>();

            System.Action<Material> processMaterialPath = null;

            switch ((ModelTexturerData.ModelType)modelType.enumValueIndex)
            {
                case ModelTexturerData.ModelType.Character:
                    var className = ((CharacterClass)characterClass.enumValueIndex).ToLowerString();
                    vmtFolder = Path.Combine(vmtFolder, "models", "player", className);

                    processMaterialPath = (sharedMat) =>
                    {
                        string name = sharedMat.name + ".vmt";
                        Team teamEnum = (Team)team.enumValueIndex;
                        if (sharedMat.name.Contains("_red"))
                        {
                            if (teamEnum == Team.BLU)
                            {
                                name = name.Replace("_red", "_blue");
                            }
                        }

                        if (!vmtsToImport.Contains(name))
                        {
                            vmtsToImport.Add(name);
                            materialPaths.Add(Path.Combine(vmtFolder, name));
                        }
                    };
                    break;
                case ModelTexturerData.ModelType.Item:
                    vmtFolder = Path.Combine(vmtFolder, "models");
                    processMaterialPath = (sharedMat) =>
                    {
                        string name = sharedMat.name;
                        string bluName = "";
                        Team teamEnum = (Team)team.enumValueIndex;
                        if (sharedMat.name.Contains("_red"))
                        {
                            if (teamEnum == Team.BLU)
                            {
                                name = name.Replace("_red", "_blue");
                                bluName = name.Replace("_blue", "blu") + ".vmt"; // Why so inconsistent???
                            }
                        }
                        name += ".vmt";

                        if (!vmtsToImport.Contains(name))
                        {
                            vmtsToImport.Add(name);
                            // There are a lot of potential paths for weapons
                            string defaultPath = Path.Combine(vmtFolder, "weapons", "c_items");
                            defaultPath = Path.Combine(defaultPath, name);
                            materialPaths.Add(defaultPath);
                            if (teamEnum == Team.BLU) materialPaths.Add(Path.Combine(vmtFolder, Path.Combine(defaultPath, bluName)));

                            string workshopPath = Path.Combine(vmtFolder, "workshop", "weapons", "c_models");
                            workshopPath = Path.Combine(workshopPath, sharedMat.name);
                            materialPaths.Add(Path.Combine(workshopPath, name));
                            if (teamEnum == Team.BLU) materialPaths.Add(Path.Combine(workshopPath, bluName));
                        }
                    };
                    break;
                case ModelTexturerData.ModelType.Map:
                    processMaterialPath = (sharedMat) =>
                    {
                        string name = sharedMat.name + ".vmt";

                        // Why does Valve do this???
                        if (name.Contains("materials")) name = name.Remove(0, 10);

                        // We don't know if an underscore is a directory or a legit file/folder name
                        // Therefore we add every variation and ignore the missing file check
                        if (!vmtsToImport.Contains(name))
                        {
                            vmtsToImport.Add(name);
                            var underScores = System.Array.FindAll(name.ToCharArray(), e => e == '_');

                            if (underScores.Length > 0)
                            {
                                string[] splits = name.Split('_');

                                for (int c = 0; c < Mathf.Pow(2, underScores.Length) && underScores.Length > 0; c++)
                                {
                                    string binary = System.Convert.ToString(c, 2);
                                    binary = new string('0', underScores.Length - binary.Length) + binary;

                                    var charB = binary.ToCharArray();

                                    string newName = splits[0];

                                    for (int j = 0; j < binary.Length; j++)
                                    {
                                        if (charB[j] == '0') // Underscore
                                        {
                                            newName += '_';
                                        }
                                        else // Slash
                                        {
                                            newName += '/';
                                        }
                                        newName += splits[j + 1];
                                    }

                                    materialPaths.Add(Path.Combine(vmtFolder, newName));
                                }
                            }
                            else
                            {
                                materialPaths.Add(Path.Combine(vmtFolder, name));
                            }
                        }
                    };
                    break;
            }

            foreach (var r in allRenderers)
            {
                foreach (var sharedMat in r.sharedMaterials)
                {
                    if (sharedMat == null)
                    {
                        if (EditorUtility.DisplayDialog("Missing Material Warning!",
                            "Renderer " + r.gameObject.name + " is missing a material!",
                            "Ignore", "Abort")) continue;
                        else return;
                    }
                    processMaterialPath.Invoke(sharedMat);
                }
            }

            string hlExtractPath = TF2LsEditorSettings.Settings.HLExtractPath;

            string packagePath = Path.Combine(TF2LsEditorSettings.Settings.TFInstallPath, VMT_VPK_FILENAME);

            string vmtExportPathCombined = Path.Combine(CurrentDirectory, vmtPath.stringValue);

            int i = 0;
            while (i < materialPaths.Count)
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(hlExtractPath);
                startInfo.Arguments =
                    "-p \"" + packagePath + "\" " +
                    "-d \"" + vmtExportPathCombined + "\" ";

                for (; i < materialPaths.Count && startInfo.Arguments.Length < MAX_ARGS_LENGTH; i++)
                {
                    startInfo.Arguments += "-e \"" + materialPaths[i] + "\" ";
                }

                var commandShell = new System.Diagnostics.Process();
                commandShell.StartInfo = startInfo;
                commandShell.Start();

                commandShell.WaitForExit();
            }

            var files = Directory.GetFiles(vmtPath.stringValue);
            var fileNames = new List<string>();

            if ((ModelTexturerData.ModelType)modelType.enumValueIndex != ModelTexturerData.ModelType.Map)
            {
                foreach (var f in files)
                {
                    var newFile = new FileInfo(f);
                    if (newFile.Extension == ".vmt")
                    {
                        fileNames.Add(newFile.Name);
                    }
                }

                List<string> missingFiles = new List<string>();
                foreach (var f in vmtsToImport)
                {
                    if (!fileNames.Contains(f)) missingFiles.Add(f);
                }

                if (missingFiles.Count > 0)
                {
                    string message = "Some VMTs failed to be extracted;";
                    foreach (var f in missingFiles)
                    {
                        message += System.Environment.NewLine + f;
                    }
                    EditorUtility.DisplayDialog("Extraction Failure Warning!", message, "OK");
                }
            }

            ExtractVTFsFromVMTs();
        }

        // Full of property dictionaries organized by their in-shader IDs
        struct ValveAssetTable
        {
            public Shader shader;
            public Dictionary<string, string> textures;
            public Dictionary<string, float> floats;
            public Dictionary<string, Color> colours;
        }

        /// <summary>
        /// Organized by VMT name to their imported properties
        /// </summary>
        static Dictionary<string, ValveAssetTable> valveAssetTables;
        /// <summary>
        /// VMT property name to the type of property they should be converted to
        /// </summary>
        static Dictionary<string, VMTPropOverrides.PropertyOverride> propertyOverrideLookup;
        static Dictionary<string, Shader> shaderOverrideLookup;

        List<string> vtfPaths = new List<string>();

        void LoadVMTs()
        {
            if (!Directory.Exists(vtfPath.stringValue))
            {
                EditorUtility.DisplayDialog(
                    "Error: Missing Input Texture Folder",
                    "Directory " + vtfPath.stringValue + " does not exist! " +
                    "Check to make sure the folder is typed correctly.", "OK");
                return;
            }

            valveAssetTables = new Dictionary<string, ValveAssetTable>();

            shaderOverrideLookup = new Dictionary<string, Shader>();
            foreach (var s in ModelTexturerData.overrideAsset.shaderOverrides)
            {
                if (!shaderOverrideLookup.ContainsKey(s.tag))
                {
                    shaderOverrideLookup.Add(s.tag.ToLower(), s.shader);
                }
            }

            propertyOverrideLookup = new Dictionary<string, VMTPropOverrides.PropertyOverride>();
            foreach (var p in ModelTexturerData.overrideAsset.propertyOverrides)
            {
                if (!propertyOverrideLookup.ContainsKey(p.tag))
                {
                    propertyOverrideLookup.Add(p.tag.ToLower(), p);
                }
            }

            vtfPaths = new List<string>();
            string tifp = Path.Combine(CurrentDirectory, vmtPath.stringValue);
            var files = Directory.GetFiles(tifp, "*.vmt");
            List<Vmt> vmts = new List<Vmt>();

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = new FileInfo(files[i]);
                string name = file.Name.Split('.')[0];
                Vmt vmt = new Vmt();
                vmt.Load(files[i]);
                vmts.Add(vmt);
                ValveAssetTable vat = new ValveAssetTable()
                {
                    colours = new Dictionary<string, Color>(),
                    textures = new Dictionary<string, string>(),
                    floats = new Dictionary<string, float>()
                };

                for (int j = 0; j < vmts[i].Root.ChildNodes.Count; j++)
                {
                    var node = vmts[i].Root.ChildNodes[j];
                    string nodeName = node.Name.ToLower(); // Because Source isn't case-sensitive

                    if (shaderOverrideLookup.ContainsKey(nodeName))
                    {
                        vat.shader = shaderOverrideLookup[nodeName];
                    }
                    else if (propertyOverrideLookup.ContainsKey(nodeName))
                    {
                        switch (propertyOverrideLookup[nodeName].propertyType)
                        {
                            case VMTPropOverrides.PropertyType.Texture:
                                string path = node.InnerText.Remove(0, nodeName.Length + 1).ToLower();
                                path = path.Replace('/', '\\');
                                if (!path.Contains(".vtf")) path += ".vtf";
                                path = Path.Combine("root", "materials", path);
                                vtfPaths.Add(path);
                                string textureName = path.Substring(path.LastIndexOf('\\') + 1).Split('.')[0];
                                vat.textures.Add(nodeName, textureName);
                                break;
                            case VMTPropOverrides.PropertyType.Float:
                                float value = float.Parse(node.InnerText.Remove(0, nodeName.Length));
                                vat.floats[nodeName] = value;
                                break;
                            case VMTPropOverrides.PropertyType.Colour:
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
                                vat.colours[nodeName] = color;
                                break;
                        }
                    }
                }
                valveAssetTables.Add(name, vat);
            }
        }

        void ExtractVTFsFromVMTs()
        {
            LoadVMTs();

            string hlExtractPath = TF2LsEditorSettings.Settings.HLExtractPath;
            string packagePath = Path.Combine(TF2LsEditorSettings.Settings.TFInstallPath, VTF_VPK_FILENAME);
            string vtfFullPath = Path.Combine(CurrentDirectory, vtfPath.stringValue);

            int k = 0;
            while (k < vtfPaths.Count)
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(hlExtractPath);
                startInfo.Arguments =
                    "-p \"" + packagePath + "\" " +
                    "-d \"" + vtfFullPath + "\" ";

                for (; k < vtfPaths.Count && startInfo.Arguments.Length < MAX_ARGS_LENGTH; k++)
                {
                    startInfo.Arguments += "-e \"" + vtfPaths[k] + "\" ";
                }

                var commandShell = new System.Diagnostics.Process();
                commandShell.StartInfo = startInfo;
                commandShell.Start();

                commandShell.WaitForExit();
            }
            ConvertVTFsToTextures();
        }

        List<Material> existingMaterials;
        List<Renderer> allRenderers = new List<Renderer>();
        List<string> materialsToGenerate;
        bool skipExistingMaterials;

        void ConvertVTFsToTextures()
        {
            skipWarnings = false;

            string tifp = Path.Combine(CurrentDirectory, vtfPath.stringValue).Replace('/', '\\');

            // Check if there are any existing TGAs, VTFCMD can't overwrite TGAs
            Dictionary<string, FileInfo> existingTextures = new Dictionary<string, FileInfo>();
            var tofp = Path.Combine(CurrentDirectory, textureOutputFolderPath.stringValue).Replace('/', '\\'); ;
            var files = Directory.GetFiles(tofp, "*.tga");
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = new FileInfo(files[i]);
                string name = file.Name.Split('.')[0];
                existingTextures.Add(name, file);
            }

            Dictionary<string, FileInfo> inputTextures = new Dictionary<string, FileInfo>();
            files = Directory.GetFiles(tifp, "*.vtf");
            List<FileInfo> vtfs = new List<FileInfo>();
            List<string> vtfsToIgnore = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = new FileInfo(files[i]);
                string name = file.Name.Split('.')[0];

                // Skip if exists
                if (existingTextures.ContainsKey(name))
                {
                    vtfsToIgnore.Add(name);
                    continue;
                }

                vtfs.Add(file);
            }

            string texturesToIgnore =
                "Existing textures found at output folder " +
                textureOutputFolderPath.stringValue +
                ". Corresponding VMTs will not be processed. Affected materials: ";
            for (int i = 0; i < Mathf.Clamp(vtfsToIgnore.Count, 0, MAX_WARNING_LENGTH); i++)
            {
                texturesToIgnore += System.Environment.NewLine + vtfsToIgnore[i];
            }

            if (existingTextures.Count >= MAX_WARNING_LENGTH)
            {
                texturesToIgnore += System.Environment.NewLine + "and " + (existingTextures.Count - MAX_WARNING_LENGTH).ToString() + " more!";
            }

            if (vtfsToIgnore.Count > 0)
            {
                if (!EditorUtility.DisplayDialog(
                    "Texture Conflict Warning!",
                    texturesToIgnore, "Continue", "Cancel")) return;
            }

            materialsToGenerate = new List<string>();
            existingMaterials = EditorHelper.ImportAssetsAtPath<Material>(generatedMaterialSavePath.stringValue);
            skipExistingMaterials = false;
            for (int i = 0; i < allRenderers.Count; i++)
            {
                for (int j = 0; j < allRenderers[i].sharedMaterials.Length; j++)
                {
                    var matName = allRenderers[i].sharedMaterials[j].name;

                    if ((ModelTexturerData.ModelType)modelType.enumValueIndex != ModelTexturerData.ModelType.Map)
                    {
                        if ((Team)team.enumValueIndex == Team.BLU) matName = matName.Replace("_red", "_blue");
                    }

                    if (!materialsToGenerate.Contains(matName)) materialsToGenerate.Add(matName);
                }
            }

            string matsToOverwrite =
                "Existing materials found at generated material folder " +
                generatedMaterialSavePath.stringValue +
                "! You can either ignore these materials or overwrite them. Affected materials: ";

            for (int i = 0; i < Mathf.Clamp(existingMaterials.Count, 0, MAX_WARNING_LENGTH); i++)
            {
                if (materialsToGenerate.Contains(existingMaterials[i].name))
                {
                    matsToOverwrite += System.Environment.NewLine + existingMaterials[i].name;
                }
            }

            if (existingMaterials.Count >= MAX_WARNING_LENGTH)
            {
                matsToOverwrite += System.Environment.NewLine + "and " + (existingMaterials.Count - MAX_WARNING_LENGTH).ToString() + " more!";
            }

            if (existingMaterials.Count > 0)
            {
                skipExistingMaterials = EditorUtility.DisplayDialog(
                        "Material Overwrite Warning!",
                        matsToOverwrite, "Ignore All", "Overwrite");
            }

            // Import all existing Textures
            var textures = EditorHelper.ImportAssetsAtPath<Texture2D>(textureOutputFolderPath.stringValue);

            {
                string vtfCmdPath = TF2LsEditorSettings.Settings.VTFCmdPath;
                int i = 0;
                while (i < vtfs.Count)
                {
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(vtfCmdPath);
                    startInfo.Arguments =
                    "-output \"" + tofp + "\" ";

                    for (; i < vtfs.Count && startInfo.Arguments.Length < MAX_ARGS_LENGTH; i++)
                    {
                        startInfo.Arguments += "-file \"" + vtfs[i].FullName + "\" ";
                    }

                    var commandShell = new System.Diagnostics.Process();
                    commandShell.StartInfo = startInfo;

                    commandShell.Start();

                    commandShell.WaitForExit();
                }
            }

            if (vtfs.Count > 0)
            {
                TFToolsAssPP.OnTexturesImported += (Texture2D[] tex) =>
                {
                    TFToolsAssPP.OnTexturesImported = null;
                    textures.AddRange(tex);
                    ApplyTexturesToModel(textures);
                };

                AssetDatabase.Refresh();
            }
            else ApplyTexturesToModel(textures);
        }

        void ApplyTexturesToModel(List<Texture2D> newTextures)
        {
            // Dictionary of texture names to themselves
            Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

            newTextures = EditorHelper.ImportAssetsAtPath<Texture2D>(textureOutputFolderPath.stringValue);

            for (int i = 0; i < newTextures.Count; i++)
            {
                textures.Add(newTextures[i].name, newTextures[i]);
            }

            Dictionary<string, Material> generatedMaterials = new Dictionary<string, Material>();
            List<string> problemMaterials = new List<string>();

            // Create reverse lookup table to match texture file names to their VMT
            Dictionary<string, string> textureToMaterial = new Dictionary<string, string>();
            string[] valveKeys = new string[valveAssetTables.Keys.Count];
            valveAssetTables.Keys.CopyTo(valveKeys, 0);
            for (int i = 0; i < valveKeys.Length; i++)
            {
                foreach (var t in valveAssetTables[valveKeys[i]].textures)
                {
                    var textureName = t.Value;
                    if (!textureToMaterial.ContainsKey(textureName))
                        textureToMaterial.Add(textureName, valveKeys[i]);
                }
            }

            // Matching VMT name to awful material name
            Dictionary<string, string> materialToVMT = new Dictionary<string, string>();

            List<string> failedMaterials = new List<string>();

            for (int x = 0; x < allRenderers.Count; x++)
            {
                SerializedObject meshObject = new SerializedObject(allRenderers[x]);
                SerializedProperty sharedMats = meshObject.FindProperty("m_Materials");
                string[] texKeys = new string[textures.Keys.Count];
                textures.Keys.CopyTo(texKeys, 0);

                EditorUtility.DisplayProgressBar(
                    "Applying textures to models" +
                    " (" + x + "/" + allRenderers.Count + ")",
                    allRenderers[x].name, (float)x / (float)allRenderers.Count);

                // Fill the material dictionaries
                for (int i = 0; i < sharedMats.arraySize; i++)
                {
                    var prop = sharedMats.GetArrayElementAtIndex(i);
                    var mat = prop.objectReferenceValue;
                    if (mat == null) continue;

                    string matName = mat.name;
                    if ((ModelTexturerData.ModelType)modelType.enumValueIndex != ModelTexturerData.ModelType.Map)
                    {
                        if ((Team)team.enumValueIndex == Team.BLU) matName = matName.Replace("_red", "_blue");
                    }

                    // Skip if checked
                    if (materialToVMT.ContainsKey(matName) || failedMaterials.Contains(matName)) continue;

                    string matchingName = string.Empty;
                    // Find the VMT name that best matches the material name
                    for (int j = 0; j < valveKeys.Length; j++)
                    {
                        string n = valveKeys[j];
                        if (matName.Contains(n))
                        {
                            if (matchingName.Length < n.Length) matchingName = n;
                        }
                    }

                    if (!string.IsNullOrEmpty(matchingName))
                    {
                        materialToVMT.Add(matName, matchingName);
                    }
                    else
                    {
                        failedMaterials.Add(matName);
                    }
                }

                for (int i = 0; i < sharedMats.arraySize; i++)
                {
                    var prop = sharedMats.GetArrayElementAtIndex(i);
                    var mat = prop.objectReferenceValue;

                    if (mat == null) continue;

                    string matName = mat.name;

                    if ((ModelTexturerData.ModelType)modelType.enumValueIndex != ModelTexturerData.ModelType.Map)
                    {
                        if ((Team)team.enumValueIndex == Team.BLU) matName = matName.Replace("_red", "_blue");
                    }

                    if (!materialsToGenerate.Contains(matName))
                    {
                        // Apply the existing material to the mesh
                        if (skipExistingMaterials)
                        {
                            string matPath = Path.Combine(generatedMaterialSavePath.stringValue, matName + ".mat");
                            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (m) prop.objectReferenceValue = m;
                            continue;
                        }
                    }

                    if (!materialToVMT.ContainsKey(matName))
                    {
                        if (!skipWarnings && !failedMaterials.Contains(matName))
                        {
                            if (EditorUtility.DisplayDialog("VMT Warning!", "No corresponding VMT for " +
                            "material " + matName + " found!", "Ignore All", "OK"))
                            {
                                skipWarnings = true;
                            }
                        }
                        failedMaterials.Add(matName);
                        continue;
                    }
                    string vmtName = materialToVMT[matName];

                    Material newMat = null;

                    // If material exists, use it!
                    if (generatedMaterials.ContainsKey(matName))
                    {
                        newMat = generatedMaterials[matName];
                    }
                    else
                    {
                        string matPath = Path.Combine(generatedMaterialSavePath.stringValue, matName + ".mat");

                        Material loadedMat = null;
                        if (skipExistingMaterials)
                        {
                            loadedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (loadedMat) newMat = loadedMat;
                        }

                        if (newMat == null)
                        {
                            newMat = new Material(defaultMaterial.objectReferenceValue as Material);
                            newMat.name = matName;
                        }

                        if (valveAssetTables[vmtName].shader != null)
                        {
                            newMat.shader = valveAssetTables[vmtName].shader;
                        }

                        bool textureFound = false;

                        // Set texture properties
                        string[] keys = new string[valveAssetTables[vmtName].textures.Count];
                        valveAssetTables[vmtName].textures.Keys.CopyTo(keys, 0);
                        for (int j = 0; j < keys.Length; j++)
                        {
                            var texture = textures[valveAssetTables[vmtName].textures[keys[j]]];
                            string propName = propertyOverrideLookup[keys[j]].shaderProperty;
                            newMat.SetTexture(propName, texture);
                            textureFound = true;
                        }

                        // Set colour properties
                        keys = new string[valveAssetTables[vmtName].colours.Count];
                        valveAssetTables[vmtName].colours.Keys.CopyTo(keys, 0);
                        for (int j = 0; j < keys.Length; j++)
                        {
                            string propName = propertyOverrideLookup[keys[j]].shaderProperty;
                            newMat.SetColor(propName, valveAssetTables[vmtName].colours[keys[j]]);
                        }

                        // Set float properties
                        keys = new string[valveAssetTables[vmtName].floats.Count];
                        valveAssetTables[vmtName].floats.Keys.CopyTo(keys, 0);
                        for (int j = 0; j < keys.Length; j++)
                        {
                            string propName = propertyOverrideLookup[keys[j]].shaderProperty;
                            newMat.SetFloat(propName, valveAssetTables[vmtName].floats[keys[j]]);
                        }

                        if (!textureFound)
                        {
                            if (!skipWarnings)
                            {
                                if (EditorUtility.DisplayDialog(
                                   "Missing Texture Warning!",
                                   "No main texture found for material " + newMat.name, "Skip All", "OK"))
                                {
                                    skipWarnings = true;
                                }
                            }
                            problemMaterials.Add(newMat.name);
                        }

                        generatedMaterials.Add(newMat.name, newMat);

                        if (!loadedMat) AssetDatabase.CreateAsset(newMat, matPath);
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

        SerializedObject assetSO;
        SerializedProperty defaultMaterial;
        SerializedProperty shaderOverrides;
        SerializedProperty propertyOverrides;

        void RenderOverrideProperties()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(overrideAsset);
            if (GUILayout.Button(" Create ", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                VMTPropOverrides newData;
                EditorHelper.OpenSmartSaveFileDialog(out newData, "New Property Overrides");
                newData.defaultMaterial = ModelTexturerData.overrideAsset.defaultMaterial;
                newData.shaderOverrides = new List<VMTPropOverrides.ShaderOverride>(ModelTexturerData.overrideAsset.shaderOverrides);
                newData.propertyOverrides = new List<VMTPropOverrides.PropertyOverride>(ModelTexturerData.overrideAsset.propertyOverrides);
                overrideAsset.objectReferenceValue = newData;
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                FindOverrideAssetProperties();
            }

            showImportOptions = EditorGUILayout.Foldout(showImportOptions, "Options", true);
            if (showImportOptions)
            {
                if (overrideAsset.objectReferenceValue)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.PropertyField(defaultMaterial, new GUIContent("Default Material",
                        "The base material all generated materials will use."));

                    EditorGUILayout.Space();

                    if (showHelpText.boolValue) EditorGUILayout.LabelField("When converting a VMT to Unity, the default material's " +
                        "shader will be replaced depending on the shader used in the VMT.",
                        TF2LsStyles.HelpTextStyle);
                    EditorGUILayout.PropertyField(shaderOverrides);

                    if (showHelpText.boolValue) EditorGUILayout.LabelField("When converting a VMT to Unity, the VMT will be scanned for " +
                        "properties using the names in the left column to pass to your shader's " +
                        "properties matching the names in the middle column.",
                        TF2LsStyles.HelpTextStyle);
                    EditorGUILayout.PropertyField(propertyOverrides);
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void FindOverrideAssetProperties()
        {
            if (!overrideAsset.objectReferenceValue) return;
            assetSO = new SerializedObject(overrideAsset.objectReferenceValue);

            defaultMaterial = assetSO.FindProperty(nameof(defaultMaterial));
            shaderOverrides = assetSO.FindProperty(nameof(shaderOverrides));
            propertyOverrides = assetSO.FindProperty(nameof(propertyOverrides));
        }

        void RenderSettings()
        {
            EditorGUILayout.PropertyField(showHelpText);
        }
    }
}