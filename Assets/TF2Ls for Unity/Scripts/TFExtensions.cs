using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TF2Ls
{
    public static class TFExtensions
    {
        public static bool Approximately(this Vector3 a, Vector3 b, float epsilon = float.Epsilon)
        {
            return
                Mathf.Abs(a.x - b.x) < epsilon &&
                Mathf.Abs(a.y - b.y) < epsilon &&
                Mathf.Abs(a.z - b.z) < epsilon;
        }

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