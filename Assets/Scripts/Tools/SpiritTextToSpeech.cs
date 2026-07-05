using System.Runtime.InteropServices;
using UnityEngine;

namespace PhasmophobiAR.Tools
{
    /// <summary>Uses the mobile device's offline-capable native text-to-speech engine.</summary>
    public static class SpiritTextToSpeech
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void SpiritTtsSpeak(string text, float rate, float pitch);

        [DllImport("__Internal")]
        static extern void SpiritTtsStop();
#endif

        public static void Speak(string text, float rate, float pitch)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            using var bridge = new AndroidJavaClass("com.phasmophobiar.SpiritTtsBridge");
            bridge.CallStatic("speak", text, Mathf.Clamp(rate, 0.5f, 2f), Mathf.Clamp(pitch, 0.5f, 2f));
#elif UNITY_IOS && !UNITY_EDITOR
            SpiritTtsSpeak(text, Mathf.Clamp(rate, 0.5f, 2f), Mathf.Clamp(pitch, 0.5f, 2f));
#else
            Debug.Log($"Spirit voice: {text}");
#endif
        }

        public static void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var bridge = new AndroidJavaClass("com.phasmophobiar.SpiritTtsBridge");
            bridge.CallStatic("stop");
#elif UNITY_IOS && !UNITY_EDITOR
            SpiritTtsStop();
#endif
        }
    }
}
