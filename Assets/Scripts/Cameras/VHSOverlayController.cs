using UnityEngine;
using TMPro;
using System;

public class VHSOverlayController : MonoBehaviour
{
    [Header("UI Component References")]
    [SerializeField] private TextMeshProUGUI timerText;    // Top right: 00:00:05
    [SerializeField] private TextMeshProUGUI dateText;     // Bottom right: AM 00:00 \n MAY 5 1970

    [Header("Date Settings")]
    [SerializeField] private bool useSystemTime = false;
    [SerializeField] private int startYear = 1970;
    [SerializeField] private int startMonth = 5;
    [SerializeField] private int startDay = 5;

    private float elapsedTime = 0f;
    private DateTime simulatedTime;

    private void Start()
    {
        // Initialize simulated starting date & time
        simulatedTime = new DateTime(startYear, startMonth, startDay, 0, 0, 0);
    }

    private void Update()
    {
        // 1. Update Top-Right Stopwatch Timer (MM:SS:MS)
        elapsedTime += Time.deltaTime;
        TimeSpan span = TimeSpan.FromSeconds(elapsedTime);
        if (timerText)
        {
            timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", span.Minutes, span.Seconds, (int)(span.Milliseconds / 10));
        }

        // 2. Update Bottom-Right Clock & Date
        if (dateText)
        {
            if (useSystemTime)
            {
                dateText.text = DateTime.Now.ToString("tt hh:mm\nMMM d yyyy").ToUpper();
            }
            else
            {
                // Advance the simulated date/time dynamically as play time progresses
                simulatedTime = simulatedTime.AddSeconds(Time.deltaTime);
                dateText.text = simulatedTime.ToString("tt hh:mm\nMMM d yyyy").ToUpper();
            }
        }
    }
}
