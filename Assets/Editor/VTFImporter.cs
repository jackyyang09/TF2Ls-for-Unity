using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

namespace TF2Ls
{
    //[ScriptedImporter(1, "vtf")]
    //public class VTFImporter : ScriptedImporter
    //{
    //    System.Diagnostics.Process commandShell;
    //    //System.Action OnShellProcessComplete;
    //
    //    public override void OnImportAsset(AssetImportContext ctx)
    //    {
    //        //string exePath = Path.Combine(
    //        //    System.Environment.CurrentDirectory,
    //        //    AssetDatabase.GetAssetPath(vtfCmdExe.objectReferenceValue));
    //        //exePath = exePath.Replace('/', '\\');
    //        string exePath = @"C:\Users\jacky\Documents\Unity Projects\TF2Ls for Unity\Assets\TF2 Tools for Unity\VTFCmd.exe";
    //        string filePath = @"C:\Users\jacky\Documents\Unity Projects\TF2Ls for Unity\Assets\TF2 Tools for Unity\run.bat";
    //
    //        //string filePath = Path.Combine(
    //        //    exePath.Remove(exePath.IndexOf("VTFCmd.exe")),
    //        //    "run.bat");
    //        string assetPath = Path.Combine(System.Environment.CurrentDirectory, ctx.assetPath);
    //        assetPath = assetPath.Replace('/', '\\');
    //
    //        File.WriteAllText(filePath, string.Empty);
    //        StreamWriter writer = new StreamWriter(filePath, true);
    //        writer.WriteLine("\"" + exePath + "\"" +
    //            " -file \"" + assetPath + "\"" + " -exportformat \"png\"");
    //        writer.Close();
    //
    //        //UnityEditor.EditorApplication.update += CheckCommandShell;
    //
    //        //OnShellProcessComplete += () =>
    //        //{
    //        //    OnShellProcessComplete = null;
    //        //
    //        //    //Debug.Log(savedContext.assetPath);
    //        //    //ctx.AddObjectToAsset("Texture", tex);
    //        //    //ctx.SetMainObject(tex);
    //        //    Debug.Log("I did it!");
    //        //};
    //
    //        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(filePath);
    //        startInfo.CreateNoWindow = true;
    //        commandShell = new System.Diagnostics.Process();
    //        commandShell.StartInfo = startInfo;
    //        commandShell.Start();
    //
    //        System.Threading.Thread.Sleep(1000);
    //
    //        string pngPath = assetPath.Replace(".vtf", ".png");
    //        Texture2D tex = ReadTexture2DFromFilePath(pngPath);
    //        ctx.AddObjectToAsset("Texture", tex, tex);
    //
    //        File.Delete(pngPath);
    //    }
    //
    //    //void CheckCommandShell()
    //    //{
    //    //    if (commandShell != null)
    //    //    {
    //    //        if (commandShell.HasExited)
    //    //        {
    //    //            commandShell = null;
    //    //            UnityEditor.EditorApplication.update -= CheckCommandShell;
    //    //            OnShellProcessComplete?.Invoke();
    //    //        }
    //    //    }
    //    //}
    //
    //    Texture2D ReadTexture2DFromFilePath(string filePath)
    //    {
    //        var data = System.IO.File.ReadAllBytes(filePath);
    //        var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
    //        tex.LoadImage(data);
    //        return tex;
    //    }
    //}
}