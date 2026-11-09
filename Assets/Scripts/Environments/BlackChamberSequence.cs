using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls Act 2 Black Chamber sequence:
/// 1. Player enters black room -> Entrance door (Door 6) slams shut and locks. Eerie music starts playing.
/// 2. As player approaches the red lamp, the music's pitch dynamically drops from 1.0 down to 0.5.
/// 3. When player gets close to the red lamp/door ->
///    - Eerie music cuts off to dead silence.
///    - Hallway scream echoes from far behind (Door 6).
///    - Fast running footsteps + walking on broken glass rush up to Door 6.
///    - Violent, loud knocking / pounding on the back door (Door 6) with procedural door shaking.
/// 4. Knocking stops -> Dead silence (player is looking back at Door 6).
/// 5. Red door in front starts acting up:
///    - Red floor lamp flickers ominously.
///    - Door unlatches / rattles.
///    - Red door slowly creaks open (~30 degrees).
///    - Monster peeks through the crack into the red light with uncanny neck twist and eerie audio.
/// 6. After staring at the monster, the 'payment sound' plays at 0.67 pitch (eerie build-up and bonk).
/// 7. On the bonk, lights cut out, and the player respawns back at the beginning (restarts current scene).
/// </summary>
public class BlackChamberSequence : MonoBehaviour {
  [Header("Chamber Entry (Trap)")]
  [Tooltip("Entrance door into the black room (Door 6) that will slam shut.")]
  [SerializeField] private DoorController entranceDoor;
  [Tooltip("Heavy door slam audio clip.")]
  [SerializeField] private AudioClip entranceSlamClip;
  [Tooltip("AudioSource for the eerie ambient music / drone.")]
  [SerializeField] private AudioSource eerieMusicSource;
  [Tooltip("Music fade in duration (seconds).")]
  [SerializeField] private float musicFadeInTime = 1.0f;

  [Header("Dynamic Music Pitch Modulation")]
  [Tooltip("Transform of the red lamp on the floor to measure distance to.")]
  [SerializeField] private Transform redLampTarget;
  [Tooltip("Distance at which music begins pitch-shifting down (e.g. 11m).")]
  [SerializeField] private float pitchStartDistance = 11.0f;
  [Tooltip("Distance at which music reaches lowest pitch (e.g. 2.5m).")]
  [SerializeField] private float pitchEndDistance = 2.5f;
  [Tooltip("Starting pitch at entrance (default 1.0).")]
  [Range(0.5f, 1.5f)]
  [SerializeField] private float startPitch = 1.0f;
  [Tooltip("Ending pitch near lamp (default 0.5).")]
  [Range(0.2f, 1.0f)]
  [SerializeField] private float endPitch = 0.5f;

  [Header("Back Door & Hallway Terror (Behind Player)")]
  [Tooltip("3D AudioSource on entrance door (Door 6) facing the hallway.")]
  [SerializeField] private AudioSource backDoorAudio;
  [Tooltip("Transform of entrance door (Door 6) to physically shake during knocking.")]
  [SerializeField] private Transform backDoorPivot;
  [Tooltip("Scream / roar heard echoing from down the hallway.")]
  [SerializeField] private AudioClip hallwayScreamClip;
  [Tooltip("Fast running footsteps approaching the back door.")]
  [SerializeField] private AudioClip[] footstepClips;
  [Tooltip("Walking/crunching on broken glass sound effect.")]
  [SerializeField] private AudioClip glassCrunchClip;
  [Tooltip("Violent loud knocking/pounding on the back door.")]
  [SerializeField] private AudioClip backKnockClip;
  [Tooltip("Duration of knocking on the back door in seconds.")]
  [SerializeField] private float backKnockDuration = 3.5f;
  [Tooltip("Shake intensity on the back door.")]
  [SerializeField] private float backDoorShakeAmount = 0.035f;

