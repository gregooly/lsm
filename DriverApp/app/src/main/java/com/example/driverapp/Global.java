package com.example.driverapp;

import android.content.Context;
import android.content.SharedPreferences;

import com.rscja.deviceapi.RFIDWithUHFBLE;

/**
 * App-wide constants and small helpers. Preference keys align with uhf-ble-demo {@code SPUtils}
 * where applicable (same {@link #PREFS_CONFIG} name and {@link #KEY_AUTO_RECONNECT}).
 */
public final class Global {

    private Global() {
    }

    public static final String SERVER_URL = "http://172.20.1.28:5148";

    /** Matches uhf-ble-demo {@code SPUtils} shared-preferences file name. */
    public static final String PREFS_CONFIG = "config";

    /** Matches uhf-ble-demo {@code SPUtils.AUTO_RECONNECT}. */
    public static final String KEY_AUTO_RECONNECT = "autoReconnect";

    public static final String KEY_SOUND_ENABLED = "soundEnabled";
    public static final String KEY_VIBRATION_ENABLED = "vibrationEnabled";

    /** Last paired BLE address for auto-reconnect / cache clear (Driver extension). */
    public static final String KEY_LAST_BLE_MAC = "lastBleMac";

    public static SharedPreferences configPrefs(Context context) {
        return context.getApplicationContext().getSharedPreferences(PREFS_CONFIG, Context.MODE_PRIVATE);
    }

    /** Single RFID BLE handle (initialized in {@link DriverApplication}). */
    public static RFIDWithUHFBLE uhf() {
        return DriverApplication.getInstance().getUhf();
    }

    public static boolean isAutoReconnect(Context c) {
        return configPrefs(c).getBoolean(KEY_AUTO_RECONNECT, false);
    }

    public static void setAutoReconnect(Context c, boolean v) {
        configPrefs(c).edit().putBoolean(KEY_AUTO_RECONNECT, v).apply();
    }

    public static boolean isSoundEnabled(Context c) {
        return configPrefs(c).getBoolean(KEY_SOUND_ENABLED, true);
    }

    public static void setSoundEnabled(Context c, boolean v) {
        configPrefs(c).edit().putBoolean(KEY_SOUND_ENABLED, v).apply();
    }

    public static boolean isVibrationEnabled(Context c) {
        return configPrefs(c).getBoolean(KEY_VIBRATION_ENABLED, true);
    }

    public static void setVibrationEnabled(Context c, boolean v) {
        configPrefs(c).edit().putBoolean(KEY_VIBRATION_ENABLED, v).apply();
    }

    public static String getLastBleMac(Context c) {
        return configPrefs(c).getString(KEY_LAST_BLE_MAC, "");
    }

    public static void setLastBleMac(Context c, String mac) {
        configPrefs(c).edit().putString(KEY_LAST_BLE_MAC, mac != null ? mac.trim() : "").apply();
    }

    /** Clears saved BLE address used for auto-reconnect (similar to clearing scanner history). */
    public static void clearScannerLinkPrefs(Context c) {
        configPrefs(c).edit().remove(KEY_LAST_BLE_MAC).apply();
    }
}
