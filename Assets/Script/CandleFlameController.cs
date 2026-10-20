using UnityEngine;

[DisallowMultipleComponent]
public sealed class CandleFlameController : MonoBehaviour
{
    [SerializeField] private Transform flamePoint;
    [SerializeField] private SpriteRenderer flameSpriteRenderer;
    [SerializeField] private Light flameLight;
    [SerializeField] private Sprite[] flameFrames = new Sprite[0];

    [Header("Frame Animation")]
    [Min(0.02f)]
    [SerializeField] private float minFrameInterval = 0.18f;

    [Min(0.02f)]
    [SerializeField] private float maxFrameInterval = 0.26f;

    [SerializeField] private Vector3 defaultSpriteScale =
        new Vector3(0.04f, 0.05f, 0.12f);

    [SerializeField] private float defaultSpriteLocalY = 0.4f;
    [SerializeField] private float minimumSpriteLocalY = -0.3f;

    [SerializeField] private Color monochromeFrameTint = Color.white;

    [Header("Flame Light")]
    [Min(0f)]
    [SerializeField] private float baseLightIntensity = 1.5f;

    [Min(0f)]
    [SerializeField] private float baseLightRange = 2.2f;

    [SerializeField] private Color lightColor = Color.white;

    private bool isInitialized;
    private float burnProgress = 1f;
    private float nextFrameChangeTime;
    private float lightFlickerOffset;
    private int currentFrameIndex = -1;
    private Vector3 baseSpriteScale;
    private int[] shuffledFrameOrder = new int[0];
    private int nextFrameInOrder;

    private void Awake()
    {
        Initialize();
        ApplyBurnProgress();
    }

    private void OnEnable()
    {
        Initialize();
        ApplyBurnProgress();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (flameFrames != null &&
            flameFrames.Length > 1 &&
            Time.time >= nextFrameChangeTime)
        {
            ChangeFrame();
        }

        UpdateLightIntensity();
    }

    private void LateUpdate()
    {
        if (flameSpriteRenderer == null || !flameSpriteRenderer.enabled)
        {
            return;
        }

        Camera targetCamera = Camera.main;

        if (targetCamera == null)
        {
            return;
        }

        Vector3 direction =
            targetCamera.transform.position -
            flameSpriteRenderer.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            flameSpriteRenderer.transform.rotation =
                Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    public void SetFlameFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        flameFrames = frames;

        if (isInitialized)
        {
            currentFrameIndex = -1;
            shuffledFrameOrder = new int[0];
            nextFrameInOrder = 0;
            ChangeFrame();
            ApplyBurnProgress();
        }
    }

