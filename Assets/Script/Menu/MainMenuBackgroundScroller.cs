
using UnityEngine;

public class MainMenuBackgroundScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MainMenuCharacterAnimation character;
    [SerializeField] private Transform backgroundA;
    [SerializeField] private Transform backgroundB;
    [SerializeField] private Camera targetCamera;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float scrollSpeed = 1f;

    private SpriteRenderer spriteA;
    private SpriteRenderer spriteB;
    private float backgroundWidth;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (backgroundA == null || backgroundB == null)
        {
            Debug.LogError(
                "BackgroundScroller: Assign both background objects.",
                this
            );
            enabled = false;
            return;
        }

        spriteA = backgroundA.GetComponent<SpriteRenderer>();
        spriteB = backgroundB.GetComponent<SpriteRenderer>();

        if (spriteA == null || spriteB == null)
        {
            Debug.LogError(
                "BackgroundScroller: Both objects need SpriteRenderer.",
                this
            );
            enabled = false;
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogError("BackgroundScroller: Camera not found.", this);
            enabled = false;
            return;
        }

        backgroundWidth = spriteA.bounds.size.x;

        if (backgroundWidth <= 0f)
        {
            Debug.LogError("BackgroundScroller: Invalid background width.", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (character == null || !character.IsWalking)
            return;

        float movement = scrollSpeed * Time.deltaTime;

        backgroundA.position += Vector3.right * movement;
        backgroundB.position += Vector3.right * movement;

        RepositionIfOutside(backgroundA, backgroundB);
        RepositionIfOutside(backgroundB, backgroundA);
    }

    private void RepositionIfOutside(Transform current, Transform other)
    {
        float cameraRight = targetCamera.ViewportToWorldPoint(
            new Vector3(
                1f,
                0.5f,
                Mathf.Abs(
                    current.position.z - targetCamera.transform.position.z
                )
            )
        ).x;

        float currentLeftEdge =
            current.position.x - backgroundWidth * 0.5f;

        if (currentLeftEdge > cameraRight)
        {
            current.position = new Vector3(
                other.position.x - backgroundWidth,
                current.position.y,
                current.position.z
            );
        }
    }
}
