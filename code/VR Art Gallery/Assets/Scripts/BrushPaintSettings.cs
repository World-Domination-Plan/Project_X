using UnityEngine;

[System.Serializable]
public struct BrushPaintSettings
{
    public Color color;
    public float radius;
    public float hardness;

    public BrushPaintSettings(Color color, float radius, float hardness)
    {
        this.color = color;
        this.radius = radius;
        this.hardness = hardness;
    }
}