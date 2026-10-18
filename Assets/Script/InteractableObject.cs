using UnityEngine;

public abstract class InteractableObject : MonoBehaviour
{
    [Header("Interaction Outline")]
    [SerializeField] private Behaviour quickOutline;

    protected virtual void Awake()
    {
        SetOutline(false);
    }

    public void SetOutline(bool visible)
    {
        if (quickOutline != null)
        {
            quickOutline.enabled = visible;
        }
    }

    public abstract bool CanInteract(PlayerInventory inventory);

    public abstract void Interact(PlayerInventory inventory);
}