using UnityEngine;

namespace TFTools
{
    public class ModelTexturerData: ScriptableObject
    {
        public enum ModelType
        {
            Character,
            Weapon,
            Cosmetic,
            Map
        }

        public ModelType modelType;
        public CharacterClass characterClass;
        public Team team;

        [Tooltip("If true, will try to find all VMTs and VTFs for the model by searching through your " +
            "TF2 game install. Uncheck this option if you already have the VMTs/VMTs in the proper folders.")]
        public bool searchTF2Install = true;

        public string vmtPath;
        public string vtfPath;
        public string textureOutputFolderPath;
        public string generatedMaterialSavePath;

        public VMTPropOverrides overrideAsset;

        public bool showHelpText = true;
    }
}