  [Header("Red Door Acting Up (In Front of Player)")]
  [Tooltip("The red door at the far end of the room (Door 7).")]
  [SerializeField] private DoorController redDoor;
  [Tooltip("Pivot of the red door.")]
  [SerializeField] private Transform redDoorPivot;
  [Tooltip("3D AudioSource attached to the red door.")]
  [SerializeField] private AudioSource redDoorAudio;
  [Tooltip("Pause after back door knocks stop before red door starts acting up.")]
  [SerializeField] private float delayBeforeRedDoorActs = 1.4f;
  [Tooltip("Point lights on the red floor lamp to flicker.")]
  [SerializeField] private Light[] redLampLights;
  [Tooltip("Door handle rattle / unlatch sound on red door.")]
  [SerializeField] private AudioClip redDoorHandleClip;
  [Tooltip("Door creak sound effect.")]
  [SerializeField] private AudioClip redDoorCreakClip;
  [Tooltip("Angle to crack open the red door (in degrees).")]
  [SerializeField] private float redDoorPeekAngle = 30f;
  [Tooltip("Speed to slowly creak open the red door.")]
  [SerializeField] private float redDoorCreakSpeed = 0.7f;

  [Header("The Peek Reveal")]
  [Tooltip("The entity or head that peeks through the open red door.")]
  [SerializeField] private GameObject peekEntity;
  [Tooltip("Eerie sound played when entity peeks in (whisper / low drone / sting).")]
  [SerializeField] private AudioClip peekEerieSound;
  [Tooltip("Optional Animator on the peek entity.")]
  [SerializeField] private Animator peekAnimator;
  [Tooltip("Optional animation trigger for the peek entity.")]
  [SerializeField] private string peekAnimTrigger = "Neck";

  [Header("Ending & Scene Restart")]
  [Tooltip("Payment sound clip played at the end ('build-up and bonk').")]
  [SerializeField] private AudioClip paymentSoundClip;
  [Tooltip("Pitch to play payment sound at (-0.67 plays in reverse, creating a rising swell ending with a bonk).")]
  [Range(-2.0f, 2.0f)]
  [SerializeField] private float paymentPitch = -0.67f;
  [Tooltip("Seconds to stare at the monster peek before the payment build-up starts.")]
  [SerializeField] private float peekStareDuration = 1.8f;
  [Tooltip("AudioSource to play the ending sound through (defaults to 2D ChamberAmbience).")]
  [SerializeField] private AudioSource endingAudioSource;
  [Tooltip("Cut lights to black right as the bonk lands.")]
  [SerializeField] private bool blackoutOnBonk = true;

  [Header("Player Reference (Optional)")]
  [SerializeField] private Transform playerTransform;

  // Runtime State
  private bool _chamberEntered = false;
  private bool _climaxTriggered = false;
  private Coroutine _climaxCoroutine;
  private float[] _originalLightIntensities;

  public bool ChamberEntered => _chamberEntered;
  public bool ClimaxTriggered => _climaxTriggered;

  private void Awake() {
    if (peekEntity != null) {
      peekEntity.SetActive(false);
    }

    if (redLampLights != null && redLampLights.Length > 0) {
      _originalLightIntensities = new float[redLampLights.Length];
      for (int i = 0; i < redLampLights.Length; i++) {
        if (redLampLights[i] != null) {
          _originalLightIntensities[i] = redLampLights[i].intensity;
        }
      }
    }
  }

  private void Update() {
    // Dynamic pitch modulation based on distance to the red lamp
    if (_chamberEntered && !_climaxTriggered && eerieMusicSource != null && eerieMusicSource.isPlaying) {
      ResolvePlayerTransform();

      if (playerTransform != null && redLampTarget != null) {
        float distance = Vector3.Distance(playerTransform.position, redLampTarget.position);
        float t = Mathf.InverseLerp(pitchStartDistance, pitchEndDistance, distance);
        eerieMusicSource.pitch = Mathf.Lerp(startPitch, endPitch, t);

        // Auto-trigger climax if player gets within minimum distance
        if (distance <= pitchEndDistance) {
          OnPlayerApproachRedDoor();
        }
      }
    }
  }

  private void ResolvePlayerTransform() {
    if (playerTransform != null) return;

    if (Camera.main != null) {
      playerTransform = Camera.main.transform;
      return;
    }

    GameObject p = GameObject.FindWithTag("Player");
    if (p != null) {
      playerTransform = p.transform;
    }
  }

