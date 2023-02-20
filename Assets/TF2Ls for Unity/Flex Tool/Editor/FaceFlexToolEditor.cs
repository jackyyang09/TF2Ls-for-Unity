using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.IO;
using JackysEditorHelpers;

namespace TF2Ls.FaceFlex
{
    [CustomEditor(typeof(FaceFlexTool))]
    public class FaceFlexToolEditor : Editor
    {
        static string LAST_FOLDER_KEY => nameof(FaceFlexToolEditor) + nameof(LAST_FOLDER_KEY);
        static string LastFolder
        {
            get => EditorPrefs.GetString(LAST_FOLDER_KEY, "Assets");
            set => EditorPrefs.SetString(LAST_FOLDER_KEY, value);
        }

        enum ControllerShowState
        { 
            Hidden,
            Visible,
            Expanded
        }

        static string CONTROLLER_VISIBLE_KEY => nameof(FaceFlexToolEditor) + nameof(CONTROLLER_VISIBLE_KEY);
        static ControllerShowState ControllerVisibility
        {
            get => (ControllerShowState)EditorPrefs.GetInt(CONTROLLER_VISIBLE_KEY);
            set => EditorPrefs.SetInt(CONTROLLER_VISIBLE_KEY, (int)value);
        }

        SerializedProperty renderer;
        SerializedProperty flexScale;
        SerializedProperty blendshapeNames;
        SerializedProperty flexControlNames;
        SerializedProperty flexControllers;
        SerializedProperty flexOps;
        SerializedProperty flexPresets;
        SerializedProperty qcFile;
        SerializedProperty qcPath;
        SerializedProperty vtaPath;
        SerializedProperty weightsL, weightsR;
        SerializedProperty menuState;

        SerializedProperty previewingMesh;

        SerializedObject qcFileObject;

        struct FlexControllerProps
        {
            public string NiceName;
            public SerializedProperty Value;
            public Vector2 Range;
        }

        DefaultAsset qcPathAsset;

        Dictionary<string, FlexControllerProps> flexControllerProps = new Dictionary<string, FlexControllerProps>();
        List<string> filteredControllers;
        string searchFilter;

        float[] animBackup;
        float animTime;

        static float scroll;

        List<FlexPreset> referencePresets;

        FaceFlexTool script => target as FaceFlexTool;
        AnimationWindow animWindow => EditorWindow.GetWindow<AnimationWindow>();
        GameObject gameObject => script.gameObject;

        static string PresetPath => Path.Combine(TF2LsSettings.Settings.PackagePath, "Flex Tool", "Presets");

        private void OnEnable()
        {
            renderer = serializedObject.FindProperty(nameof(renderer));
            flexScale = serializedObject.FindProperty(nameof(flexScale));
            blendshapeNames = serializedObject.FindProperty(nameof(blendshapeNames));
            flexControlNames = serializedObject.FindProperty(nameof(flexControlNames));
            flexControllers = serializedObject.FindProperty(nameof(flexControllers));
            flexOps = serializedObject.FindProperty(nameof(flexOps));
            flexPresets = serializedObject.FindProperty(nameof(flexPresets));
            qcFile = serializedObject.FindProperty(nameof(qcFile));
            qcPath = serializedObject.FindProperty(nameof(qcPath));
            vtaPath = serializedObject.FindProperty(nameof(vtaPath));
            weightsL = serializedObject.FindProperty(nameof(weightsL));
            weightsR = serializedObject.FindProperty(nameof(weightsR));
            menuState = serializedObject.FindProperty(nameof(menuState));

            previewingMesh = serializedObject.FindProperty(nameof(previewingMesh));

            qcPathAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(qcPath.stringValue);

            if (qcFile.objectReferenceValue)
            {
                qcFileObject = new SerializedObject(qcFile.objectReferenceValue);

                for (int i = 0; i < flexControllers.arraySize; i++)
                {
                    var prop = flexControllers.GetArrayElementAtIndex(i);
                    var name = prop.FindPropertyRelative("Name").stringValue;
                    var newProp = new FlexControllerProps();
                    newProp.NiceName = ObjectNames.NicifyVariableName(name.Replace('_', ' '));
                    newProp.Value = qcFileObject.FindProperty("value" + i);
                    newProp.Range = prop.FindPropertyRelative("Range").vector2Value;
                    flexControllerProps.Add(name, newProp);
                }
            }
            else
            {
                menuState.enumValueIndex = 0;
                serializedObject.ApplyModifiedProperties();
            }

            ApplySearchFilter();

            LoadReferencePresets();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        private void PlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                script.UnloadEditorMesh();
            }
        }

