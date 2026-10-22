
using UnityEngine;

public class MainMenuEnvironmentMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MainMenuCharacterAnimation character;

    [Header("Movement")]
    [SerializeField]
    private Vector3 movementDirection =
        new Vector3(-1f, 0f, 0f);

    [SerializeField] private float movementSpeed = 1f;

    private void Update()
    {
        if (character == null || !character.IsWalking)
            return;

        transform.Translate(
            movementDirection.normalized *
            movementSpeed *
            Time.deltaTime,
            Space.World
        );
    }
}
