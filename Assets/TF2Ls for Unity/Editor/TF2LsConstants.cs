using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TF2Ls
{
    public static class TF2LsConstants
    {
        public const string PACKAGE_NAME = "TF2Ls for Unity";
        public const string VERSION = "0.0.3";
        public static class Paths
        {
            public const string MENU_BASE = "Tools/" + PACKAGE_NAME + "/";
            public const string ABOUT = MENU_BASE + "About " + TF2LsConstants.PACKAGE_NAME;
            public const string SETTINGS = MENU_BASE + "TF2Ls Settings";
            public const string PROJECT_SETTINGS = "Project/TF2Ls";
            public const string MODEL_TEXTURER = MENU_BASE + "Model Texturer";
        }
    }
}