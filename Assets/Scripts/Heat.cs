using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Heat : MonoBehaviour
{
    [Header("Heat Settings")]
    [Tooltip("Maximum heat level")]
    public float maxHeat = 100f;

    [Tooltip("Current heat level")]
    public float currentHeat = 100f;

    [Tooltip("Rate at which heat decreases per second when away from warmth")]
    public float heatDecreaseRate = 5f;

    [Header("Warmth Detection")]
    [Tooltip("Tag used for warmth sources (e.g., 'Fire', 'WarmthSource')")]
    public string warmthSourceTag = "WarmthSource";

    [Tooltip("Distance within which player is considered near a warmth source")]
    public float warmthDetectionRadius = 5f;

    [Header("Movement Detection")]
    [Tooltip("Reference to the player's FirstPersonController")]
    public FirstPersonController playerController;

    [Header("Frost Effect (Camera)")]
    [Tooltip("Drag the FrostEffect component from your camera here.")]
    public FrostMaterialDriver frostEffect;

    [Tooltip("Heat value where frost starts appearing (>= this = no frost).")]
    public float frostStartHeat = 60f;

    [Tooltip("How quickly the frost visual catches up (bigger = snappier).")]
    public float frostLerpSpeed = 5f;

    [Header("Freeze Death UI")]
    [Tooltip("Full-screen white image CanvasGroup. Alpha should start at 0.")]
    [SerializeField] private CanvasGroup whiteFadeGroup;

    [Tooltip("CanvasGroup for the 'You have frozen to death' text. Alpha should start at 0.")]
    [SerializeField] private CanvasGroup frozenTextGroup;

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float whiteFadeDuration = 1.2f;
    [SerializeField] private float textFadeDuration = 0.4f;
    [SerializeField] private float holdBeforeReturnToMenu = 2.2f;

    [Header("Disable On Freeze")]
    [Tooltip("Movement / look / interaction scripts to disable when death starts.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnFreeze;

    // Internal variables
    private bool hasPlayerMoved = false;
    private bool isNearWarmthSource = false;
    private Vector3 lastPosition;
    private float lastHeatLogTime = 0f;
    private float heatLogInterval = 1f;

    private bool freezeSequenceStarted = false;
    private bool frozenLogged = false;

    void Start()
    {
        Debug.Log("Heat system initialized!");

        currentHeat = maxHeat;
        lastPosition = transform.position;
        Debug.Log($"Starting position: {lastPosition}");

        if (playerController == null)
        {
            playerController = GetComponent<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogWarning("FirstPersonController not found. Please assign it manually in the Inspector.");
            }
            else
            {
                Debug.Log("FirstPersonController found and assigned.");
            }
        }

        if (frostEffect == null)
        {
            frostEffect = FindFirstObjectByType<FrostMaterialDriver>();
            if (frostEffect == null)
                Debug.LogWarning("FrostEffect not found. Assign it from the camera in the Inspector.");
        }

        if (frostEffect != null)
            frostEffect.FrostAmount = 0f;

        SetCanvasGroupInstant(whiteFadeGroup, 0f);
        SetCanvasGroupInstant(frozenTextGroup, 0f);
    }

    void Update()
    {
        if (freezeSequenceStarted)
            return;

        if (!hasPlayerMoved)
        {
            CheckIfPlayerMoved();

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"Waiting for player to move. Current pos: {transform.position}, Start pos: {lastPosition}, Distance: {Vector3.Distance(transform.position, lastPosition):F4}");
            }
        }

        if (hasPlayerMoved)
        {
            CheckForWarmthSources();

            if (!isNearWarmthSource)
                DecreaseHeat();
            else
                IncreaseHeatOverTime();
        }

        UpdateFrost();

        if (!freezeSequenceStarted && frostEffect != null && frostEffect.FrostAmount >= 0.999f)
        {
            StartCoroutine(FreezeDeathSequence());
        }
    }

    private void CheckIfPlayerMoved()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        if (distance > 0.001f)
        {
            hasPlayerMoved = true;
            Debug.Log($"<color=green>Player has moved! Heat system activated. Distance moved: {distance:F4}</color>");
        }
    }

    private void CheckForWarmthSources()
    {
        GameObject[] warmthSources = GameObject.FindGameObjectsWithTag(warmthSourceTag);

        if (warmthSources.Length == 0)
        {
            Debug.LogWarning($"No objects found with tag '{warmthSourceTag}'. Make sure to tag your warmth sources!");
        }

        isNearWarmthSource = false;

        foreach (GameObject source in warmthSources)
        {
            float distance = Vector3.Distance(transform.position, source.transform.position);

            if (distance <= warmthDetectionRadius)
            {
                isNearWarmthSource = true;
                Debug.Log($"Near warmth source: {source.name} (Distance: {distance:F2})");
                break;
            }
        }

        if (!isNearWarmthSource && warmthSources.Length > 0)
        {
            Debug.Log("Not near any warmth sources. Heat will decrease.");
        }
    }

    private void DecreaseHeat()
    {
        float heatLost = heatDecreaseRate * Time.deltaTime;
        currentHeat -= heatLost;
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        if (Time.time - lastHeatLogTime >= heatLogInterval)
        {
            Debug.Log($"Heat decreasing: Current Heat: {currentHeat:F2}/{maxHeat} ({GetHeatPercentage():F1}%)");
            lastHeatLogTime = Time.time;
        }

        if (currentHeat <= 0f)
        {
            OnPlayerFrozen();
        }
    }

    private void IncreaseHeatOverTime()
    {
        float heatGained = heatDecreaseRate * Time.deltaTime;
        currentHeat += heatGained;
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        if (Time.time - lastHeatLogTime >= heatLogInterval)
        {
            Debug.Log($"Heat increasing: Current Heat: {currentHeat:F2}/{maxHeat} ({GetHeatPercentage():F1}%)");
            lastHeatLogTime = Time.time;
        }
    }

    private void UpdateFrost()
    {
        if (frostEffect == null) return;

        float targetFrost = 0f;

        if (currentHeat < frostStartHeat)
        {
            targetFrost = Mathf.InverseLerp(frostStartHeat, 0f, currentHeat);
        }

        frostEffect.FrostAmount = Mathf.Lerp(
            frostEffect.FrostAmount,
            targetFrost,
            Time.deltaTime * frostLerpSpeed
        );
    }

    private void OnPlayerFrozen()
    {
        if (frozenLogged) return;
        frozenLogged = true;

        Debug.Log("Player heat reached zero. Waiting for frost visual to hit 1.00 before death sequence.");
        
        if (frostEffect == null && !freezeSequenceStarted)
        {
            StartCoroutine(FreezeDeathSequence());
        }
    }

    private IEnumerator FreezeDeathSequence()
    {
        if (freezeSequenceStarted)
            yield break;

        freezeSequenceStarted = true;
        SetFreezeControlsEnabled(false);

        yield return FadeCanvasGroup(whiteFadeGroup, 1f, whiteFadeDuration);
        yield return FadeCanvasGroup(frozenTextGroup, 1f, textFadeDuration);

        if (holdBeforeReturnToMenu > 0f)
            yield return new WaitForSeconds(holdBeforeReturnToMenu);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetFreezeControlsEnabled(bool enabled)
    {
        if (playerController != null)
            playerController.enabled = enabled;

        if (scriptsToDisableOnFreeze != null)
        {
            foreach (var script in scriptsToDisableOnFreeze)
            {
                if (script != null)
                    script.enabled = enabled;
            }
        }
    }

    private void SetCanvasGroupInstant(CanvasGroup group, float alpha)
    {
        if (group == null) return;

        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = alpha > 0.001f;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        if (group == null) yield break;

        float start = group.alpha;
        group.interactable = false;
        group.blocksRaycasts = true;

        if (duration <= 0.0001f)
        {
            group.alpha = targetAlpha;
            group.blocksRaycasts = targetAlpha > 0.001f;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }

        group.alpha = targetAlpha;
        group.blocksRaycasts = targetAlpha > 0.001f;
    }

    public void IncreaseHeat(float amount)
    {
        currentHeat += amount;
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        Debug.Log($"Heat increased: +{amount:F2} | Current Heat: {currentHeat:F2}/{maxHeat}");
    }

    public float GetHeatPercentage()
    {
        return (currentHeat / maxHeat) * 100f;
    }

    public bool IsNearWarmth()
    {
        return isNearWarmthSource;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, warmthDetectionRadius);
    }
}