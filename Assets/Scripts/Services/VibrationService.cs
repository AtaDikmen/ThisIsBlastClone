using UnityEngine;

namespace Services
{
    public class VibrationService : IVibrationService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static readonly AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        private static readonly AndroidJavaObject CurrentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        private static readonly AndroidJavaObject Vibrator = CurrentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#endif

        public void VibrateLight()  => TriggerVibration(20);
        public void VibrateMedium() => TriggerVibration(45);
        public void VibrateHeavy()  => TriggerVibration(90);

        private void TriggerVibration(long milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Vibrator != null && Vibrator.Call<bool>("hasVibrator"))
            {
                Vibrator.Call("vibrate", milliseconds);
            }
#endif
        }
    }
}
