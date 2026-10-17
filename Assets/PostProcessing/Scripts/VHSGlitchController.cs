using System.Collections;
using UnityEngine;

public class VHSGlitchController : MonoBehaviour
{
    public VHSEffect vhsEffect;
    public float glitchInterval = 5f;
    public float glitchDuration = 0.2f;

    void Start()
    {
        if (vhsEffect == null)
            vhsEffect = GetComponent<VHSEffect>();

        StartCoroutine(GlitchRoutine());
    }

    IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(glitchInterval + Random.Range(-1f, 1f));

            // Сохраняем исходные значения
            float origJitter = vhsEffect.jitterAmount;
            float origSplit = vhsEffect.rgbSplit;
            float origSync = vhsEffect.syncOffset;

            // Глюк
            vhsEffect.jitterAmount = 0.04f + Random.Range(0, 0.02f);
            vhsEffect.rgbSplit = 0.03f + Random.Range(0, 0.02f);
            vhsEffect.syncOffset = Random.Range(-0.08f, 0.08f);

            yield return new WaitForSeconds(glitchDuration);

            // Возвращаем
            vhsEffect.jitterAmount = origJitter;
            vhsEffect.rgbSplit = origSplit;
            vhsEffect.syncOffset = origSync;
        }
    }
}