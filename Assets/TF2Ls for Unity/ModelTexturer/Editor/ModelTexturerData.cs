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

        public string vmtPath;
        public string vtfPath;
        public string textureOutputFolderPath;
        public string generatedMaterialSavePath;

        public VMTPropOverrides overrideAsset;

        public bool showHelpText = true;
    }
}
