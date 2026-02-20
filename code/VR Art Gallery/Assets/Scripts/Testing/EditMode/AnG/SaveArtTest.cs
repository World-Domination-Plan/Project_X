using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;


public class PaintableSurfaceRTTests
{
    string tempDir;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Application.dataPath, "Assets/Scripts/Testing/EditMode/AnG/TestPngs").Replace("\\", "/");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);
    }

    [Test]
    public void LoadCanvasFromPNG_AppliesTextureCorrectly()
    {
        // Arrange: set up surface
        var go = new GameObject("TestCanvas");
        var renderer = go.AddComponent<MeshRenderer>();
        go.AddComponent<MeshFilter>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        var surface = go.AddComponent<PaintableSurfaceRT>();
        surface.resolution = 1024;
        surface.Awake();

        // First save a PNG so we have something to load
        string fileName = "load_test.png";
        string fullPath = surface.SaveCanvasToPNG(fileName, tempDir);
        Assert.IsTrue(File.Exists(fullPath), "Prerequisite save failed.");

        // Act: load it back
        bool result = surface.LoadCanvasFromPNG(fullPath);

        // Assert
        Assert.IsTrue(result, "LoadCanvasFromPNG returned false.");

        // Assert: file loads with correct resolution
        byte[] bytes = File.ReadAllBytes(fullPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        Assert.AreEqual(surface.resolution, tex.width, "Loaded texture width mismatch.");
        Assert.AreEqual(surface.resolution, tex.height, "Loaded texture height mismatch.");

        // Assert: bad path returns false gracefully
        bool badResult = surface.LoadCanvasFromPNG("non/existent/path.png");
        Assert.IsFalse(badResult, "Should return false for missing file.");

        // Cleanup
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
        Directory.Delete(tempDir, true);
    }

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

