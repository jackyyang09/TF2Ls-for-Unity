using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace TF2Ls
{
    public enum CharacterClass { Scout, Soldier, Pyro, Demoman, Heavy, Engineer, Medic, Sniper, Spy }
    public enum Team { RED, BLU }

    public class TF2Colors
    {
        public static Color BUFF = new Color(0.6f, 0.8f, 1f);
        public static Color DEBUFF = new Color(1, 0.25f, 0.25f);
    }

    [System.Serializable]
    public class ItemData
    {
        public string name;
        public string item_name;
        public string model_player;
        public string model_player_per_class;
        public string ModelPath
        {
            get
            {
                string path = Path.Combine("root", "materials", model_player_per_class);
                //path = model_player_per_class.Replace("%s", )
                return model_player_per_class;
            }
        }

        public string image_url;
        public string image_url_large;
        public Texture2D loadedImage;
        public List<string> used_by_classes;
    }

    [System.Serializable]
    public class ItemPayload
    {
        public ItemData[] items;
    }

    [System.Serializable]
    public class TF2APIResult
    {
        public ItemPayload result;
    }
}