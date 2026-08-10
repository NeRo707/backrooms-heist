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
  public VHSSound soundType;
  public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
  public static AudioManager Instance { get; private set; }

  [Header("Audio Settings")]
  public SoundMapping[] sounds;

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
      if (mapping.soundType == type && mapping.clip != null)
      {
        // If it's already playing this exact clip, don't restart it
        if (audioSource.clip == mapping.clip && audioSource.isPlaying) return;

        audioSource.clip = mapping.clip;
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
      if (mapping.soundType == type && mapping.clip != null)
      {
        audioSource.PlayOneShot(mapping.clip);
        return;
      }
    }
  }
}
