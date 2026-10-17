using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class VHSEffect : MonoBehaviour
{
    [Header("=== SCANLINES ===")]
    [Range(0, 1)] public float scanlineIntensity = 0.3f;
    [Range(1, 10)] public float scanlineSize = 3f;
    [Range(-5, 5)] public float scanlineSpeed = 0.5f;

    [Header("=== NOISE ===")]
    public Texture2D noiseTexture;
    [Range(0, 0.5f)] public float noiseIntensity = 0.1f;
    [Range(0.1f, 10f)] public float noiseScale = 1f;
    [Range(-10, 10)] public float noiseSpeed = 2f;
    [Range(0, 1)] public float noiseThreshold = 0.95f;

    [Header("=== JITTER ===")]
    [Range(0, 0.05f)] public float jitterAmount = 0.02f;
    [Range(0, 10)] public float jitterSpeed = 3f;

    [Header("=== RGB SPLIT ===")]
    [Range(0, 0.05f)] public float rgbSplit = 0.01f;
    [Range(0, 10)] public float rgbSplitSpeed = 2f;

    [Header("=== VIGNETTE ===")]
    [Range(0, 1)] public float vignetteIntensity = 0.5f;
    [Range(0, 1)] public float vignetteRadius = 0.8f;

    [Header("=== SYNC LOSS ===")]
    [Range(-0.1f, 0.1f)] public float syncOffset = 0f;
    [Range(0, 10)] public float syncSpeed = 3f;
    [Range(0, 1)] public float syncThreshold = 0.95f;

    [Header("=== BRIGHTNESS ===")]
    [Range(0, 5)] public float brightnessSpeed = 1.3f;
    [Range(0, 0.2f)] public float brightnessAmount = 0.1f;

    [Header("=== GLOBAL ===")]
    [Range(0, 5)] public float globalTimeScale = 1f;

    private Material material;
    private Shader shader;

    private Texture2D GenerateDefaultNoise()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++)
        {
            float val = Random.Range(0f, 1f);
            colors[i] = new Color(val, val, val, 1f);
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    void Awake()
    {
        shader = Shader.Find("Custom/VHSEffect");
        if (shader == null)
        {
            Debug.LogError("VHS Shader not found!");
            enabled = false;
            return;
        }

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;

        if (noiseTexture == null)
        {
            noiseTexture = GenerateDefaultNoise();
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        if (noiseTexture != null)
            material.SetTexture("_NoiseTexture", noiseTexture);

        material.SetFloat("_ScanlineIntensity", scanlineIntensity);
        material.SetFloat("_ScanlineSize", scanlineSize);
        material.SetFloat("_ScanlineSpeed", scanlineSpeed);
        material.SetFloat("_NoiseIntensity", noiseIntensity);
        material.SetFloat("_NoiseScale", noiseScale);
        material.SetFloat("_NoiseSpeed", noiseSpeed);
        material.SetFloat("_NoiseThreshold", noiseThreshold);
        material.SetFloat("_JitterAmount", jitterAmount);
        material.SetFloat("_JitterSpeed", jitterSpeed);
        material.SetFloat("_RGBSplit", rgbSplit);
        material.SetFloat("_RGBSplitSpeed", rgbSplitSpeed);
        material.SetFloat("_VignetteIntensity", vignetteIntensity);
        material.SetFloat("_VignetteRadius", vignetteRadius);
        material.SetFloat("_SyncOffset", syncOffset);
        material.SetFloat("_SyncSpeed", syncSpeed);
        material.SetFloat("_SyncThreshold", syncThreshold);
        material.SetFloat("_BrightnessSpeed", brightnessSpeed);
        material.SetFloat("_BrightnessAmount", brightnessAmount);
        material.SetFloat("_GlobalTimeScale", globalTimeScale);

        Graphics.Blit(src, dest, material);
    }

    void OnDestroy()
    {
        if (material != null)
        {
            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }
    }

    void OnValidate()
    {
        if (material != null)
        {
            if (noiseTexture != null)
                material.SetTexture("_NoiseTexture", noiseTexture);

            material.SetFloat("_ScanlineIntensity", scanlineIntensity);
            material.SetFloat("_ScanlineSize", scanlineSize);
            material.SetFloat("_ScanlineSpeed", scanlineSpeed);
            material.SetFloat("_NoiseIntensity", noiseIntensity);
            material.SetFloat("_NoiseScale", noiseScale);
            material.SetFloat("_NoiseSpeed", noiseSpeed);
            material.SetFloat("_NoiseThreshold", noiseThreshold);
            material.SetFloat("_JitterAmount", jitterAmount);
            material.SetFloat("_JitterSpeed", jitterSpeed);
            material.SetFloat("_RGBSplit", rgbSplit);
            material.SetFloat("_RGBSplitSpeed", rgbSplitSpeed);
            material.SetFloat("_VignetteIntensity", vignetteIntensity);
            material.SetFloat("_VignetteRadius", vignetteRadius);
            material.SetFloat("_SyncOffset", syncOffset);
            material.SetFloat("_SyncSpeed", syncSpeed);
            material.SetFloat("_SyncThreshold", syncThreshold);
            material.SetFloat("_BrightnessSpeed", brightnessSpeed);
            material.SetFloat("_BrightnessAmount", brightnessAmount);
            material.SetFloat("_GlobalTimeScale", globalTimeScale);
        }
    }

    [ContextMenu("Generate New Noise Texture")]
    public void GenerateNewNoise()
    {
        noiseTexture = GenerateDefaultNoise();
        if (material != null)
            material.SetTexture("_NoiseTexture", noiseTexture);
        Debug.Log("New noise texture generated!");
    }
}