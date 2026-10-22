using System.Collections;
using UnityEngine;

public class MainMenuCharacterAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation States")]
    [SerializeField] private string walkRightState = "WalkRight";
    [SerializeField] private string lookForwardState = "WalkForward";
    [SerializeField] private string lookLeftState = "WalkLeft";

    [Header("Timing")]
    [SerializeField] private float minWalkDuration = 5f;
    [SerializeField] private float maxWalkDuration = 10f;
    [SerializeField] private float lookDuration = 1f;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.2f;

    public bool IsWalking { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        StartCoroutine(PlayLoop());
    }

    private IEnumerator PlayLoop()
    {
        while (true)
        {
            // Идёт вправо
            IsWalking = true;
            PlayState(walkRightState);
            yield return new WaitForSeconds(Random.Range(minWalkDuration, maxWalkDuration));

            // Смотрит вперёд
            IsWalking = false;
            PlayState(lookForwardState);
            yield return new WaitForSeconds(lookDuration);

            // Один раз смотрит влево
            PlayState(lookLeftState);
            yield return new WaitForSeconds(lookDuration);

            // Сразу снова идёт вправо
        }
    }

    private void PlayState(string stateName)
    {
        animator.CrossFadeInFixedTime(stateName, transitionDuration);
    }
}