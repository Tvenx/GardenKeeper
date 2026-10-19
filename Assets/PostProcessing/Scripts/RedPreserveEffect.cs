using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RedPreserveEffect : MonoBehaviour
{
    public Shader shader;
    [Range(0, 1)] public float redThreshold = 0.1f;
    [Range(0.01f, 0.5f)] public float redSoftness = 0.1f;

    private Material _material;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (shader == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        if (_material == null)
        {
            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
        }

        _material.SetFloat("_RedThreshold", redThreshold);
        _material.SetFloat("_RedSoftness", redSoftness);

        Graphics.Blit(src, dest, _material);
    }

    void OnDisable()
    {
        if (_material != null)
            DestroyImmediate(_material);
    }
}