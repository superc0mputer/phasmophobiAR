using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR_OSX
using System.Diagnostics;
#endif

namespace PhasmophobiAR.Tools
{
    /// <summary>Uses the mobile device's offline-capable native text-to-speech engine.</summary>
    public static class SpiritTextToSpeech
    {
#if UNITY_EDITOR_OSX
        static Process s_EditorSpeechProcess;
#endif

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
#elif UNITY_EDITOR_OSX
            StopEditorSpeech();
            var wordsPerMinute = Mathf.RoundToInt(Mathf.Lerp(105f, 220f, Mathf.InverseLerp(0.5f, 2f, rate)));
            var safeText = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
            s_EditorSpeechProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/say",
                Arguments = $"-r {wordsPerMinute} \"{safeText}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
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
#elif UNITY_EDITOR_OSX
            StopEditorSpeech();
#endif
        }

#if UNITY_EDITOR_OSX
        static void StopEditorSpeech()
        {
            if (s_EditorSpeechProcess == null) return;
            if (!s_EditorSpeechProcess.HasExited)
                s_EditorSpeechProcess.Kill();
            s_EditorSpeechProcess.Dispose();
            s_EditorSpeechProcess = null;
        }
#endif
    }
}
