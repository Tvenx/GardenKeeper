using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class LowBitrateEffect : MonoBehaviour
{
    [Header("Basic Settings")]
    [Range(8, 4096)]
    public float blockCount = 64f;

    [Range(2, 64)]
    public float colorDepth = 8f;

    [Header("Artifact Settings")]
    [Range(0f, 1f)]
    public float compressionArtifacts = 0.5f;

    [Range(0f, 1f)]
    public float chromaSubsampling = 0.3f;

    [Header("Noise Settings")]
    public Texture2D noiseTexture;
    [Range(0f, 0.5f)]
    public float noiseIntensity = 0.1f;
    [Range(1f, 10f)]
    public float noiseTiling = 4f;

    [Header("Animation")]
    public bool animateEffect = false;
    [Range(0f, 5f)]
    public float animationSpeed = 1f;
    [Range(0f, 1f)]
    public float animationIntensity = 0.1f;

    private Material effectMaterial;
    private float timeCounter = 0f;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial == null)
        {
            Shader shader = Shader.Find("Custom/LowBitrate");
            if (shader == null)
            {
                Debug.LogError("LowBitrate shader not found!");
                Graphics.Blit(source, destination);
                return;
            }
            effectMaterial = new Material(shader);
        }

        // Устанавливаем параметры
        effectMaterial.SetFloat("_BlockCount", blockCount);
        effectMaterial.SetFloat("_ColorDepth", colorDepth);
        effectMaterial.SetFloat("_CompressionArtifacts", compressionArtifacts);
        effectMaterial.SetFloat("_ChromaSubsampling", chromaSubsampling);
        effectMaterial.SetFloat("_NoiseIntensity", noiseIntensity);
        effectMaterial.SetFloat("_NoiseTiling", noiseTiling);

        if (noiseTexture != null)
        {
            effectMaterial.SetTexture("_NoiseTex", noiseTexture);
        }

        // Анимация эффекта (мерцание параметров)
        if (animateEffect && Application.isPlaying)
        {
            timeCounter += Time.deltaTime * animationSpeed;
            float variation = Mathf.Sin(timeCounter) * animationIntensity;

            effectMaterial.SetFloat("_BlockCount", blockCount * (1f + variation * 0.2f));
            effectMaterial.SetFloat("_NoiseIntensity", noiseIntensity * (1f + variation));
        }

        Graphics.Blit(source, destination, effectMaterial);
    }

    void OnDisable()
    {
        if (effectMaterial != null)
        {
            DestroyImmediate(effectMaterial);
        }
    }

    // Методы для быстрой настройки пресетов
    public void SetVHSPreset()
    {
        blockCount = 48f;
        colorDepth = 4f;
        compressionArtifacts = 0.8f;
        chromaSubsampling = 0.7f;
        noiseIntensity = 0.2f;
        animateEffect = true;
    }

    public void SetWebStreamPreset()
    {
        blockCount = 96f;
        colorDepth = 6f;
        compressionArtifacts = 0.6f;
        chromaSubsampling = 0.5f;
        noiseIntensity = 0.05f;
        animateEffect = false;
    }

    public void SetDigitalArtifactPreset()
    {
        blockCount = 32f;
        colorDepth = 3f;
        compressionArtifacts = 1f;
        chromaSubsampling = 0.9f;
        noiseIntensity = 0.3f;
        animateEffect = true;
        animationSpeed = 2f;
    }
}