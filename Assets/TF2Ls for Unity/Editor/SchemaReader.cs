using UnityEngine;
using UnityEditor;
using Ibasa.Valve.Vmt;
using System.IO;
using System.Collections.Generic;

namespace TF2Ls
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

        string SCHEMA_PATH => Path.Combine(new string[] { TF2LsSettings.Settings.TFInstallPath, "scripts", "items", "items_game.txt" });
        string EN_LOCALE_PATH => Path.Combine(new string[] { TF2LsSettings.Settings.TFInstallPath, "resource", "tf_english.txt" });

        static List<ItemProperties> items = new List<ItemProperties>();

        static NodeReader itemsGame;
        static NodeReader tfEnglish;

        static bool schemaLoaded => itemsGame != null;

        [MenuItem(AboutEditor.MENU_DIRECTORY + "Schema Test", false, 1)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            Window.Show();
            window.SetWindowTitle();
            window.Focus();
        }

        private void OnEnable()
        {
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
                tfEnglish = new NodeReader(File.ReadAllText(EN_LOCALE_PATH));
            }

            if (schemaLoaded)
            {
                if (GUILayout.Button("Retrieve Items"))
                {
                    RetrieveItems();
                }
                int row = 0;
                for (int i = 0; i < items.Count; i++, row++)
                {
                    if (row == 0) EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(items[i].name);
                    for (int j = 0; j < items[i].used_by_classes.Count; j++)
                    {
                        EditorGUILayout.LabelField(items[i].used_by_classes[j]);
                    }
                    EditorGUILayout.EndVertical();
                    if (row == 3 || i == items.Count - 1)
                    {
                        EditorGUILayout.EndHorizontal();
                        row = -1;
                    }
                }
                RenderNode(itemsGame.rootNode);
                RenderNode(tfEnglish.rootNode);
            }
            EditorGUILayout.EndScrollView();
        }

        void RetrieveItems()
        {
            items = new List<ItemProperties>();
            Node itemNode = itemsGame.rootNode.Get("items");
            foreach (var node in itemNode.children)
            {
                if (!node.childrenDictionary.ContainsKey("used_by_classes")) continue;
                var newItem = new ItemProperties();
                string nameKey = node.childrenDictionary["name"].property;
                if (node.childrenDictionary.ContainsKey("item_name"))
                {
                    nameKey = node.childrenDictionary["item_name"].property.Remove(0, 1);
                    newItem.item_name = nameKey;
                }
                if (!tfEnglish.rootNode.children[1].childrenDictionary.ContainsKey(nameKey)) continue;
                newItem.name = tfEnglish.rootNode.children[1].childrenDictionary[nameKey].property;
                newItem.used_by_classes = new List<string>();
                foreach (var c in node.childrenDictionary["used_by_classes"].children)
                {
                    newItem.used_by_classes.Add(c.name);
                }
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