    public void SetBurnProgress(float progress)
    {
        burnProgress = Mathf.Clamp01(progress);
        Initialize();
        ApplyBurnProgress();
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (flamePoint == null)
        {
            flamePoint = transform.Find("FlamePoint");

            if (flamePoint == null)
            {
                GameObject point = new GameObject("FlamePoint");
                flamePoint = point.transform;
                flamePoint.SetParent(transform, false);
                flamePoint.localPosition = new Vector3(0f, 1.03f, 0f);
                flamePoint.localRotation = Quaternion.identity;
            }
        }

        if (flameSpriteRenderer == null)
        {
            flameSpriteRenderer =
                flamePoint.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (flameSpriteRenderer == null)
        {
            GameObject visual = new GameObject("FlameSprite");
            visual.transform.SetParent(flamePoint, false);
            visual.transform.localScale = defaultSpriteScale;
            flameSpriteRenderer = visual.AddComponent<SpriteRenderer>();
        }

        Vector3 spriteLocalPosition =
            flameSpriteRenderer.transform.localPosition;
        spriteLocalPosition.y = defaultSpriteLocalY;
        flameSpriteRenderer.transform.localPosition = spriteLocalPosition;

        baseSpriteScale = flameSpriteRenderer.transform.localScale;

        if (baseSpriteScale.sqrMagnitude < 0.0001f)
        {
            baseSpriteScale = defaultSpriteScale;
            flameSpriteRenderer.transform.localScale = baseSpriteScale;
        }

        flameSpriteRenderer.sortingOrder = 10;
        flameSpriteRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        flameSpriteRenderer.receiveShadows = false;

        if (flameLight == null)
        {
            flameLight = flamePoint.GetComponentInChildren<Light>(true);
        }

        if (flameLight == null)
        {
            GameObject lightObject = new GameObject("FlameLight");
            lightObject.transform.SetParent(flamePoint, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            flameLight = lightObject.AddComponent<Light>();
        }

        flameLight.type = LightType.Point;
        flameLight.color = lightColor;
        flameLight.shadows = LightShadows.None;
        flameLight.range = baseLightRange;
        flameLight.intensity = baseLightIntensity;

        lightFlickerOffset = Random.Range(0f, 1000f);
        isInitialized = true;

        if (flameFrames != null && flameFrames.Length > 0)
        {
            ChangeFrame();
        }
        else
        {
            ScheduleNextFrame();
        }
    }

    private void ChangeFrame()
    {
        if (flameFrames == null || flameFrames.Length == 0)
        {
            return;
        }

        if (flameFrames.Length == 1)
        {
            currentFrameIndex = 0;
        }
        else
        {
            if (shuffledFrameOrder == null ||
                shuffledFrameOrder.Length != flameFrames.Length ||
                nextFrameInOrder >= shuffledFrameOrder.Length)
            {
                PrepareShuffledFrameOrder();
            }

            currentFrameIndex = shuffledFrameOrder[nextFrameInOrder];
            nextFrameInOrder++;
        }

        ApplyCurrentFrame();
        ScheduleNextFrame();
    }

    private void PrepareShuffledFrameOrder()
    {
        shuffledFrameOrder = new int[flameFrames.Length];

        for (int i = 0; i < shuffledFrameOrder.Length; i++)
        {
            shuffledFrameOrder[i] = i;
        }

        for (int i = shuffledFrameOrder.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int frameIndex = shuffledFrameOrder[i];
            shuffledFrameOrder[i] = shuffledFrameOrder[swapIndex];
            shuffledFrameOrder[swapIndex] = frameIndex;
        }

        if (currentFrameIndex >= 0 &&
            shuffledFrameOrder.Length > 1 &&
            shuffledFrameOrder[0] == currentFrameIndex)
        {
            int swapIndex = Random.Range(1, shuffledFrameOrder.Length);
            int frameIndex = shuffledFrameOrder[0];
            shuffledFrameOrder[0] = shuffledFrameOrder[swapIndex];
            shuffledFrameOrder[swapIndex] = frameIndex;
        }

        nextFrameInOrder = 0;
    }

    private void ApplyCurrentFrame()
    {
        if (flameSpriteRenderer == null ||
            flameFrames == null ||
            flameFrames.Length == 0 ||
            currentFrameIndex < 0)
        {
            return;
        }

        flameSpriteRenderer.sprite = flameFrames[currentFrameIndex];

        bool isMonochromeFrame =
            currentFrameIndex == 1 || currentFrameIndex == 3;

        Color tint = isMonochromeFrame
            ? monochromeFrameTint
            : Color.white;

        tint.a = burnProgress;
        flameSpriteRenderer.color = tint;
    }

    private void ScheduleNextFrame()
    {
        float minimum = Mathf.Max(0.02f, minFrameInterval);
        float maximum = Mathf.Max(minimum, maxFrameInterval);
        nextFrameChangeTime =
            Time.time + Random.Range(minimum, maximum);
    }

    private void ApplyBurnProgress()
    {
        if (!isInitialized)
        {
            return;
        }

        float progress = Mathf.Clamp01(burnProgress);
        bool flameIsVisible =
            progress > 0.001f &&
            flameSpriteRenderer != null &&
            flameFrames != null &&
            flameFrames.Length > 0;

        if (flameSpriteRenderer != null)
        {
            Vector3 spriteLocalPosition =
                flameSpriteRenderer.transform.localPosition;
            spriteLocalPosition.y = Mathf.Lerp(
                minimumSpriteLocalY,
                defaultSpriteLocalY,
                progress
            );
            flameSpriteRenderer.transform.localPosition =
                spriteLocalPosition;

            flameSpriteRenderer.enabled = flameIsVisible;

            if (flameIsVisible)
            {
                flameSpriteRenderer.transform.localScale =
                    baseSpriteScale * progress;

                Color tint = currentFrameIndex == 1 ||
                             currentFrameIndex == 3
                    ? monochromeFrameTint
                    : Color.white;

                tint.a = progress;
                flameSpriteRenderer.color = tint;
            }
        }

        if (flameLight != null)
        {
            flameLight.enabled = progress > 0.001f;
            flameLight.range = baseLightRange *
                Mathf.Lerp(0.2f, 1f, progress);
            UpdateLightIntensity();
        }
    }

    private void UpdateLightIntensity()
    {
        if (flameLight == null || !flameLight.enabled)
        {
            return;
        }

        float noise = Mathf.PerlinNoise(
            Time.time * 11f,
            lightFlickerOffset
        );
        float flicker = Mathf.Lerp(0.9f, 1.08f, noise);

        flameLight.intensity =
            baseLightIntensity * burnProgress * flicker;
    }
}