  /// <summary>
  /// Called when the player steps into the black chamber through Door 6.
  /// </summary>
  public void OnPlayerEnterChamber() {
    if (_chamberEntered) return;
    _chamberEntered = true;

    // Slam entrance door shut and lock it
    if (entranceDoor != null) {
      entranceDoor.SlamShutAndLock(entranceSlamClip, 9f);
    }

    // Start eerie background music
    if (eerieMusicSource != null) {
      StartCoroutine(FadeInMusic(musicFadeInTime));
    }
  }

  /// <summary>
  /// Called when the player gets close to the red lamp and red door.
  /// </summary>
  public void OnPlayerApproachRedDoor() {
    if (_climaxTriggered || !_chamberEntered) return;
    _climaxTriggered = true;

    if (_climaxCoroutine != null) StopCoroutine(_climaxCoroutine);
    _climaxCoroutine = StartCoroutine(RunClimaxSequence());
  }

  private IEnumerator FadeInMusic(float duration) {
    if (eerieMusicSource == null) yield break;
    float targetVol = eerieMusicSource.volume > 0.05f ? eerieMusicSource.volume : 0.65f;
    eerieMusicSource.volume = 0f;
    eerieMusicSource.pitch = startPitch;
    eerieMusicSource.loop = true;
    eerieMusicSource.Play();

    float elapsed = 0f;
    while (elapsed < duration) {
      elapsed += Time.deltaTime;
      eerieMusicSource.volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
      yield return null;
    }
    eerieMusicSource.volume = targetVol;
  }

