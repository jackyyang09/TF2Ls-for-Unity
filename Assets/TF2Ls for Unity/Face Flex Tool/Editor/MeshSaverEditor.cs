using UnityEditor;
using UnityEngine;

namespace TF2Ls.FaceFlex
{
	/// <summary>
	/// Modified code borrowed here
	/// https://github.com/pharan/Unity-MeshSaver
	/// Mesh file sizes created with this is massive, not recommended
	/// </summary>
	public static class MeshSaverUtility
	{
		public static Object SaveMesh(Mesh mesh, string path)
		{
			if (string.IsNullOrEmpty(path)) return null;

			Mesh meshToSave = Object.Instantiate(mesh) as Mesh;

			MeshUtility.Optimize(meshToSave);

			AssetDatabase.CreateAsset(meshToSave, path);
			AssetDatabase.SaveAssets();
			return AssetDatabase.LoadAssetAtPath<Mesh>(path);
		}
	}
}