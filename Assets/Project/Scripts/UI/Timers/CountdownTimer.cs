using TMPro;
using UnityEngine;

namespace ModularMovement
{
    // Counts down to zero and shows the remaining minutes and seconds in a TMP label.
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Countdown Timer")]
    public sealed class CountdownTimer : MonoBehaviour
    {
        [SerializeField, Tooltip("Text that shows the countdown. If empty, uses TMP text on this object.")]
        private TMP_Text timerText;

        [SerializeField, Min(0f), Tooltip("Starting time in seconds. For example, 120 is two minutes. Zero keeps the timer stopped.")]
        private float durationSeconds = 300f;

        [SerializeField, Tooltip("Start counting when this object first loads. Otherwise, call ResumeTimer to begin.")]
        private bool startAutomatically = true;

        private float remainingSeconds;
        private bool isRunning;
        // Remember the last number shown to avoid rewriting the text every frame.
        private int lastDisplayedSeconds = -1;

        // Other scripts can read the countdown without changing it directly.
        public float RemainingSeconds => remainingSeconds;
        public bool IsRunning => isRunning;

        // Unity calls this when the component is added or reset in the Inspector.
        private void Reset()
        {
            timerText = GetComponent<TMP_Text>();
        }

        // Prepare the starting time once, while keeping any text assigned in the Inspector.
        private void Awake()
        {
            if (timerText == null)
                timerText = GetComponent<TMP_Text>();

            remainingSeconds = Mathf.Max(0f, durationSeconds);
            isRunning = startAutomatically && remainingSeconds > 0f;
            UpdateDisplay();
        }

        private void Update()
        {
            if (!isRunning)
                return;

            // Subtract the time since the last frame. The game's time scale also affects this timer.
            // Keep zero as the lowest value so the countdown never becomes negative.
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            if (remainingSeconds <= 0f)
                isRunning = false;

            UpdateDisplay();
        }

        // Keep the remaining time so it can be continued later.
        public void PauseTimer()
        {
            isRunning = false;
        }

        // A finished countdown stays stopped until it is restarted.
        public void ResumeTimer()
        {
            isRunning = remainingSeconds > 0f;
        }

        // Restore the configured duration and start again, even if automatic start is off.
        public void RestartTimer()
        {
            remainingSeconds = Mathf.Max(0f, durationSeconds);
            isRunning = remainingSeconds > 0f;
            lastDisplayedSeconds = -1;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (timerText == null)
                return;

            // Round up so the label reaches zero only when the countdown has finished.
            int totalSeconds = Mathf.CeilToInt(remainingSeconds);
            if (totalSeconds == lastDisplayedSeconds)
                return;

            lastDisplayedSeconds = totalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            // Minutes have no leading zero; seconds always use two digits, such as 2:05.
            timerText.text = $"{minutes}:{seconds:00}";
        }
    }
}
