using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorldInteractionPrompt : MonoBehaviour
{
    private const int CanvasWidth = 320;
    private const int CanvasHeight = 76;
    private const float WorldScale = 0.0035f;

    private static Sprite capsuleSprite;

    private Transform target;
    private GameObject visualRoot;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;
    private Text keyLabel;
    private Text actionLabel;
    private Vector3 localAnchor;
    private Camera targetCamera;
    private bool isVisible;

    public void Initialize(Transform followTarget)
    {
        target = followTarget;

        if (visualRoot == null)
        {
            BuildVisual();
        }

        localAnchor = CalculateAnchor(target);
        UpdateWorldPosition();
    }

    public void SetPrompt(string action, string key)
    {
        if (actionLabel != null)
        {
            string nextAction = string.IsNullOrWhiteSpace(action)
                ? "ВЗАИМОДЕЙСТВОВАТЬ"
                : action.ToUpperInvariant();

            if (actionLabel.text != nextAction)
            {
                actionLabel.text = nextAction;
            }
        }

        if (keyLabel != null)
        {
            string nextKey = string.IsNullOrWhiteSpace(key) ? "E" : key;

            if (keyLabel.text != nextKey)
            {
                keyLabel.text = nextKey;
            }

            keyLabel.fontSize = nextKey.Length <= 1
                ? 24
                : nextKey.Length <= 3
                    ? 18
                    : 13;
            keyLabel.resizeTextForBestFit = false;
            keyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            keyLabel.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    public void SetVisible(bool visible)
    {
        if (visible && !enabled)
        {
            enabled = true;
        }

        isVisible = visible;

        if (visible && visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }
    }

    private void Update()
    {
        if (canvasGroup == null || canvasRect == null)
        {
            return;
        }

        if (!isVisible && canvasGroup.alpha <= 0.001f)
        {
            if (visualRoot.activeSelf)
            {
                visualRoot.SetActive(false);
            }

            enabled = false;
            return;
        }

        float targetAlpha = isVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * 8f
        );

        float targetScale = isVisible ? 1f : 0.92f;
        float currentScale = canvasRect.localScale.x / WorldScale;
        float nextScale = Mathf.Lerp(
            currentScale,
            targetScale,
            Time.deltaTime * 10f
        );

        canvasRect.localScale = Vector3.one * (WorldScale * nextScale);

        if (!isVisible &&
            canvasGroup.alpha <= 0.001f &&
            visualRoot.activeSelf)
        {
            visualRoot.SetActive(false);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!isVisible || visualRoot == null || !visualRoot.activeSelf)
        {
            return;
        }

        UpdateWorldPosition();
        FaceCamera();
    }

    private void OnDisable()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }
    }

    private void BuildVisual()
    {
        visualRoot = new GameObject("Interaction Prompt");
        canvasRect = visualRoot.AddComponent<RectTransform>();

        Canvas canvas = visualRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 25;
        canvas.worldCamera = Camera.main;

        canvasGroup = visualRoot.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        canvasRect.localScale = Vector3.one * (WorldScale * 0.92f);

        Image background = CreateImage(
            "Pill Background",
            canvasRect,
            new Vector2(CanvasWidth, CanvasHeight),
            new Color(0.055f, 0.085f, 0.11f, 0.94f)
        );

        background.sprite = GetCapsuleSprite();
        background.type = Image.Type.Simple;

        Image keyBackground = CreateImage(
            "Key Background",
            canvasRect,
            new Vector2(52f, 52f),
            new Color(0.91f, 0.72f, 0.42f, 1f)
        );

        keyBackground.sprite = GetCapsuleSprite();
        keyBackground.type = Image.Type.Simple;
        keyBackground.rectTransform.anchoredPosition =
            new Vector2(-116f, 0f);

        keyLabel = CreateText(
            "Key",
            canvasRect,
            new Vector2(52f, 52f),
            new Vector2(-116f, 0f),
            24,
            FontStyle.Bold,
            new Color(0.09f, 0.12f, 0.14f, 1f),
            TextAnchor.MiddleCenter
        );

        keyLabel.text = "E";
        keyLabel.resizeTextForBestFit = false;
        keyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        keyLabel.verticalOverflow = VerticalWrapMode.Overflow;

        actionLabel = CreateText(
            "Action",
            canvasRect,
            new Vector2(220f, 58f),
            new Vector2(27f, 0f),
            22,
            FontStyle.Bold,
            Color.white,
            TextAnchor.MiddleCenter
        );

        actionLabel.text = "ВЗАИМОДЕЙСТВОВАТЬ";
        actionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        actionLabel.verticalOverflow = VerticalWrapMode.Truncate;

        targetCamera = Camera.main;
        visualRoot.SetActive(false);
    }

    private void UpdateWorldPosition()
    {
        if (visualRoot == null || target == null)
        {
            return;
        }

        float floatOffset = Mathf.Sin(Time.time * 1.6f) * 0.025f;
        visualRoot.transform.position =
            target.TransformPoint(localAnchor) + Vector3.up * floatOffset;
    }

    private void FaceCamera()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 directionToCamera =
            targetCamera.transform.position - visualRoot.transform.position;

        // Keep the sign upright and face the readable side of the
        // world-space Canvas toward the camera.
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.0001f)
        {
            visualRoot.transform.rotation = Quaternion.LookRotation(
                -directionToCamera,
                Vector3.up
            );
        }
    }

    private static Vector3 CalculateAnchor(Transform root)
    {
        if (root == null)
        {
            return Vector3.up * 1.8f;
        }

        Bounds bounds = new Bounds();
        bool foundBounds = false;

        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        foreach (Collider item in colliders)
        {
            if (item == null || !item.enabled)
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds = item.bounds;
                foundBounds = true;
            }
            else
            {
                bounds.Encapsulate(item.bounds);
            }
        }

        if (!foundBounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            foreach (Renderer item in renderers)
            {
                if (item == null || !item.enabled)
                {
                    continue;
                }

                if (!foundBounds)
                {
                    bounds = item.bounds;
                    foundBounds = true;
                }
                else
                {
                    bounds.Encapsulate(item.bounds);
                }
            }
        }

        if (!foundBounds)
        {
            return Vector3.up * 1.8f;
        }

        Vector3 worldAnchor =
            bounds.center + Vector3.up * (bounds.extents.y + 0.25f);

        return root.InverseTransformPoint(worldAnchor);
    }

    private static Image CreateImage(
        string objectName,
        Transform parent,
        Vector2 size,
        Color color)
    {
        GameObject child = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        child.transform.SetParent(parent, false);

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = child.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        Vector2 size,
        Vector2 position,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment)
    {
        GameObject child = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Text)
        );

        child.transform.SetParent(parent, false);

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = child.GetComponent<Text>();
        text.font = GetRuntimeFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, fontSize - 5);
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static Font GetRuntimeFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Arial", "Segoe UI" },
                24
            );
        }

        return font;
    }

    private static Sprite GetCapsuleSprite()
    {
        if (capsuleSprite != null)
        {
            return capsuleSprite;
        }

        const int width = 128;
        const int height = 64;
        const float radius = 31f;
        const float centerY = height / 2f;

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );

        texture.name = "Interaction Prompt Capsule";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nearestX = Mathf.Clamp(x, radius, width - radius);
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(nearestX, centerY)
                );

                byte alpha = (byte)(
                    Mathf.Clamp01(radius + 0.5f - distance) * 255f
                );

                pixels[y * width + x] = new Color32(
                    255,
                    255,
                    255,
                    alpha
                );
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        capsuleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(31f, 31f, 31f, 31f)
        );

        capsuleSprite.name = "Interaction Prompt Capsule";
        return capsuleSprite;
    }
}
