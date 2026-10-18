using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BillboardAnimatorSwitcher : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;

    [Header("Direction Settings")]
    public int directions = 8;
    public float angleOffset = 0f;

    [Header("Rotation Settings")]
    public bool keepVertical = true;
    public float rotationSpeed = 0f;

    [Header("Animator")]
    public string directionParameter = "Direction";

    private Animator animator;
    private int lastDirection = -1;
    private int paramHash;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        animator = GetComponent<Animator>();
        paramHash = Animator.StringToHash(directionParameter);
    }

    private void Update()
    {
        if (targetCamera == null) return;

        Vector3 toCamera = targetCamera.transform.position - transform.position;

        if (keepVertical)
            toCamera.y = 0;

        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        RotateToCamera(toCamera);
        UpdateAnimation(toCamera);
    }

    private void RotateToCamera(Vector3 toCamera)
    {
        Transform parent = transform.parent;

        // Переводим направление в локальное пространство родителя
        Vector3 localDir = parent != null
            ? parent.InverseTransformDirection(toCamera)
            : toCamera;

        Quaternion targetRotation = Quaternion.LookRotation(localDir);

        if (rotationSpeed > 0)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.localRotation = targetRotation;
        }
    }

    private void UpdateAnimation(Vector3 toCamera)
    {
        Transform parent = transform.parent;

        // Направление "вперёд" относительно родителя
        Vector3 forward = parent != null ? parent.forward : Vector3.forward;

        if (keepVertical)
        {
            forward.y = 0;
            toCamera.y = 0;
        }

        float angle = Vector3.SignedAngle(forward, toCamera, Vector3.up);
        angle += angleOffset;

        if (angle < 0) angle += 360f;

        float sectorSize = 360f / directions;
        int directionIndex = Mathf.FloorToInt((angle + sectorSize * 0.5f) / sectorSize) % directions;

        if (directionIndex != lastDirection)
        {
            animator.SetInteger(paramHash, directionIndex);
            lastDirection = directionIndex;
        }
    }
}