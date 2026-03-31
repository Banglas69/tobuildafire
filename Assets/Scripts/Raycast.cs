using UnityEngine;
using UnityEngine.InputSystem;

public class PickupRaycaster : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask pickupLayerMask = ~0;

    [Header("Input")]
    [SerializeField] private InputActionReference pickupClick;

    [Header("Stick Progression")]
    [SerializeField] private SticksToCampfireCutscene stickManager;

    [Header("Pickup Audio")]
    [Tooltip("AudioSource used to play pickup sounds (recommended: on the player/camera).")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Played when ANY PickupItem is collected (optional).")]
    [SerializeField] private AudioClip genericPickupClip;

    [Tooltip("Played when a StickCollectible is collected (optional). If assigned, overrides generic for sticks).")]
    [SerializeField] private AudioClip stickPickupClip;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private PickupItem currentItem;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (pickupClick != null) pickupClick.action.Enable();
    }

    private void OnDisable()
    {
        if (pickupClick != null) pickupClick.action.Disable();

        if (currentItem != null)
            currentItem.SetHighlight(false);

        currentItem = null;
    }

    private void Update()
    {
        UpdateLookTarget();

        if (currentItem == null) return;
        if (pickupClick == null) return;
        if (!pickupClick.action.WasPressedThisFrame()) return;

        bool isStick = false;

        // If this pickup is a stick, register it BEFORE destroying the object
        var stick = currentItem.GetComponentInParent<StickCollectible>();
        if (stick != null && !stick.IsCollected)
        {
            isStick = true;
            stick.MarkCollected();

            if (stickManager != null)
                stickManager.RegisterStickPickup();
        }

        // Play pickup SFX (one-shot)
        PlayPickupSfx(isStick);

        Debug.Log("Picked up: " + currentItem.name);

        // Destroy pickup object
        currentItem.Pickup();
        currentItem = null;
    }

    private void PlayPickupSfx(bool isStick)
    {
        if (sfxSource == null) return;

        AudioClip clip = null;

        if (isStick && stickPickupClip != null) clip = stickPickupClip;
        else if (genericPickupClip != null) clip = genericPickupClip;

        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void UpdateLookTarget()
    {
        PickupItem hitPickup = null;

        if (playerCamera != null &&
            Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                            out RaycastHit hit, pickupRange, pickupLayerMask, QueryTriggerInteraction.Ignore))
        {
            hitPickup = hit.collider.GetComponentInParent<PickupItem>();
        }

        if (hitPickup != currentItem)
        {
            if (currentItem != null) currentItem.SetHighlight(false);
            currentItem = hitPickup;
            if (currentItem != null) currentItem.SetHighlight(true);
        }
    }
}