  private IEnumerator RunClimaxSequence() {
    // ── Step 1: Eerie music stops abruptly (dead silence) ───────────────────
    if (eerieMusicSource != null && eerieMusicSource.isPlaying) {
      eerieMusicSource.Stop();
    }

    // Suffocating breathless pause
    yield return new WaitForSeconds(0.35f);

    // ── Step 2: Scream from down the hallway (BEHIND player at Door 6) ──────
    if (backDoorAudio != null && hallwayScreamClip != null) {
      backDoorAudio.PlayOneShot(hallwayScreamClip, 0.95f);
    }

    yield return new WaitForSeconds(0.45f);

    // ── Step 3: Fast running footsteps + walking on broken glass ────────────
    if (backDoorAudio != null) {
      if (glassCrunchClip != null) {
        backDoorAudio.PlayOneShot(glassCrunchClip, 0.85f);
      }

      if (footstepClips != null && footstepClips.Length > 0) {
        float stepInterval = 0.08f;
        for (int i = 0; i < 8; i++) {
          AudioClip step = footstepClips[i % footstepClips.Length];
          if (step != null) backDoorAudio.PlayOneShot(step, 0.9f);
          yield return new WaitForSeconds(stepInterval);
        }
      }
    } else {
      yield return new WaitForSeconds(0.7f);
    }

    // ── Step 4: Violent loud knockings on the back door + door shake ────────
    if (backDoorAudio != null && backKnockClip != null) {
      backDoorAudio.PlayOneShot(backKnockClip, 1.0f);
    }

    // Physically shake Door 6 while being violently pounded on
    if (backDoorPivot != null) {
      Vector3 originalPos = backDoorPivot.localPosition;
      Quaternion originalRot = backDoorPivot.localRotation;
      float shakeTimer = 0f;

      while (shakeTimer < backKnockDuration) {
        shakeTimer += Time.deltaTime;

        float xOffset = Random.Range(-backDoorShakeAmount, backDoorShakeAmount);
        float zOffset = Random.Range(-backDoorShakeAmount, backDoorShakeAmount);
        backDoorPivot.localPosition = originalPos + new Vector3(xOffset, 0f, zOffset);

        float rotJitter = Random.Range(-2.0f, 2.0f);
        backDoorPivot.localRotation = originalRot * Quaternion.Euler(0f, rotJitter, 0f);

        yield return null;
      }

      backDoorPivot.localPosition = originalPos;
      backDoorPivot.localRotation = originalRot;
    } else {
      yield return new WaitForSeconds(backKnockDuration);
    }

    // ── Step 5: Knocking stops -> Dead silence ──────────────────────────────
    yield return new WaitForSeconds(delayBeforeRedDoorActs);

    // ── Step 6: The Red Room & Red Door Start Acting Up ─────────────────────
    // A) Red floor lamp flickers twice
    yield return StartCoroutine(FlickerRedLamp(2));

    // B) Handle rattle on the red door
    if (redDoorAudio != null && redDoorHandleClip != null) {
      redDoorAudio.PlayOneShot(redDoorHandleClip, 0.8f);
    }

    yield return new WaitForSeconds(0.4f);

    // C) Red door slowly creaks open
    if (redDoor != null) {
      redDoor.ForceOpen(redDoorPeekAngle, redDoorCreakSpeed, redDoorCreakClip);
    }

    yield return new WaitForSeconds(0.5f);

    // D) Monster peeks through crack with eerie sound and head tilt
    if (peekEntity != null) {
      peekEntity.SetActive(true);

      if (peekAnimator != null && !string.IsNullOrEmpty(peekAnimTrigger)) {
        peekAnimator.SetTrigger(peekAnimTrigger);
      }

      if (redDoorAudio != null && peekEerieSound != null) {
        redDoorAudio.PlayOneShot(peekEerieSound, 1.0f);
      }
    }

    // ── Step 7: Tense stare pause ───────────────────────────────────────────
    yield return new WaitForSeconds(peekStareDuration);

    // ── Step 8: Play Payment Sound in Reverse at -0.67 Pitch (Build-up and Bonk) ─
    if (paymentSoundClip != null) {
      AudioSource playSource = endingAudioSource != null ? endingAudioSource : redDoorAudio;
      if (playSource != null) {
        playSource.clip = paymentSoundClip;
        playSource.pitch = paymentPitch;
        playSource.loop = false;

        // When playing in reverse (negative pitch in Unity), playback must start from the end of the clip!
        if (paymentPitch < 0f) {
          playSource.timeSamples = paymentSoundClip.samples - 1;
        } else {
          playSource.time = 0f;
        }

        playSource.Play();

        float soundDuration = paymentSoundClip.length / Mathf.Max(0.05f, Mathf.Abs(paymentPitch));

        // When the bonk hits near the end of the reverse sound (the original sound attack), snap lights to black!
        if (blackoutOnBonk && redLampLights != null) {
          float preBonkWait = Mathf.Max(0.1f, soundDuration - 0.25f);
          yield return new WaitForSeconds(preBonkWait);

          for (int i = 0; i < redLampLights.Length; i++) {
            if (redLampLights[i] != null) redLampLights[i].enabled = false;
          }

          yield return new WaitForSeconds(soundDuration - preBonkWait);
        } else {
          yield return new WaitForSeconds(soundDuration);
        }
      } else {
        yield return new WaitForSeconds(3.0f);
      }
    } else {
      yield return new WaitForSeconds(2.0f);
    }

    // 0.4s pause in total blackness after the bonk
    yield return new WaitForSeconds(0.4f);

    // ── Step 9: Restart Current Scene (Respawn at beginning) ────────────────
    int activeIndex = SceneManager.GetActiveScene().buildIndex;
    if (activeIndex >= 0) {
      SceneManager.LoadScene(activeIndex);
    } else {
      string activeName = SceneManager.GetActiveScene().name;
      SceneManager.LoadScene(activeName);
    }
  }

  private IEnumerator FlickerRedLamp(int times) {
    if (redLampLights == null || redLampLights.Length == 0) yield break;

    for (int t = 0; t < times; t++) {
      for (int i = 0; i < redLampLights.Length; i++) {
        if (redLampLights[i] != null) redLampLights[i].intensity = _originalLightIntensities[i] * 0.1f;
      }
      yield return new WaitForSeconds(0.06f);

      for (int i = 0; i < redLampLights.Length; i++) {
        if (redLampLights[i] != null) redLampLights[i].intensity = _originalLightIntensities[i];
      }
      yield return new WaitForSeconds(0.1f);
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected() {
    if (entranceDoor != null) {
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(entranceDoor.transform.position, 1.2f);
    }
    if (redDoor != null) {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(redDoor.transform.position, 1.2f);
    }
    if (redLampTarget != null) {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(redLampTarget.position, pitchEndDistance);
      Gizmos.DrawWireSphere(redLampTarget.position, pitchStartDistance);
    }
    if (peekEntity != null) {
      Gizmos.color = Color.magenta;
      Gizmos.DrawWireCube(peekEntity.transform.position, new Vector3(0.5f, 1.8f, 0.5f));
    }
  }
#endif
}
