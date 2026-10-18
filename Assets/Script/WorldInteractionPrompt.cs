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

    private Transform target;
    private Vector3 localAnchor;
    private Camera targetCamera;
    private bool isVisible;

    public bool Initialize(Transform followTarget)
    {
        target = followTarget;

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

        localAnchor = CalculateAnchor(target);
        UpdateWorldPosition();
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
}
