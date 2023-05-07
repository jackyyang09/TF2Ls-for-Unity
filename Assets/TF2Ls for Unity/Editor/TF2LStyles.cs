using UnityEngine;
using UnityEditor;
using JackysEditorHelpers;

namespace TF2Ls
{
    public class TF2LsStyles : Editor
    {
        public static GUIStyle CenteredTitle =>
            EditorStyles.boldLabel
            .ApplyBoldText()
            .ApplyTextAnchor(TextAnchor.MiddleCenter)
            .SetFontSize(20);

        public static GUIStyle CenteredLabel => 
            EditorStyles.label
            .ApplyTextAnchor(TextAnchor.MiddleCenter);

        public static GUIStyle CenteredBoldHeader =>
            new GUIStyle(EditorStyles.boldLabel)
            .ApplyTextAnchor(TextAnchor.UpperCenter)
            .SetFontSize(14);

        public static GUIStyle CenteredHeader =>
            EditorStyles.largeLabel.ApplyTextAnchor(TextAnchor.MiddleCenter);

        public static GUIStyle UpCenteredHeader =>
            EditorStyles.largeLabel.ApplyTextAnchor(TextAnchor.UpperCenter);

        public static GUIStyle HelpTextStyle => 
            new GUIStyle(EditorStyles.helpBox).SetFontSize(TF2LsEditorSettings.Settings.HelpTextSize);
    }
}