using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceFlexTool : MonoBehaviour
{
    [SerializeField] new SkinnedMeshRenderer renderer;

    string path = @"I:\My Drive\Modelling\TF2 Mods\Sniper\sniper.qc";

    [ContextMenu(nameof(ParseQCFile))]
    void ParseQCFile()
    {
        var qc = System.IO.File.ReadAllLines(path);
        for (int i = 0; i < qc.Length; i++)
        {
            if (qc[i].Contains("flexcontroller")) Debug.Log(qc[i]);
        }
    }
}
