using UnityEngine;
using UnityEditor;
using Ibasa.Valve.Vmt;
using System.IO;
using System.Collections.Generic;
using JackysEditorHelpers;

namespace TF2Ls
{
    public class ItemViewer : BaseTFToolsEditor<ItemViewer>
    {
        string SCHEMA_PATH => Path.Combine(new string[] { TF2LsSettings.Settings.TFInstallPath, "scripts", "items", "items_game.txt" });
        string EN_LOCALE_PATH => Path.Combine(new string[] { TF2LsSettings.Settings.TFInstallPath, "resource", "tf_english.txt" });

        static BasicVDFParser itemsGame;
        static BasicVDFParser tfEnglish;

        static bool schemaLoaded => itemsGame != null;

        static float scroll;
        string searchFilter;
        static List<ItemData> filteredItems;
        static List<string> itemNames;
        static TF2APIResult payload;
        Texture2D placeholderGraphic;

        const int BUTTON_ROW_SIZE = 4;
        static float buttonSize => (Window.position.width - 12) / BUTTON_ROW_SIZE;

        class Button
        {
            public ItemData ItemData;
            public Rect Rect;
            public bool Visible;
        }

        static List<Button> buttons;

        public static void InitUtility()
        {
            window = CreateInstance<ItemViewer>();
            window.SetWindowTitle();
            window.ShowUtility();

            window.maxSize = new Vector2(128 * BUTTON_ROW_SIZE, 1080);
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Item Viewer");
        }

