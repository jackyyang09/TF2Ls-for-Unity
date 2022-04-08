using UnityEngine;
using UnityEditor;
using Ibasa.Valve.Vmt;
using System.IO;
using System.Collections.Generic;

namespace TFTools
{
    public class SchemaReader : BaseTFToolsEditor<SchemaReader>
    {
        #region Preferences
        const string SCROLL_X_KEY = "TFTOOLS_SCHEMAREADER_WINDOWSCROLL_X";
        static float scrollX
        {
            get { return EditorPrefs.GetFloat(SCROLL_X_KEY); }
            set { EditorPrefs.SetFloat(SCROLL_X_KEY, value); }
        }

        const string SCROLL_Y_KEY = "TFTOOLS_SCHEMAREADER_WINDOWSCROLL_Y";
        static float scrollY
        {
            get { return EditorPrefs.GetFloat(SCROLL_Y_KEY); }
            set { EditorPrefs.SetFloat(SCROLL_Y_KEY, value); }
        }
        #endregion

        static Vector2 scrollProgress
        {
            get { return new Vector2(scrollX, scrollY); }
            set { scrollX = value.x; scrollY = value.y; }
        }

        string SCHEMA_PATH { get { return Path.Combine(new string[] { TF2LsSettings.Settings.TFInstallPath, "scripts", "items", "items_game.txt" }); } }
        string EN_LOCALE_PATH { get { return Path.Combine(new string[] { TF2LsSettings.Settings.TFInstallPath, "resource", "tf_english.txt" }); } }

        static List<ItemProperties> items = new List<ItemProperties>();

        static NodeReader itemsGame;
        static NodeReader tfEnglish;

        static bool schemaLoaded = false;

        //[MenuItem("Window/TF2Ls for Unity/Schema Test", false, 1)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            Window.Show();
            window.SetWindowTitle();
            window.Focus();
        }

        private void OnEnable()
        {
            schemaLoaded = false;
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Schema Reader");
        }

        private void OnGUI()
        {
            scrollProgress = EditorGUILayout.BeginScrollView(new Vector2(scrollX, scrollY));
            if (GUILayout.Button("Load Schema"))
            {
                itemsGame = new NodeReader(File.ReadAllText(SCHEMA_PATH));
                //tfEnglish = new NodeReader(File.ReadAllText(EN_LOCALE_PATH));
                schemaLoaded = true;
            }

            if (schemaLoaded)
            {
                if (GUILayout.Button("Retrieve Items"))
                {
                    RetrieveItems();
                }
                for (int i = 0; i < items.Count; i++)
                {
                }
                RenderNode(itemsGame.rootNode);
                //RenderNode(tfEnglish.rootNode);
            }
            EditorGUILayout.EndScrollView();
        }

        void RetrieveItems()
        {
            Node itemNode = itemsGame.rootNode.Get("items");
            foreach (var node in itemNode.children)
            {
                var newItem = new ItemProperties();
                newItem.name = node.name;
                items.Add(newItem);
            }
        }

        void RenderNode(Node node)
        {
            if (node.children.Count == 0)
            {
                EditorGUILayout.LabelField(node.name, node.property);
            }
            else
            {
                node.foldout = EditorGUILayout.Foldout(node.foldout, node.name);
                if (node.foldout)
                {
                    EditorGUI.indentLevel++;
                    foreach (var p in node.children)
                    {
                        RenderNode(p);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}