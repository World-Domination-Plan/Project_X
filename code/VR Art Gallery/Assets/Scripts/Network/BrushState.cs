using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct BrushState : INetworkSerializable
{
    public Color color;
    public float radius;
    public float hardness;

    public BrushState(Color color, float radius, float hardness)
    {
        this.color = color;
        this.radius = radius;
        this.hardness = hardness;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        float r = color.r;
        float g = color.g;
        float b = color.b;
        float a = color.a;

        serializer.SerializeValue(ref r);
        serializer.SerializeValue(ref g);
        serializer.SerializeValue(ref b);
        serializer.SerializeValue(ref a);
        serializer.SerializeValue(ref radius);
        serializer.SerializeValue(ref hardness);

        if (serializer.IsReader)
            color = new Color(r, g, b, a);
    }
}
