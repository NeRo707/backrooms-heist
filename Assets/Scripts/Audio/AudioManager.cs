using System;
using UnityEngine;

public enum VHSSound
{
  ZoomIn,
  ZoomOut
}

[Serializable]
public struct SoundMapping
{
  [SerializeField] private VHSSound soundType;
  public VHSSound SoundType { get => soundType; set => soundType = value; }
  [SerializeField] private AudioClip clip;
  public AudioClip Clip { get => clip; set => clip = value; }
}

public class AudioManager : MonoBehaviour
{
  public static AudioManager Instance { get; private set; }

  [Header("Audio Settings")]
  [SerializeField] private SoundMapping[] sounds;

  private AudioSource audioSource;

  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }

    audioSource = gameObject.AddComponent<AudioSource>();
  }

  // Play a sound continuously on loop
  public void PlayLoopingSound(VHSSound type)
  {
    foreach (SoundMapping mapping in sounds)
    {
      if (mapping.SoundType == type && mapping.Clip != null)
      {
        // If it's already playing this exact clip, don't restart it
        if (audioSource.clip == mapping.Clip && audioSource.isPlaying) return;

        audioSource.clip = mapping.Clip;
        audioSource.loop = true;
        audioSource.Play();
        return;
      }
    }
  }

  // Stop the looping sound immediately
  public void StopSound()
  {
    if (audioSource.isPlaying)
    {
      audioSource.Stop();
      audioSource.loop = false;
      audioSource.clip = null;
    }
  }

  // Keep your one-shot method just in case you use it elsewhere
  public void PlayOneShot(VHSSound type)
  {
    foreach (SoundMapping mapping in sounds)
    {
      if (mapping.SoundType == type && mapping.Clip != null)
      {
        audioSource.PlayOneShot(mapping.Clip);
        return;
      }
    }
  }
}
