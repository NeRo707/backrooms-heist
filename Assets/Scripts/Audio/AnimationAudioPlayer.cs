using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AnimationAudioPlayer : MonoBehaviour
{
  [Tooltip("The light switch click sound")]
  [SerializeField] private AudioClip clickSound;

  private AudioSource _audioSource;

  private void Awake()
  {
    _audioSource = GetComponent<AudioSource>();
  }

  // The Animation Event will trigger this exact method
  public void PlayClickSound()
  {
    if (clickSound != null && _audioSource != null)
    {
      _audioSource.PlayOneShot(clickSound);
    }
  }
}
