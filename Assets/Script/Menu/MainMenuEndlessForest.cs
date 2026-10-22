
using UnityEngine;

public class MainMenuEndlessForest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MainMenuCharacterAnimation character;
    [SerializeField] private Camera targetCamera;

    [Header("Forest Segments")]
    [Tooltip("Группы деревьев и камней, расположенные слева направо")]
    [SerializeField] private Transform[] forestSegments;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 1f;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (character == null || targetCamera == null)
        {
            Debug.LogError(
                "MainMenuEndlessForest: Assign Character and Camera.",
                this
            );

            enabled = false;
            return;
        }

        if (forestSegments == null || forestSegments.Length < 2)
        {
            Debug.LogError(
                "MainMenuEndlessForest: Assign at least two forest segments.",
                this
            );

            enabled = false;
            return;
        }

        foreach (Transform segment in forestSegments)
        {
            if (segment == null ||
                segment.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogError(
                    "Every forest segment needs objects with Renderers.",
                    this
                );

                enabled = false;
                return;
            }
        }
    }

    private void Update()
    {
        if (!character.IsWalking)
            return;

        float movement = movementSpeed * Time.deltaTime;

        foreach (Transform segment in forestSegments)
        {
            segment.position += Vector3.left * movement;
        }

        RecycleSegments();
    }

    private void RecycleSegments()
    {
        foreach (Transform segment in forestSegments)
        {
            Bounds bounds = GetBounds(segment);

            float depth = Vector3.Dot(
                segment.position - targetCamera.transform.position,
                targetCamera.transform.forward
            );

            float cameraLeft = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0.5f, depth)
            ).x;

            if (bounds.max.x >= cameraLeft)
                continue;

            // Ищем самый правый край остальных групп.
            float rightmostEdge = float.NegativeInfinity;

            foreach (Transform other in forestSegments)
            {
                if (other == segment)
                    continue;

                Bounds otherBounds = GetBounds(other);
                rightmostEdge = Mathf.Max(
                    rightmostEdge,
                    otherBounds.max.x
                );
            }

            // Переносим группу за правый край леса.
            float offset = rightmostEdge - bounds.min.x;

            segment.position += Vector3.right * offset;
        }
    }

    private Bounds GetBounds(Transform segment)
    {
        Renderer[] renderers =
            segment.GetComponentsInChildren<Renderer>(true);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
