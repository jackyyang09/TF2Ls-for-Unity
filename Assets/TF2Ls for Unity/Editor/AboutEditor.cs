using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TF2Ls
{
    public class AboutEditor : Editor
    {
        [MenuItem(TF2LsConstants.Paths.ABOUT, false, priority = 999)]
        public static void About()
        {
            if (EditorUtility.DisplayDialog(TF2LsConstants.PACKAGE_NAME + " (Version " + TF2LsConstants.VERSION + ")",
                "Thanks for the support!" +
                System.Environment.NewLine + "Check me out on Twitter for more TF2 Unity stuff", "Check out Twitter", "Close"))
            {
                Application.OpenURL("https://twitter.com/Brogrammist");
            }
        }
    }
}