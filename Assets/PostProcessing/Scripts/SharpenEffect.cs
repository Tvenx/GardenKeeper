using UnityEngine;

[ExecuteInEditMode]
public class SharpenEffect : MonoBehaviour
{
    public Material material;
    [Range(0, 100)] public float amount = 0.5f;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        material.SetFloat("_Amount", amount);
        Graphics.Blit(src, dest, material);
    }
}
