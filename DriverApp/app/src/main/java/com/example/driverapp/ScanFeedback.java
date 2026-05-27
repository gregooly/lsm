package com.example.driverapp;

import android.content.Context;
import android.media.AudioManager;
import android.media.ToneGenerator;
import android.os.Build;
import android.os.VibrationEffect;
import android.os.Vibrator;

/**
 * Scan feedback similar in spirit to uhf-ble-demo {@code Utils.playSound}, gated by Settings prefs.
 */
public final class ScanFeedback {

    private ScanFeedback() {
    }

    public static void onUniqueTagScanned(Context context) {
        Context app = context.getApplicationContext();
        if (Global.isSoundEnabled(app)) {
            try {
                ToneGenerator tg = new ToneGenerator(AudioManager.STREAM_NOTIFICATION, 85);
                tg.startTone(ToneGenerator.TONE_PROP_BEEP, 100);
                tg.release();
            } catch (Throwable ignored) {
            }
        }
        if (Global.isVibrationEnabled(app)) {
            Vibrator vibrator = (Vibrator) app.getSystemService(Context.VIBRATOR_SERVICE);
            if (vibrator != null && vibrator.hasVibrator()) {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    vibrator.vibrate(VibrationEffect.createOneShot(35, VibrationEffect.DEFAULT_AMPLITUDE));
                } else {
                    vibrator.vibrate(35);
                }
            }
        }
    }
}
