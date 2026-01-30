using UnityEngine;

/// <summary>
/// Manages all audio playback for the mushroom generator.
/// Handles sound effects for mushroom selection and UI interactions.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip mushroomClickSound;
    [SerializeField] private AudioClip uiClickSound;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;

    private static AudioManager instance;
    public static AudioManager Instance => instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Verify audio source
        if (sfxSource == null)
        {
            Debug.LogError("AudioManager: SFX Audio Source is missing!");
        }

        // Set initial volume
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    /// <summary>
    /// Play the mushroom click sound effect.
    /// Called when a mushroom is selected.
    /// </summary>
    public void PlayMushroomClick()
    {
        if (sfxSource != null && mushroomClickSound != null)
        {
            sfxSource.PlayOneShot(mushroomClickSound);
        }
    }

    /// <summary>
    /// Play a generic UI click sound effect.
    /// Called for UI button interactions.
    /// </summary>
    public void PlayUIClick()
    {
        if (sfxSource != null && uiClickSound != null)
        {
            sfxSource.PlayOneShot(uiClickSound);
        }
    }

    /// <summary>
    /// Set the volume for sound effects.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
}
