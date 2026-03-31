using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private CanvasGroup blackFadeGroup;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    [Header("Campfire")]
    [Tooltip("Assign the TinyFlames transform here.")]
    [SerializeField] private Transform flamesTransform;
    [Tooltip("Assign the TinyFlames Particle System here.")]
    [SerializeField] private ParticleSystem flamesParticleSystem;

    [Header("Frost")]
    [SerializeField] private FrostMaterialDriver frostDriver;
    [SerializeField, Range(0f, 1f)] private float targetFrostAmount = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip extinguishClip;

    [Header("Intro Timing")]
    [SerializeField] private float titleFadeInDuration = 1.6f;
    [SerializeField] private float delayBeforeButtons = 0.35f;
    [SerializeField] private float buttonsFadeInDuration = 1f;

    [Header("Start Transition Timing")]
    [SerializeField] private float extinguishDuration = 0.35f;
    [SerializeField] private float textFadeOutDuration = 0.4f;
    [SerializeField] private float frostRaiseDuration = 0.8f;
    [SerializeField] private float blackFadeDuration = 1f;
    [SerializeField] private float loadSceneDelay = 0.05f;

    private bool isTransitioning;
    private Vector3 flamesStartScale;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartPressed);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitPressed);

        if (flamesTransform != null)
            flamesStartScale = flamesTransform.localScale;
    }

    private void Start()
    {
        PrepareInitialState();
        StartCoroutine(IntroSequence());
    }

    private void PrepareInitialState()
    {
        SetCanvasGroup(titleGroup, 0f, false);
        SetCanvasGroup(buttonsGroup, 0f, false);
        SetCanvasGroup(blackFadeGroup, 0f, false);

        if (frostDriver != null)
            frostDriver.FrostAmount = 0f;

        if (flamesTransform != null)
            flamesTransform.localScale = flamesStartScale;

        if (flamesParticleSystem != null && !flamesParticleSystem.isPlaying)
            flamesParticleSystem.Play(true);

        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private IEnumerator IntroSequence()
    {
        yield return FadeCanvasGroup(titleGroup, 0f, 1f, titleFadeInDuration, false);

        yield return new WaitForSeconds(delayBeforeButtons);

        yield return FadeCanvasGroup(buttonsGroup, 0f, 1f, buttonsFadeInDuration, true);
    }

    private void OnStartPressed()
    {
        if (isTransitioning) return;
        StartCoroutine(StartGameSequence());
    }

    private void OnExitPressed()
    {
        if (isTransitioning) return;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator StartGameSequence()
    {
        isTransitioning = true;
        SetButtonsInteractable(false);

        if (extinguishClip != null && sfxSource != null)
            sfxSource.PlayOneShot(extinguishClip);

        if (flamesParticleSystem != null)
            flamesParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        float elapsed = 0f;
        float totalDuration = Mathf.Max(extinguishDuration, textFadeOutDuration, frostRaiseDuration, blackFadeDuration);

        float titleStartAlpha = titleGroup != null ? titleGroup.alpha : 0f;
        float buttonsStartAlpha = buttonsGroup != null ? buttonsGroup.alpha : 0f;
        float blackStartAlpha = blackFadeGroup != null ? blackFadeGroup.alpha : 0f;
        float frostStart = frostDriver != null ? frostDriver.FrostAmount : 0f;
        Vector3 flamesInitialScale = flamesTransform != null ? flamesTransform.localScale : Vector3.zero;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            // Flames shrink quickly
            if (flamesTransform != null)
            {
                float t = Mathf.Clamp01(elapsed / extinguishDuration);
                flamesTransform.localScale = Vector3.Lerp(flamesInitialScale, Vector3.zero, t);
            }

            // Title + buttons fade out
            if (titleGroup != null)
            {
                float t = Mathf.Clamp01(elapsed / textFadeOutDuration);
                titleGroup.alpha = Mathf.Lerp(titleStartAlpha, 0f, t);
            }

            if (buttonsGroup != null)
            {
                float t = Mathf.Clamp01(elapsed / textFadeOutDuration);
                buttonsGroup.alpha = Mathf.Lerp(buttonsStartAlpha, 0f, t);
            }

            // Frost rises to 0.5
            if (frostDriver != null)
            {
                float t = Mathf.Clamp01(elapsed / frostRaiseDuration);
                frostDriver.FrostAmount = Mathf.Lerp(frostStart, targetFrostAmount, t);
            }

            // Black fade in
            if (blackFadeGroup != null)
            {
                float t = Mathf.Clamp01(elapsed / blackFadeDuration);
                blackFadeGroup.alpha = Mathf.Lerp(blackStartAlpha, 1f, t);
            }

            yield return null;
        }

        // Snap final state
        if (flamesTransform != null)
            flamesTransform.localScale = Vector3.zero;

        if (flamesParticleSystem != null)
            flamesParticleSystem.Clear(true);

        if (titleGroup != null)
            titleGroup.alpha = 0f;

        if (buttonsGroup != null)
            buttonsGroup.alpha = 0f;

        if (frostDriver != null)
            frostDriver.FrostAmount = targetFrostAmount;

        if (blackFadeGroup != null)
            blackFadeGroup.alpha = 1f;

        yield return new WaitForSeconds(loadSceneDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, bool enableInteractionAtEnd)
    {
        if (group == null) yield break;

        group.alpha = from;
        group.interactable = false;
        group.blocksRaycasts = false;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
        group.interactable = enableInteractionAtEnd;
        group.blocksRaycasts = enableInteractionAtEnd;
    }

    private void SetCanvasGroup(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    private void SetButtonsInteractable(bool value)
    {
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = value;
            buttonsGroup.blocksRaycasts = value;
        }

        if (startButton != null)
            startButton.interactable = value;

        if (exitButton != null)
            exitButton.interactable = value;
    }
}