        private void OnEnable()
        {
            if (ModelTexturerWindow.Items.Count == 0)
            {
                //EditorUtility.DisplayProgressBar(ObjectNames.GetClassName(this),
                //    "Parsing item_game.txt...",
                //    0
                //    );
                //itemsGame = new VDFParser(File.ReadAllLines(SCHEMA_PATH));
                //var prop = Gameloop.Vdf.VdfConvert.Deserialize(File.ReadAllText(SCHEMA_PATH));
                EditorUtility.DisplayProgressBar(ObjectNames.GetClassName(this),
                    "Parsing tf_english.txt...",
                    0.5f
                    );
                tfEnglish = new BasicVDFParser(File.ReadAllLines(EN_LOCALE_PATH));
                EditorUtility.ClearProgressBar();

                RetrieveOnlineItems();
                //RetrieveItems();
            }
            filteredItems = new List<ItemData>(ModelTexturerWindow.Items);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            searchFilter = EditorGUILayout.TextField(GUIContent.none, searchFilter);
            if (GUILayout.Button("Search"))
            {
                ApplySearchFilter();
            }
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(new Vector2(0, scroll)).y;

            var style = GUI.skin.button.ApplyTextAnchor(TextAnchor.LowerCenter);
            style.imagePosition = ImagePosition.ImageAbove;

            int row = 0;

            buttons = new List<Button>();

            //for (int i = 0; i < filteredItems.Count; i++)
            //{
            //    //if (!filteredItems[i].used_by_classes.Contains("spy")) continue;
            //    if (GUILayout.Button(filteredItems[i].name))
            //    {
            //        Application.OpenURL("https://wiki.teamfortress.com/wiki/" + filteredItems[i].name);
            //    }
            //}

            var items = filteredItems;
            for (int i = 0; i < items.Count; i++, row = (row + 1) % BUTTON_ROW_SIZE)
            {
                if (row == 0) EditorGUILayout.BeginHorizontal();

                var shortName = items[i].name;
                if (shortName.Length > 15) shortName = shortName.Remove(15) + "...";
                var graphic = items[i].loadedImage ?? placeholderGraphic;
                GUIContent content = new GUIContent(shortName, graphic, items[i].name);

                EditorGUILayout.BeginVertical();
                Rect rect = EditorGUILayout.GetControlRect(false, buttonSize, style);
                rect.width = buttonSize;
                rect.x = row * buttonSize;

                if (GUI.Button(rect, content, style))
                {
                    ModelTexturerWindow.ActiveItem = items[i];
                    window.Close();
                    ModelTexturerWindow.Window.Repaint();
                    GUIUtility.ExitGUI();
                }

                var b = new Button();
                rect.position += position.position - new Vector2(0, scroll);
                b.ItemData = items[i];
                b.Rect = rect;
                buttons.Add(b);

                EditorGUILayout.EndVertical();

                if (row == BUTTON_ROW_SIZE - 1) EditorGUILayout.EndHorizontal();
            }

            if (Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    bool visible = position.Contains(buttons[i].Rect.min) || position.Contains(buttons[i].Rect.max);
                    if (visible && buttons[i].ItemData.loadedImage == null)
                    {
                        if (string.IsNullOrEmpty(buttons[i].ItemData.image_url)) continue;
                        EditorUtility.DisplayProgressBar(ObjectNames.GetClassName(this),
                            "Loading preview graphic for " + buttons[i].ItemData.name + "...",
                            0.5f
                            );
                        //buttons[i].ItemData.loadedImage = LoadImageFromURL(buttons[i].ItemData.image_url);
                        buttons[i].ItemData.loadedImage = ModelTexturerWindow.PlaceHolderGraphic;
                        EditorUtility.ClearProgressBar();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(searchFilter))
            {
                filteredItems = new List<ItemData>(ModelTexturerWindow.Items);
            }
            else
            {
                filteredItems = new List<ItemData>();

                for (int i = 0; i < ModelTexturerWindow.Items.Count; i++)
                {
                    if (ModelTexturerWindow.Items[i].name.ToLowerInvariant().Contains(searchFilter.ToLowerInvariant()))
                    {
                        filteredItems.Add(ModelTexturerWindow.Items[i]);
                    }
                }
            }
        }

        Texture2D LoadImageFromURL(string url)
        {
            Texture2D tex;
            using (var webClient = new System.Net.WebClient())
            {
                byte[] dataArr = webClient.DownloadData(url);

                tex = new Texture2D(128, 128);
                ImageConversion.LoadImage(tex, dataArr);
            }
            return tex;
        }

        void RetrieveOnlineItems()
        {
            var path = Path.Combine(TF2LsSettings.ResourcesPath, "item_schema.txt");
            payload = JsonUtility.FromJson<TF2APIResult>(File.ReadAllText(path));

            itemNames = new List<string>();
            ModelTexturerWindow.Items = new List<ItemData>();
            var items = payload.result.items;
            for (int i = 0; i < items.Length; i++)
            {
                var item_name = items[i].item_name.Substring(1).ToLowerInvariant();
                if (!tfEnglish.rootNode.children[1].childrenDictionary.ContainsKey(item_name)) continue;
                if (itemNames.Contains(item_name)) continue;
                itemNames.Add(item_name);
                items[i].name = tfEnglish.rootNode.children[1].childrenDictionary[item_name].property;
                ModelTexturerWindow.Items.Add(items[i]);
            }

            placeholderGraphic = new Texture2D(128, 128);

            EditorUtility.ClearProgressBar();
        }

        void RetrieveItems()
        {
            ModelTexturerWindow.Items = new List<ItemData>();
            itemNames = new List<string>();

            Node itemNode = itemsGame.rootNode.Get("items");
            for (int i = 0; i < itemNode.children.Count; i++)
            {
                if (NodeToItem(itemNode.children[i], out ItemData newItem))
                {
                    if (!itemNames.Contains(newItem.name))
                    {
                        ModelTexturerWindow.Items.Add(newItem);
                        itemNames.Add(newItem.name);
                    }
                }
            }

            Node prefabsNode = itemsGame.rootNode.Get("prefabs");
            foreach (var node in prefabsNode.children)
            {
                if (NodeToItem(node, out ItemData newItem))
                {
                    if (!itemNames.Contains(newItem.name))
                    {
                        ModelTexturerWindow.Items.Add(newItem);
                        itemNames.Add(newItem.name);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        bool NodeToItem(Node node, out ItemData newItem)
        {
            newItem = new ItemData();

            bool hasModel = false;
            if (node.childrenDictionary.ContainsKey("model_player"))
            {
                if (!string.IsNullOrWhiteSpace(node.childrenDictionary["model_player"].property))
                {
                    newItem.model_player = node.childrenDictionary["model_player"].property;
                    hasModel = true;
                }
            }

            if (node.childrenDictionary.ContainsKey("model_player_per_class"))
            {
                if (!string.IsNullOrWhiteSpace(node.childrenDictionary["model_player_per_class"].children[0].property))
                {
                    newItem.model_player_per_class = node.childrenDictionary["model_player_per_class"].children[0].property;
                    hasModel = true;
                }
            }

            if (!hasModel) return false;

            string nameKey;
            if (node.childrenDictionary.ContainsKey("item_name"))
            {
                nameKey = node.childrenDictionary["item_name"].property.Remove(0, 1).ToLowerInvariant();
                newItem.item_name = nameKey;
            }
            else if (node.childrenDictionary.ContainsKey("base_item_name"))
            {
                nameKey = node.childrenDictionary["base_item_name"].property.Remove(0, 1).ToLowerInvariant();
                newItem.item_name = nameKey;
            }
            else
            {
                return false;
            }

            if (tfEnglish.rootNode.children[1].childrenDictionary.ContainsKey(nameKey))
            {
                newItem.name = tfEnglish.rootNode.children[1].childrenDictionary[nameKey].property;
            }
            else
            {
                Debug.Log(nameKey);
                return false;
            }

            newItem.used_by_classes = new List<string>();
            if (node.childrenDictionary.ContainsKey("used_by_classes"))
            {
                foreach (var c in node.childrenDictionary["used_by_classes"].children)
                {
                    newItem.used_by_classes.Add(c.name);
                }
            }

            return true;
        }
    }

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

        static List<ItemData> items = new List<ItemData>();

        static BasicVDFParser itemsGame;
        static BasicVDFParser tfEnglish;

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
                itemsGame = new BasicVDFParser(File.ReadAllLines(SCHEMA_PATH));
                tfEnglish = new BasicVDFParser(File.ReadAllLines(EN_LOCALE_PATH));
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
            items = new List<ItemData>();
            Node itemNode = itemsGame.rootNode.Get("items");
            foreach (var node in itemNode.children)
            {
                if (!node.childrenDictionary.ContainsKey("used_by_classes")) continue;
                var newItem = new ItemData();
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