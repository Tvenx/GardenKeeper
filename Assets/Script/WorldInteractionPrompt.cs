using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorldInteractionPrompt : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text keyLabel;
    [SerializeField] private Text actionLabel;
    [SerializeField, Min(0.001f)] private float worldScale = 0.0035f;

    private Camera targetCamera;
    private bool isVisible;

    public bool Initialize()
    {
        if (visualRoot == null ||
            canvasRect == null ||
            canvasGroup == null ||
            keyLabel == null ||
            actionLabel == null)
        {
            Debug.LogError(
                $"{nameof(WorldInteractionPrompt)} on {name} is missing " +
                "its prefab UI references. Assign the panel, CanvasGroup, " +
                "and both labels in the Inspector.",
                this
            );
            return false;
        }

        Canvas canvas = visualRoot.GetComponent<Canvas>();
        targetCamera = Camera.main;
        if (canvas != null)
        {
            canvas.worldCamera = targetCamera;
        }

        canvasGroup.alpha = 0f;
        SetWorldScale(worldScale * 0.92f);
        visualRoot.SetActive(false);

        return true;
    }

    public void SetPrompt(string action, string key)
    {
        if (actionLabel != null)
        {
            actionLabel.text = string.IsNullOrWhiteSpace(action)
                ? "ВЗАИМОДЕЙСТВОВАТЬ"
                : action.ToUpperInvariant();
        }

        if (keyLabel != null)
        {
            keyLabel.text = string.IsNullOrWhiteSpace(key) ? "E" : key;
        }
    }

    public void SetVisible(bool visible)
    {
        if (visualRoot == null || canvasGroup == null || canvasRect == null)
        {
            return;
        }

        if (visible && !enabled)
        {
            enabled = true;
        }

        isVisible = visible;

        if (visible && !visualRoot.activeSelf)
        {
            canvasGroup.alpha = 0f;
            SetWorldScale(worldScale * 0.92f);
            visualRoot.SetActive(true);
        }
    }

    private void Update()
    {
        if (canvasGroup == null || canvasRect == null || visualRoot == null)
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
        float currentScale = canvasRect.lossyScale.x / worldScale;
        float nextScale = Mathf.Lerp(
            currentScale,
            targetScale,
            Time.deltaTime * 10f
        );

        SetWorldScale(worldScale * nextScale);

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

    private void SetWorldScale(float scale)
    {
        if (canvasRect == null)
        {
            return;
        }

        Transform parent = canvasRect.parent;
        if (parent == null)
        {
            canvasRect.localScale = Vector3.one * scale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        canvasRect.localScale = new Vector3(
            scale / SafeScale(parentScale.x),
            scale / SafeScale(parentScale.y),
            scale / SafeScale(parentScale.z)
        );
    }

    private static float SafeScale(float value)
    {
        return Mathf.Abs(value) < 0.0001f ? 1f : value;
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
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.0001f)
        {
            visualRoot.transform.rotation = Quaternion.LookRotation(
                -directionToCamera,
                Vector3.up
            );
        }
    }
}
