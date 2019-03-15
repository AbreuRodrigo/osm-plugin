/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System.IO;
using UnityEditor;
using UnityEngine;

public static class OnlineMapsEditorUtils
{
    private static string _assetPath;

    public static string assetPath
    {
        get
        {
            if (_assetPath == null)
            {
                string[] dirs = Directory.GetDirectories("Assets", "Online maps", SearchOption.AllDirectories);
                _assetPath = dirs.Length > 0 ? dirs[0] : string.Empty;
            }
            return _assetPath;
        }
    }

    public static void CheckMarkerTextureImporter(SerializedProperty property)
    {
        Texture2D texture = property.objectReferenceValue as Texture2D;
        CheckMarkerTextureImporter(texture);
    }

    public static void CheckMarkerTextureImporter(Texture2D texture)
    {
        if (texture == null) return;

        string textureFilename = AssetDatabase.GetAssetPath(texture.GetInstanceID());
        TextureImporter textureImporter = AssetImporter.GetAtPath(textureFilename) as TextureImporter;
        if (textureImporter == null) return;

        bool needReimport = false;
        if (textureImporter.mipmapEnabled)
        {
            textureImporter.mipmapEnabled = false;
            needReimport = true;
        }
        if (!textureImporter.isReadable)
        {
            textureImporter.isReadable = true;
            needReimport = true;
        }
        if (textureImporter.textureCompression != TextureImporterCompression.Uncompressed)
        {
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            needReimport = true;
        }

        if (needReimport) AssetDatabase.ImportAsset(textureFilename, ImportAssetOptions.ForceUpdate);
    }

    public static void ImportPackage(string path, Warning warning = null, string errorMessage = null)
    {
        if (warning != null && !warning.Show()) return;
        if (string.IsNullOrEmpty(assetPath))
        {
            if (!string.IsNullOrEmpty(errorMessage)) Debug.LogError(errorMessage);
            return;
        }

        string filaname = assetPath + "\\" + path;
        if (!File.Exists(filaname))
        {
            if (!string.IsNullOrEmpty(errorMessage)) Debug.LogError(errorMessage);
            return;
        }

        AssetDatabase.ImportPackage(filaname, true);
    }

    public static T LoadAsset<T>(string path, bool throwOnMissed = false) where T : Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            if (throwOnMissed) throw new FileNotFoundException(assetPath);
            return default(T);
        }
        string filename = assetPath + "\\" + path;
        if (!File.Exists(filename))
        {
            if (throwOnMissed) throw new FileNotFoundException(assetPath);
            return default(T);
        }
        return (T)AssetDatabase.LoadAssetAtPath(filename, typeof(T));
    }

    public class Warning
    {
        public string title = "Warning";
        public string message;
        public string ok = "OK";
        public string cancel = "Cancel";

        public bool Show()
        {
            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }
    }
}