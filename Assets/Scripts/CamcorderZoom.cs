using UnityEngine;
using Unity.Cinemachine;

public class CamcorderManualZoom : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineCamera vCam;

    [Header("Zoom Limits")]
    public float maxFOV = 80f;
    public float minFOV = 30f;
    public float zoomSpeed = 25f;

    private float currentFOV;
    private bool isZoomingIn = false;
    private bool isZoomingOut = false;

    void Start()
    {
        if (vCam == null)
            vCam = GetComponent<CinemachineCamera>();

        currentFOV = maxFOV;
        ApplyFOVToCamera();
    }

    void LateUpdate()
    {
        // --- 1. DETECT INPUT PRESS / RELEASE ---

        // Left Click Pressed
        if (Input.GetMouseButtonDown(0))
        {
            isZoomingIn = true;
            isZoomingOut = false;
            AudioManager.Instance.PlayLoopingSound(VHSSound.ZoomIn);
        }
        // Left Click Released
        else if (Input.GetMouseButtonUp(0))
        {
            isZoomingIn = false;
            AudioManager.Instance.StopSound();
        }

        // Right Click Pressed
        if (Input.GetMouseButtonDown(1))
        {
            isZoomingOut = true;
            isZoomingIn = false;
            AudioManager.Instance.PlayLoopingSound(VHSSound.ZoomOut);
        }
        // Right Click Released
        else if (Input.GetMouseButtonUp(1))
        {
            isZoomingOut = false;
            AudioManager.Instance.StopSound();
        }

        // --- 2. APPLY ZOOM LOGIC ---

        float previousFOV = currentFOV;

        if (isZoomingIn)
        {
            currentFOV -= zoomSpeed * Time.deltaTime;
        }
        else if (isZoomingOut)
        {
            currentFOV += zoomSpeed * Time.deltaTime;
        }

        // Clamp the FOV between limits
        currentFOV = Mathf.Clamp(currentFOV, minFOV, maxFOV);

        // --- 3. CUT AUDIO IF WE HIT A LIMIT WALL ---
        // If the player is holding the button down, but we reached the min/max limit, stop the motor sound
        if ((isZoomingIn && currentFOV <= minFOV) || (isZoomingOut && currentFOV >= maxFOV))
        {
            AudioManager.Instance.StopSound();
        }

        ApplyFOVToCamera();
    }

    private void ApplyFOVToCamera()
    {
        var lens = vCam.Lens;
        lens.FieldOfView = currentFOV;
        vCam.Lens = lens;
    }
}
