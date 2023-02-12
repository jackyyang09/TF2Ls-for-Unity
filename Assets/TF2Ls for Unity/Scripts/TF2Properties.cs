using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace TF2Ls
{
    public enum CharacterClass { Scout, Soldier, Pyro, Demoman, Heavy, Engineer, Medic, Sniper, Spy }
    public enum Team { RED, BLU }

    [System.Serializable]
    public struct ItemProperties
    {
        public string name;
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
    }
}