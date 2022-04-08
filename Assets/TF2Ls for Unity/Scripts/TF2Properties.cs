using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace TFTools
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

    public static class TFExtensions
    { 
        public static string ToLowerString(this Team t)
        {
            switch (t)
            {
                case Team.RED:
                    return "red";
                case Team.BLU:
                    return "blue";
            }
            return "";
        }

        /// <summary>
        /// Cached to save on getting enum names and using ToLower
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string ToLowerString(this CharacterClass c)
        {
            switch (c)
            {
                case CharacterClass.Scout:
                    return "scout";
                case CharacterClass.Soldier:
                    return "soldier";
                case CharacterClass.Pyro:
                    return "pyro";
                case CharacterClass.Demoman:
                    return "demoman";
                case CharacterClass.Heavy:
                    return "heavy";
                case CharacterClass.Engineer:
                    return "engineer";
                case CharacterClass.Medic:
                    return "medic";
                case CharacterClass.Sniper:
                    return "sniper";
                case CharacterClass.Spy:
                    return "spy";
            }
            return "";
        }
    }
}