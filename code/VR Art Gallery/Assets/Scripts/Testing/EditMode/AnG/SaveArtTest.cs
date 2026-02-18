using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;


public class PaintableSurfaceRTTests
{
    [Test]
    public void SaveCanvasToPNG_CreatesPNGFile()
    {
        var go = new GameObject("TestCanvas");
        var renderer = go.AddComponent<MeshRenderer>();
        go.AddComponent<MeshFilter>(); 

        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        var surface = go.AddComponent<PaintableSurfaceRT>();
        surface.resolution = 1024;
        
        surface.Awake();
        
        string tempDir = Path.Combine(Application.dataPath, "../TempPaintTests");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        string fileName = "test_painting.png";
        string fullPath = surface.SaveCanvasToPNG(fileName, tempDir);

        Assert.IsTrue(File.Exists(fullPath), "PNG file was not created.");
        byte[] bytes = File.ReadAllBytes(fullPath);
        Assert.Greater(bytes.Length, 0, "PNG file is empty.");

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        Assert.AreEqual(surface.resolution, tex.width, "PNG width mismatch.");
        Assert.AreEqual(surface.resolution, tex.height, "PNG height mismatch.");

        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
        Directory.Delete(tempDir, true);
    }
}

