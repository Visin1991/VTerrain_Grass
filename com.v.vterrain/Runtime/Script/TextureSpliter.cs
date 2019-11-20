using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureSpliter : MonoBehaviour
{
    //public Texture2D tex2d;
    public List<Texture2D> chunks;
    public Texture2D densityMap;


    [ContextMenu("Process")]
    public void SplitTextureTo_8x8()
    {
        if (chunks != null)
        {
            for (int i = chunks.Count - 1; i > 0; i--)
            {
                GameObject.DestroyImmediate(chunks[i]);
            }
            chunks.Clear();
        }
        else
        {
            chunks = new List<Texture2D>();
        }

        //if (tex2d) { GameObject.DestroyImmediate(tex2d); }
        //tex2d = new Texture2D(densityMap.width, densityMap.height);

        for (int y = 0; y < densityMap.height / 8; y++)
        {
            for (int x = 0; x < densityMap.width / 8; x++)
            {
                Color[] pixels = densityMap.GetPixels(x * 8, y * 8, 8, 8);
                Texture2D _8x8 = new Texture2D(8, 8);
                _8x8.SetPixels(0,0,8,8,pixels);
                _8x8.Apply(false);
                chunks.Add(_8x8);
            }
        }


        //tex2d.Apply(false);

#if UNITY_EDITOR
        //string path = EditorUtility.SaveFilePanel("Save To PNG", System.IO.Path.GetFullPath("Assets").Replace('\\', '/'), "", "png");
        //WriteTextureToDisk(tex2d, ToUnity(path));

        string folder = EditorUtility.SaveFolderPanel("Save Chunks", System.IO.Path.GetFullPath("Assets").Replace('\\', '/'), "");
        for (int i = 0; i < chunks.Count; i++)
        {
            WriteTextureToDisk(chunks[i], folder + "/Density_" + i + ".png");
        }
        AssetDatabase.Refresh();
#endif
    }



#if UNITY_EDITOR

    public string ToUnity(string path)
    {
        path = path.Replace('\\', '/');
        string workSpace = System.IO.Path.GetFullPath("Assets").Replace('\\', '/');
        if (path.Contains(workSpace))
            return "Assets" + path.Substring(workSpace.Length);
        else
            return null;
    }

    public Texture2D WriteTextureToDisk(Texture2D textureCache, string targetPath)
    {
        if (textureCache == null) { return null; }
        System.IO.File.WriteAllBytes(targetPath, ImageConversion.EncodeToPNG(textureCache));
        AssetDatabase.ImportAsset(targetPath);
        Object.DestroyImmediate(textureCache);        
        return AssetDatabase.LoadMainAssetAtPath(targetPath) as Texture2D;
    }
#endif

}
