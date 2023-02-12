using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TF2Ls
{
    public class AboutEditor : Editor
    {
        public const string PACKAGE_NAME = "TF2Ls for Unity";
        public const string VERSION = "Version 0.0.3";
        public const string MENU_DIRECTORY = "Tools/" + PACKAGE_NAME + "/";

        [MenuItem(MENU_DIRECTORY + "About " + PACKAGE_NAME, false, priority = 999)]
        public static void About()
        {
            if (EditorUtility.DisplayDialog(PACKAGE_NAME + " (" + VERSION + ")",
                "Thanks for the support!" +
                System.Environment.NewLine + "Check me out on Twitter for more TF2 Unity stuff", "Check out Twitter", "Close"))
            {
                Application.OpenURL("https://twitter.com/Brogrammist");
            }
        }
    }
}