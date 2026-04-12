using System.Collections.Generic;
using UnityEngine;

public class SharedBrushSettings : MonoBehaviour
{
    public static SharedBrushSettings Instance { get; private set; }

    [Header("Shared Defaults")]
    [SerializeField] private Color currentColor = Color.black;
    [SerializeField, Range(0.001f, 0.25f)] private float currentRadius = 0.03f;
    [SerializeField, Range(0f, 1f)] private float currentHardness = 0.7f;

    private readonly List<BrushToolState> registeredBrushes = new();

    public Color CurrentColor => currentColor;
    public float CurrentRadius => currentRadius;
    public float CurrentHardness => currentHardness;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterBrush(BrushToolState brush)
    {
        if (brush == null || registeredBrushes.Contains(brush))
            return;

        registeredBrushes.Add(brush);
        ApplyToBrush(brush);
    }

    public void UnregisterBrush(BrushToolState brush)
    {
        if (brush == null)
            return;

        registeredBrushes.Remove(brush);
    }

    public void SetColor(Color color)
    {
        Debug.Log($"[SharedBrushSettings] SetColor alpha = {color.a}");
        currentColor = color;
        ApplyToAllBrushes();
    }

    public void SetRadius(float radius)
    {
        currentRadius = Mathf.Clamp(radius, 0.001f, 0.25f);
        ApplyToAllBrushes();
    }

    public void SetHardness(float hardness)
    {
        currentHardness = Mathf.Clamp01(hardness);
        ApplyToAllBrushes();
    }

    public BrushState GetBrushState()
    {
        return new BrushState(currentColor, currentRadius, currentHardness);
    }

    private void ApplyToAllBrushes()
    {
        for (int i = registeredBrushes.Count - 1; i >= 0; i--)
        {
            if (registeredBrushes[i] == null)
            {
                registeredBrushes.RemoveAt(i);
                continue;
            }

            ApplyToBrush(registeredBrushes[i]);
        }
    }

    private void ApplyToBrush(BrushToolState brush)
    {
        brush.ApplySharedSettings(currentColor, currentRadius, currentHardness);
    }
}