        private void OnUndoRedoPerformed()
        {
            script.UpdateBlendShapes();
        }

        void Update()
        {
            if (qcFileObject == null) return;

            if (AnimationMode.InAnimationMode())
            {
                if (animBackup == null)
                {
                    if (!previewingMesh.boolValue && qcFileObject != null)
                    {
                        if (EditorUtility.DisplayDialog("Entered Animation Mode", "You just " +
                            "entered Animation mode, do you want to enable Face Flex previews?",
                            "Yes", "No"))
                        {
                            script.CreateMeshInstanceInEditor();
                        }
                    }

                    string[] keys = flexControllerProps.GetKeysCached();
                    animBackup = new float[flexControllerProps.Keys.Count];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        var prop = flexControllerProps[keys[i]];
                        animBackup[i] = prop.Value.floatValue;
                    }

                    ApplyAnimationChanges();
                    animTime = animWindow.time;
                }

                if (animTime != animWindow.time)
                {
                    ApplyAnimationChanges();
                    animTime = animWindow.time; 
                }
            }
            else
            {
                if (animBackup != null)
                {
                    string[] keys = flexControllerProps.GetKeysCached();
                    for (int i = 0; i < animBackup.Length; i++)
                    {
                        var prop = flexControllerProps[keys[i]].Value;
                        prop.floatValue = animBackup[i];
                    }
                    serializedObject.ApplyModifiedProperties();
                    if (qcFileObject.ApplyModifiedProperties()) script.UpdateBlendShapes();
                    animBackup = null;
                }
            }
        }

        void ApplyAnimationChanges()
        {
            if (!script.MeshBlendshapesConverted) return;

            var bindings = AnimationUtility.GetCurveBindings(animWindow.animationClip);
            var keys = flexControllerProps.GetKeysCached();
            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].path != gameObject.name || !bindings[i].propertyName.Contains("value")) continue;
                var curve = AnimationUtility.GetEditorCurve(animWindow.animationClip, bindings[i]);
                var index = System.Convert.ToInt16(bindings[i].propertyName.Remove(0, "value".Length));
                var prop = flexControllerProps[keys[index]];
                prop.Value.floatValue = curve.Evaluate(animWindow.time);
            }

            script.UpdateBlendShapes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            GUIStyle leftStyle = EditorStyles.miniButtonLeft;
            string buttonLabel = "  FacePoser  ";
            if (menuState.enumValueIndex == (int)MenuState.FacePoser)
            {
                leftStyle = leftStyle.ApplyBoldText();
                buttonLabel = "[ FacePoser ]";
            }

            if (GUILayout.Button(buttonLabel, leftStyle))
            {
                menuState.enumValueIndex = (int)MenuState.FacePoser;
            }

            buttonLabel = "    Setup    ";
            GUIStyle rightStyle = EditorStyles.miniButtonRight;
            if (menuState.enumValueIndex == (int)MenuState.Setup)
            {
                rightStyle = rightStyle.ApplyBoldText();
                buttonLabel = "  [ Setup ]  ";
            }

            if (GUILayout.Button(buttonLabel, rightStyle))
            {
                menuState.enumValueIndex = (int)MenuState.Setup;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space();

            switch ((MenuState)menuState.enumValueIndex)
            {
                case MenuState.Setup:
                    RenderSetupGUI();
                    break;
                case MenuState.FacePoser:
                    if (qcFileObject != null)
                    {
                        qcFileObject.Update();
                    }

                    RenderFaceposerGUI();

                    if (qcFileObject != null)
                    {
                        if (qcFileObject.ApplyModifiedProperties()) script.UpdateBlendShapes();
                    }
                    break;
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        GUIStyle titleStyle => EditorStyles.boldLabel
                .ApplyBoldText()
                .ApplyTextAnchor(TextAnchor.MiddleCenter)
                .SetFontSize(20);

        void RenderSetupGUI()
        {
            EditorGUILayout.LabelField("Setup", titleStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Convert Blend Shapes to Valve's Flex system",
                EditorStyles.label.ApplyTextAnchor(TextAnchor.MiddleCenter));

            RenderWarnings();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Setup Status", EditorStyles.largeLabel.ApplyTextAnchor(TextAnchor.UpperCenter));

            var style = EditorStyles.largeLabel.ApplyTextAnchor(TextAnchor.MiddleCenter);
            if (renderer.objectReferenceValue && qcFileObject != null)
            {
                EditorGUILayout.LabelField("Finished!", style.SetTextColor(TF2Colors.BUFF));
            }
            else
            {
                EditorGUILayout.LabelField("Unfinished", style.SetTextColor(TF2Colors.DEBUFF));
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField(ObjectNames.GetClassName(script) + " needs a reference to your " +
                "model's SkinnedMeshRenderer component in order to change mesh data and allow Face Flex manipulation.",
                TF2LsSettings.Settings.HelpTextStyle);

            EditorGUILayout.PropertyField(renderer);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("To properly convert your mesh's Blendshapes into flexes, " +
                "add your mesh's .qc file to your project and get a reference to it in the field below.",
                TF2LsSettings.Settings.HelpTextStyle);

            EditorGUI.BeginChangeCheck();
            EditorHelper.RenderSmartFileProperty(new GUIContent(".QC File"), qcPath, "qc", true, "Choose your model's .qc file");
            if (EditorGUI.EndChangeCheck())
            {
                qcPathAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(qcPath.stringValue);
            }

            EditorGUI.BeginDisabledGroup(!renderer.objectReferenceValue || !qcPathAsset || string.IsNullOrEmpty(qcPath.stringValue));
            if (GUILayout.Button("Generate Helper Component from .qc File"))
            {
                ApplyMeshImportSettings();
                ParseQCFile();
                GenerateFile();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(qcFile, new GUIContent(
                "QC Helper", ""));
        }

        void RenderFaceposerGUI()
        {
            EditorGUILayout.LabelField("Faceposer", titleStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Alter the expressions of Source-engine models",
                EditorStyles.label.ApplyTextAnchor(TextAnchor.MiddleCenter));

            RenderWarnings();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Flex Preview Status", EditorStyles.largeLabel.ApplyTextAnchor(TextAnchor.UpperCenter));

            var style = EditorStyles.largeLabel.ApplyTextAnchor(TextAnchor.MiddleCenter);
            if (previewingMesh.boolValue)
            {
                EditorGUILayout.LabelField("Previewing", style.SetTextColor(TF2Colors.BUFF));

                EditorGUILayout.LabelField("Do NOT touch the SkinnedMeshRenderer component attached to\n" +
                    "<b>" + renderer.objectReferenceValue.name + "</b>\n" +
                    "until you stop previewing!\n\n" +
                    "Otherwise, things will break!",
                    EditorStyles.label.ApplyRichText()
                    .ApplyTextAnchor(TextAnchor.MiddleCenter)
                    .ApplyWordWrap());
            }
            else
            {
                EditorGUILayout.LabelField("Not Previewing", style.SetTextColor(TF2Colors.DEBUFF));
            }

            EditorGUI.BeginDisabledGroup(qcFileObject == null);
            if (GUILayout.Button("Toggle Mesh Preview"))
            {
                if (previewingMesh.boolValue)
                {
                    script.UnloadEditorMesh();
                }
                else
                {
                    script.CreateMeshInstanceInEditor();
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!previewingMesh.boolValue);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Apply Reference Preset");
            if (EditorGUILayout.DropdownButton(new GUIContent("Pick From List to Apply"), FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                for (int i = 0; i < referencePresets.Count; i++)
                {
                    var c = new GUIContent(referencePresets[i].name);
                    menu.AddItem(c, false, LoadFaceFromPreset, referencePresets[i]);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save As Preset"))
            {
                CreatePresetFromFace();
            }

            if (GUILayout.Button("Load Face from Saved Preset"))
            {
                OpenLoadPresetDialog();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(flexScale);
            EditorGUILayout.LabelField(
                "Higher values distort the face in horrible ways. Keep between 1 and 2 for " +
                "best results. Leave it at 1 if you're not sure!",
                TF2LsSettings.Settings.HelpTextStyle);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize Sliders"))
            {
                string[] keys = flexControllerProps.GetKeysCached();
                for (int i = 0; i < keys.Length; i++)
                {
                    var prop = flexControllerProps[keys[i]];
                    prop.Value.floatValue = Random.Range(prop.Range.x, prop.Range.y);
                }
            }

            if (GUILayout.Button("Reset to Reference"))
            {
                LoadFaceFromPreset(referencePresets[0]);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Toggle Slider Visibility"))
            {
                ControllerVisibility = (ControllerShowState)Mathf.Repeat((int)ControllerVisibility + 1, (int)ControllerShowState.Expanded + 1);
            }

            EditorGUILayout.Space();

            if (ControllerVisibility != ControllerShowState.Hidden)
            {
                EditorGUI.BeginChangeCheck();
                searchFilter = EditorGUILayout.TextField("Quick Filter", searchFilter);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySearchFilter();
                }

                if (ControllerVisibility == ControllerShowState.Visible && filteredControllers.Count > 0)
                {
                    scroll = EditorGUILayout.BeginScrollView(new Vector2(0, scroll),
                    new GUILayoutOption[] { GUILayout.MaxHeight(400) }).y;
                }

                for (int i = 0; i < filteredControllers.Count; i++)
                {
                    var p = flexControllerProps[filteredControllers[i]];

                    SerializedProperty prop = p.Value;
                    //float leftValue = unclampSliders.boolValue ? -1 : p.Range.x;
                    float leftValue = -1;

                    EditorGUILayout.Slider(prop, leftValue, p.Range.y, p.NiceName);
                }

                if (ControllerVisibility == ControllerShowState.Visible && filteredControllers.Count > 0)
                    EditorGUILayout.EndScrollView();
            }

            EditorGUI.EndDisabledGroup();
        }

        void RenderWarnings()
        {
            if (!qcFile.objectReferenceValue)
            {
                EditorGUILayout.HelpBox("Couldn't find a QC Helper component! " +
                    "Make sure you've completed setting up the Face Flex Tool."
                , MessageType.Error);
            }
        }

        void LoadReferencePresets()
        {
            referencePresets = EditorHelper.ImportAssetsAtPath<FlexPreset>(PresetPath);
        }

        void CreatePresetFromFace()
        {
            serializedObject.ApplyModifiedProperties();

            var path = EditorHelper.OpenSmartSaveFileDialog(out FlexPreset newPreset, "New Face Preset", LastFolder);

            if (newPreset == null) return;

            newPreset.flexScale = flexScale.floatValue;

            var keys = flexControllerProps.GetKeysCached();
            newPreset.flexControllerNames = new string[keys.Length];
            newPreset.values = new float[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                newPreset.flexControllerNames[i] = keys[i];
                newPreset.values[i] = flexControllerProps[keys[i]].Value.floatValue;
            }

            LastFolder = path;

            Selection.activeGameObject = gameObject;
        }

        void OpenLoadPresetDialog()
        {
            var path = EditorUtility.OpenFilePanel("Open Face Preset", LastFolder, "asset");
            path = EditorHelper.GetProjectRelativePath(path);

            if (string.IsNullOrEmpty(path)) return;

            var preset = AssetDatabase.LoadAssetAtPath<FlexPreset>(path);
            LoadFaceFromPreset(preset);

            LastFolder = path;
        }

        void LoadFaceFromPreset(object input)
        {
            var preset = input as FlexPreset;

            flexScale.floatValue = preset.flexScale;

            for (int i = 0; i < preset.flexControllerNames.Length; i++)
            {
                if (!flexControllerProps.ContainsKey(preset.flexControllerNames[i])) continue;
                flexControllerProps[preset.flexControllerNames[i]].Value.floatValue = preset.values[i];
            }

            if (qcFileObject.ApplyModifiedProperties()) script.UpdateBlendShapes();
            serializedObject.ApplyModifiedProperties();
        }

        void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(searchFilter))
            {
                filteredControllers = new List<string>(flexControllerProps.Keys);
            }
            else
            {
                filteredControllers = new List<string>();

                foreach (var item in flexControllerProps)
                {
                    if (item.Value.NiceName.ToLower().Contains(searchFilter.ToLower()))
                    {
                        filteredControllers.Add(item.Key);
                    }
                }
            }
        }

        const float MaxX = 3.5f;
        const float SmoothedX = 0.5f;
        const float Diff = 0.001f;

        void ApplyMeshImportSettings()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(script.Mesh));
            ModelImporter modelImporter = importer as ModelImporter;
            modelImporter.isReadable = true;
            modelImporter.SaveAndReimport();
        }

        void RevertMeshChanges()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(script.Mesh));
            ModelImporter modelImporter = importer as ModelImporter;
            modelImporter.importNormals = ModelImporterNormals.Calculate;
            modelImporter.importBlendShapes = true;
            modelImporter.importBlendShapeNormals = ModelImporterNormals.Calculate;
            modelImporter.globalScale = 100;
            modelImporter.SaveAndReimport();
        }

        void ParseQCFile()
        {
            script.SplitBlendshapes();

            blendshapeNames.ClearArray();

            bool flexFileOpen = false;
            var qc = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), qcPath.stringValue));
            for (int i = 0; i < qc.Length; i++)
            {
                if (qc[i].Contains("flexfile")) flexFileOpen = true;

                if (flexFileOpen)
                {
                    if (qc[i].Contains("}")) break;
                    else if (qc[i].Contains("\""))
                    {
                        qc[i] = qc[i].TrimStart();
                        var splits = qc[i].Split(' ');
                        splits[1] = splits[1].Trim('"');
                        if (splits[0] == "flexpair")
                        {
                            blendshapeNames.AddAndReturnNewArrayElement().stringValue = splits[1] + "L+" + splits[1] + "R";
                        }
                        else if (splits[0] == "flex")
                        {
                            blendshapeNames.AddAndReturnNewArrayElement().stringValue = splits[1];
                        }
                    }
                }
            }
        }

        #region .pre file logic
        void ImportDMEPresets()
        {
            var path = @"I:\My Drive\Modelling\TF2 Mods\Sniper\sniper_emotion.pre";
            var preFile = File.ReadAllLines(path);

            flexPresets.ClearArray();

            for (int i = 0; i < preFile.Length; i++)
            {
                if (preFile[i].Contains("DmePreset"))
                {
                    var newPreset = new FacePreset();
                    newPreset.FlexNames = new List<string>();
                    newPreset.FlexValues = new List<float>();
                    newPreset.FlexBalances = new List<float>();
                    newPreset.FlexMultiLevels = new List<float>();
                    while (!preFile[i].Contains("]"))
                    {
                        i++;
                        var splits = preFile[i].TrimStart().Split(' ');
                        if (splits[0].Contains("name"))
                        {
                            newPreset.Name = splits[2].Trim('"');
                        }
                        else if (splits[0].Contains("DmElement"))
                        {
                            newPreset.FlexNames.Add("");
                            newPreset.FlexValues.Add(-1);
                            newPreset.FlexBalances.Add(-1);
                            newPreset.FlexMultiLevels.Add(-1);
                            // Bad recursion, but it's only two layers so whatever
                            while (!preFile[i].Contains("}"))
                            {
                                i++;
                                var s = preFile[i].TrimStart().Split(' ');
                                if (s[0].Contains("name"))
                                {
                                    newPreset.FlexNames[i] = s[2].Trim('"');
                                }
                                else if (s[0].Contains("value"))
                                {
                                    newPreset.FlexValues[i] = float.Parse(s[2].Trim('"'));
                                }
                                else if (s[0].Contains("balance"))
                                {
                                    newPreset.FlexValues[i] = float.Parse(s[2].Trim('"'));
                                }
                                else if (s[0].Contains("multilevel"))
                                {
                                    newPreset.FlexValues[i] = float.Parse(s[2].Trim('"'));
                                }
                            }
                        }
                    }
                    flexPresets.AddAndReturnNewArrayElement().managedReferenceValue = newPreset;
                }
            }
        }

        int selectedOption;
        void ApplyPreset()
        {
            var selectedPreset = flexPresets.GetArrayElementAtIndex(selectedOption);
            var names = selectedPreset.FindPropertyRelative("FlexNames");
            var values = selectedPreset.FindPropertyRelative("FlexValues");
            var balances = selectedPreset.FindPropertyRelative("FlexBalances");
            var multiLevels = selectedPreset.FindPropertyRelative("FlexMultiLevels");
            for (int i = 0; i < names.arraySize; i++)
            {
                var name = names.GetArrayElementAtIndex(i).stringValue;
                var value = values.GetArrayElementAtIndex(i).floatValue;
                var balance = balances.GetArrayElementAtIndex(i).floatValue;
                var multiLevel = multiLevels.GetArrayElementAtIndex(i).floatValue;
                if (balance > -1) // How is this calculated?
                {
                }
                //flexControllerProps[name].Value.floatValue = 
            }
        }
        #endregion

        string scriptPath;
        void GenerateFile()
        {
            string className = qcPath.stringValue.Substring(qcPath.stringValue.LastIndexOf('/') + 1);
            className = className.Remove(className.IndexOf('.')) + "QC";
            var c = className.ToCharArray();
            c[0] = char.ToUpper(c[0]);
            className = new string(c);

            string fileName = className + ".cs";

            EditorUtility.DisplayProgressBar("Generating Helper Component", "Generating code...", 0.15f);

            var qcPathFull = Path.Combine(System.Environment.CurrentDirectory, qcPath.stringValue);
            scriptPath = Path.Combine(Application.dataPath, fileName);

            File.WriteAllText(scriptPath, string.Empty);
            StreamWriter writer = new StreamWriter(scriptPath, true);

            writer.Write("/**" +
                "\n* File generated by TF2Ls" +
                "\n* Do not modify unless you know what you're doing" +
                "\n*/ ");

            writer.WriteLine("\nusing UnityEngine;");

            writer.WriteLine("namespace TF2Ls.FaceFlex {");

            string indent = "    ";
            string bigIndent = indent + indent;
            writer.WriteLine(indent + "public class " + className + " : BaseQC {");

            var flexProps = new List<string>();
            var blendShapeCode = new List<string>();
            if (flexControlNames != null) flexControlNames.ClearArray();
            if (flexControllers != null) flexControllers.ClearArray();

            List<string> localVars = new List<string>();
            var qc = File.ReadAllLines(qcPath.stringValue);
            for (int i = 0; i < qc.Length; i++)
            {
                if (qc[i].Contains("flexcontroller"))
                {
                    qc[i] = qc[i].Substring(qc[i].IndexOf("flexcontroller ") + 15);
                    int quote = qc[i].IndexOf("\"") + 1;
                    var name = qc[i].Substring(quote, qc[i].Length - quote - 1);
                    qc[i] = qc[i].Substring(qc[i].IndexOf("range") + 6);
                    Vector2 range = new Vector2();
                    float.TryParse(qc[i].Substring(0, qc[i].IndexOf(' ')), out range.x);
                    qc[i] = qc[i].Substring(qc[i].IndexOf(' ') + 1);
                    float.TryParse(qc[i].Substring(0, qc[i].IndexOf(' ')), out range.y);

                    var newProp = flexControllers.AddAndReturnNewArrayElement();
                    flexControlNames.AddAndReturnNewArrayElement().stringValue = name;
                    newProp.FindPropertyRelative("Name").stringValue = name;
                    newProp.FindPropertyRelative("Range").vector2Value = range;
                }
                else if (qc[i].Contains("localvar")) // Ignore these
                {
                    var splits = qc[i].TrimStart().Split(' ');
                    localVars.Add(splits[1]);
                }
                else
                {
                    var percent = qc[i].IndexOf('%');

                    if (percent > -1)
                    {
                        string name = qc[i].Substring(percent + 1, qc[i].IndexOf('=') - 3);

                        if (localVars.Contains(name)) continue;

                        qc[i] = qc[i].Substring(percent + 1);

                        var line = qc[i];

                        if (line.Contains("//")) // Exclude comments
                        {
                            line = line.Remove(line.IndexOf("//"));
                        }

                        flexProps.Add(line);

                        line = line.Replace("min", "Mathf.Min");
                        line = line.Replace("max", "Mathf.Max");
                        line += ";";
                        blendShapeCode.Add(line);
                    }
                }
            }

            writer.WriteLine("\n" + bigIndent + "// Flex Props");
            for (int i = 0; i < flexControllers.arraySize; i++)
            {
                var prop = flexControllers.GetArrayElementAtIndex(i);
                var name = prop.FindPropertyRelative("Name").stringValue;
                writer.WriteLine(bigIndent + "[SerializeField, HideInInspector] float value" + i + ";");
                writer.WriteLine(bigIndent + "float " + name +
                     " => faceFlex.ProcessValue(value" + i + ", " + i + ");");
            }

            writer.WriteLine("\n" + bigIndent + "// Blend Shape Props");
            for (int i = 0; i < this.script.Mesh.blendShapeCount; i++)
            {
                writer.WriteLine(bigIndent + "float " + this.script.Mesh.GetBlendShapeName(i) + 
                    " { set { renderer.SetBlendShapeWeight(" + i + ", value * FlexScale); } }");
            }

            writer.WriteLine("\n" + bigIndent + "public override void UpdateBlendShapes()");
            writer.WriteLine(bigIndent + "{");
            for (int i = 0; i < blendShapeCode.Count; i++)
            {
                writer.WriteLine(bigIndent + indent + blendShapeCode[i]);
            }

            writer.WriteLine(bigIndent + "}");
            writer.WriteLine(indent + "}");
            writer.WriteLine("}");

            writer.Close();

            script.UnloadEditorMesh();

            EditorUtility.DisplayProgressBar("Generating Helper Component", "Importing and compiling...", 0.9f);
            scriptPath = EditorHelper.GetProjectRelativePath(scriptPath);

            TFToolsAssPP.OnMonoScriptImported += AddHelperComponent;

            EditorApplication.update += ImportScript;

            EditorUtility.DisplayProgressBar("Generating Helper Component", "Done!", 1);

            EditorUtility.ClearProgressBar();
        }

        public void ImportScript()
        {
            EditorApplication.update -= ImportScript;
            //Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(scriptPath);
        }

        private void AddHelperComponent(MonoScript obj)
        {
            if (obj == null) return;
            TFToolsAssPP.OnMonoScriptImported -= AddHelperComponent;
            if (!script.gameObject.GetComponent(obj.GetClass()))
            {
                script.gameObject.AddComponent(obj.GetClass());
            }
        }

        #region Deprecated
        void GenerateBlendShapes()
        {
            var v = script.Mesh.vertices;

            script.Mesh.ClearBlendShapes();

            var weightsL = new float[v.Length];
            var weightsR = new float[v.Length];
            int i;
            for (i = 0; i < v.Length; i++)
            {
                if (Mathf.Abs(v[i].x) < MaxX)
                {
                    if (v[i].x < 0)
                    {
                        weightsL[i] = Mathf.Lerp(1, 0.5f, Mathf.InverseLerp(-SmoothedX, 0, v[i].x));
                        weightsR[i] = Mathf.Lerp(0, 0.5f, Mathf.InverseLerp(-SmoothedX, 0, v[i].x));
                    }
                    else
                    {
                        weightsL[i] = Mathf.Lerp(0.5f, 0, Mathf.InverseLerp(0, SmoothedX, v[i].x));
                        weightsR[i] = Mathf.Lerp(0.5f, 1, Mathf.InverseLerp(0, SmoothedX, v[i].x));
                    }
                }
            }

            List<int> vtaToMeshVerts = new List<int>();
            var vta = File.ReadAllLines(vtaPath.stringValue);

            for (i = 0; i < vta.Length; i++) // Do nothing until we get to shape key data
            {
                if (vta[i].TrimStart() == "vertexanimation")
                {
                    break;
                }
            }

            bool readingVertex = false;
            bool settingUpBasis = false;
            string shapeKeyName = "";
            var shapeKeyDeltas = new Vector3[v.Length];
            var shapeKeyNormals = new Vector3[v.Length];
            bool hasLeftRight;
            for (; i < vta.Length; i++)
            {
                vta[i] = vta[i].TrimStart();
                var splits = vta[i].Split(' ');

                if (splits[0] == "time" || splits[0] == "end")
                {
                    readingVertex = true;
                    if (splits[0] == "time")
                    {
                        settingUpBasis = splits[1] == "0";
                    }
                    else settingUpBasis = false;

                    if (!settingUpBasis)
                    {
                        if (shapeKeyName != "") // Start evaluating last shape key data
                        {
                            hasLeftRight = shapeKeyName.IndexOf('+') > -1;

                            if (!hasLeftRight)
                            {
                                script.Mesh.AddBlendShapeFrame(shapeKeyName, 1, shapeKeyDeltas, shapeKeyNormals, new Vector3[v.Length]);
                            }
                            else
                            {
                                var nameSplits = shapeKeyName.Split('+');
                                var leftDeltas = new Vector3[v.Length];
                                for (int j = 0; j < leftDeltas.Length; j++)
                                {
                                    leftDeltas[j] = shapeKeyDeltas[j] * weightsL[j];
                                }
                                script.Mesh.AddBlendShapeFrame(nameSplits[0], 1, leftDeltas, shapeKeyNormals, new Vector3[v.Length]);

                                var rightDeltas = new Vector3[v.Length];
                                for (int j = 0; j < leftDeltas.Length; j++)
                                {
                                    rightDeltas[j] = shapeKeyDeltas[j] * weightsR[j];
                                }
                                script.Mesh.AddBlendShapeFrame(nameSplits[1], 1, rightDeltas, shapeKeyNormals, new Vector3[v.Length]);
                            }
                        }

                        if (splits.Length > 2)
                        {
                            shapeKeyName = splits[3];
                            shapeKeyDeltas = new Vector3[v.Length];
                            shapeKeyNormals = new Vector3[v.Length];
                        }
                    }
                    continue;
                }

                if (readingVertex)
                {
                    if (settingUpBasis)
                    {
                        var vertex = new Vector3(
                            float.Parse(splits[1]), float.Parse(splits[2]), float.Parse(splits[3]));
                        var adjustedV = new Vector3(-vertex.x, -vertex.z, vertex.y);
                        //var adjustedV = new Vector3(vertex.x, -vertex.z, vertex.y);

                        var normal = new Vector3(
                            float.Parse(splits[4]), float.Parse(splits[5]), float.Parse(splits[6]));
                        var adjustedN = new Vector3(-normal.x, -normal.z, normal.y);
                        //var adjustedN = new Vector3(normal.x, -normal.z, normal.z);

                        bool found = false;
                        for (int j = 0; j < v.Length; j++)
                        {
                            if (v[j].Approximately(adjustedV, Diff)/* && n[j].Approximately(adjustedN, Diff)*/)
                            {
                                vtaToMeshVerts.Add(j);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            vtaToMeshVerts.Add(-1); // Pad it out
                        }
                    }
                    else
                    {
                        int index = int.Parse(splits[0]);
                        index = vtaToMeshVerts[index];
                        if (index >= v.Length || index == -1) continue;
                        var og = new Vector3(
                            float.Parse(splits[1]), float.Parse(splits[2]), float.Parse(splits[3]));

                        var normal = new Vector3(
                            float.Parse(splits[4]), float.Parse(splits[5]), float.Parse(splits[6]));

                        //shapeKeyDeltas[index] = new Vector3(og.x, -og.z, og.y);
                        shapeKeyDeltas[index] = new Vector3(-og.x, -og.z, og.y);
                        shapeKeyDeltas[index] = shapeKeyDeltas[index] - v[index];
                        //shapeKeyNormals[index] = new Vector3(normal.x, -normal.z, normal.y);
                        shapeKeyNormals[index] = new Vector3(-normal.x, -normal.z, normal.y);
                    }
                }
            }
        }
        #endregion
    }
}