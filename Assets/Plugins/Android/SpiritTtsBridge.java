package com.phasmophobiar;

import android.speech.tts.TextToSpeech;
import com.unity3d.player.UnityPlayer;
import java.util.Locale;

public final class SpiritTtsBridge {
    private static TextToSpeech engine;
    private static boolean ready;
    private static String pendingText;
    private static float pendingRate;
    private static float pendingPitch;

    public static void speak(final String text, final float rate, final float pitch) {
        UnityPlayer.currentActivity.runOnUiThread(() -> {
            if (engine == null) {
                pendingText = text;
                pendingRate = rate;
                pendingPitch = pitch;
                engine = new TextToSpeech(UnityPlayer.currentActivity, status -> {
                    ready = status == TextToSpeech.SUCCESS;
                    if (!ready) return;
                    engine.setLanguage(Locale.US);
                    if (pendingText != null) {
                        speakNow(pendingText, pendingRate, pendingPitch);
                        pendingText = null;
                    }
                });
                return;
            }

            if (ready) speakNow(text, rate, pitch);
            else {
                pendingText = text;
                pendingRate = rate;
                pendingPitch = pitch;
            }
        });
    }

    private static void speakNow(String text, float rate, float pitch) {
        engine.setSpeechRate(rate);
        engine.setPitch(pitch);
        engine.speak(text, TextToSpeech.QUEUE_FLUSH, null, "spirit-response");
    }

    public static void stop() {
        UnityPlayer.currentActivity.runOnUiThread(() -> {
            pendingText = null;
            if (engine != null) engine.stop();
        });
    }
}
