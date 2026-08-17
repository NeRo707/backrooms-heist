using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Handles playing a full-screen jumpscare / biting video when caught by an entity.
/// Auto-creates its UI Canvas, RawImage, and VideoPlayer at runtime.
/// </summary>
public class JumpscareVideoManager : MonoBehaviour
{
  private static JumpscareVideoManager _instance;

  public static JumpscareVideoManager Instance
  {
    get
    {
      if (_instance == null)
      {
        _instance = FindFirstObjectByType<JumpscareVideoManager>();
        if (_instance == null)
        {
          var go = new GameObject("JumpscareVideoManager");
          _instance = go.AddComponent<JumpscareVideoManager>();
          DontDestroyOnLoad(go);
        }
      }
      return _instance;
    }
  }

  [Header("References")]
  [SerializeField] private Canvas jumpscareCanvas;
  [SerializeField] private RawImage videoRawImage;
  [SerializeField] private VideoPlayer videoPlayer;
  [SerializeField] private AudioSource videoAudioSource;

  private Action _onVideoFinished;
  private bool _isPlaying = false;

  private void Awake()
  {
    if (_instance != null && _instance != this)
    {
      Destroy(gameObject);
      return;
    }
    _instance = this;

    EnsureUIReferences();
  }

  /// <summary>
  /// Plays a jumpscare video, stops monster audio, and executes onComplete callback (respawn / load scene) when finished.
  /// </summary>
  public void PlayJumpscare(VideoClip clip, AudioSource monsterAudioSource, Action onComplete)
  {
    if (_isPlaying) return;
    _isPlaying = true;
    _onVideoFinished = onComplete;

    // 1. Stop monster audio
    if (monsterAudioSource != null)
    {
      monsterAudioSource.Stop();
    }

    EnsureUIReferences();

    // 2. Enable canvas
    if (jumpscareCanvas != null)
    {
      jumpscareCanvas.gameObject.SetActive(true);
    }

    // 3. Play video if assigned, or fallback to timed sequence if no video asset yet
    if (clip != null && videoPlayer != null)
    {
      videoPlayer.clip = clip;
      videoPlayer.isLooping = false;
      videoPlayer.playOnAwake = false;

      // Subscribe to video completion event
      videoPlayer.loopPointReached -= OnVideoEnd;
      videoPlayer.loopPointReached += OnVideoEnd;

      videoPlayer.Play();
    }
    else
    {
      Debug.LogWarning("[JumpscareVideoManager] No VideoClip assigned on entity! Playing fallback 2s sequence.");
      StartCoroutine(FallbackSequence());
    }
  }

  private void OnVideoEnd(VideoPlayer source)
  {
    if (source != null)
    {
      source.loopPointReached -= OnVideoEnd;
    }

    FinishJumpscare();
  }

  private IEnumerator FallbackSequence()
  {
    yield return new WaitForSeconds(2.0f);
    FinishJumpscare();
  }

  private void FinishJumpscare()
  {
    _isPlaying = false;

    if (jumpscareCanvas != null)
    {
      jumpscareCanvas.gameObject.SetActive(false);
    }

    Action callback = _onVideoFinished;
    _onVideoFinished = null;
    callback?.Invoke();
  }

  private void EnsureUIReferences()
  {
    if (jumpscareCanvas == null)
    {
      var canvasObj = new GameObject("Jumpscare_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      jumpscareCanvas = canvasObj.GetComponent<Canvas>();
      jumpscareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
      jumpscareCanvas.sortingOrder = 999; // Top sorting order

      var scaler = canvasObj.GetComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920, 1080);
    }

    if (videoRawImage == null)
    {
      var imageObj = new GameObject("VideoRawImage", typeof(RectTransform), typeof(RawImage));
      imageObj.transform.SetParent(jumpscareCanvas.transform, false);

      videoRawImage = imageObj.GetComponent<RawImage>();
      videoRawImage.color = Color.white;

      var rect = imageObj.GetComponent<RectTransform>();
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;
    }

    if (videoPlayer == null)
    {
      videoPlayer = jumpscareCanvas.gameObject.GetComponent<VideoPlayer>();
      if (videoPlayer == null)
      {
        videoPlayer = jumpscareCanvas.gameObject.AddComponent<VideoPlayer>();
      }

      videoPlayer.renderMode = VideoRenderMode.RenderTexture;
      videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
      videoPlayer.EnableAudioTrack(0, true);
      videoPlayer.SetDirectAudioMute(0, false);
      videoPlayer.SetDirectAudioVolume(0, 1.0f);

      // Link video output to RawImage texture
      RenderTexture rt = new RenderTexture(1920, 1080, 0);
      videoPlayer.targetTexture = rt;
      videoRawImage.texture = rt;
    }

    jumpscareCanvas.gameObject.SetActive(false);
  }
}
