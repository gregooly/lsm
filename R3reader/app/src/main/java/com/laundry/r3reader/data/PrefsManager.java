package com.laundry.r3reader.data;

import android.content.Context;
import android.content.SharedPreferences;

import androidx.annotation.NonNull;

public final class PrefsManager {

    private static final String PREFS = "r3reader_prefs";
    private static final String KEY_DARK = "dark_mode";
    private static final String KEY_SOUND = "sound";
    private static final String KEY_VIBRATION = "vibration";
    private static final String KEY_LOGGED_IN = "logged_in";
    private static final String KEY_READER_CONNECTED = "reader_connected";
    private static final String KEY_DEVICE_ID = "device_id";
    private static final String KEY_TOKEN = "auth_token";
    private static final String KEY_CUSTOMER_ID = "customer_id";
    private static final String KEY_READER_ID = "reader_id";
    private static final String KEY_HANDHELD_ID = "handheld_id";
    private static final String KEY_ACTIVE_READER_WAY_ID = "active_reader_way_id";
    private static final String KEY_ACTIVE_WORKFLOW = "active_workflow";

    private PrefsManager() {
    }

    private static SharedPreferences prefs(Context context) {
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    public static boolean isDarkMode(Context context) {
        return prefs(context).getBoolean(KEY_DARK, false);
    }

    public static void setDarkMode(Context context, boolean enabled) {
        prefs(context).edit().putBoolean(KEY_DARK, enabled).apply();
    }

    public static boolean isSoundEnabled(Context context) {
        return prefs(context).getBoolean(KEY_SOUND, true);
    }

    public static void setSoundEnabled(Context context, boolean enabled) {
        prefs(context).edit().putBoolean(KEY_SOUND, enabled).apply();
    }

    public static boolean isVibrationEnabled(Context context) {
        return prefs(context).getBoolean(KEY_VIBRATION, true);
    }

    public static void setVibrationEnabled(Context context, boolean enabled) {
        prefs(context).edit().putBoolean(KEY_VIBRATION, enabled).apply();
    }

    public static boolean isLoggedIn(Context context) {
        return prefs(context).getBoolean(KEY_LOGGED_IN, false)
                && !getToken(context).isEmpty();
    }

    public static void setLoggedIn(Context context, boolean loggedIn) {
        prefs(context).edit().putBoolean(KEY_LOGGED_IN, loggedIn).apply();
    }

    /** Login: token + customerId + tablet deviceId; readerId comes from connection-status. */
    public static void saveAuthAfterLogin(@NonNull Context context, @NonNull String token,
                                          int customerId, @NonNull String deviceId) {
        prefs(context).edit()
                .putString(KEY_TOKEN, token)
                .putInt(KEY_CUSTOMER_ID, customerId)
                .putInt(KEY_READER_ID, 0)
                .putString(KEY_DEVICE_ID, deviceId)
                .remove(KEY_HANDHELD_ID)
                .putBoolean(KEY_LOGGED_IN, true)
                .putBoolean(KEY_READER_CONNECTED, false)
                .apply();
        BootstrapStore.clear();
    }

    public static void clearAuth(@NonNull Context context) {
        prefs(context).edit()
                .remove(KEY_TOKEN)
                .remove(KEY_CUSTOMER_ID)
                .remove(KEY_READER_ID)
                .remove(KEY_HANDHELD_ID)
                .putBoolean(KEY_LOGGED_IN, false)
                .putBoolean(KEY_READER_CONNECTED, false)
                .apply();
        BootstrapStore.clear();
    }

    public static void clearReaderConnection(@NonNull Context context) {
        prefs(context).edit()
                .putInt(KEY_READER_ID, 0)
                .remove(KEY_HANDHELD_ID)
                .putBoolean(KEY_READER_CONNECTED, false)
                .putInt(KEY_ACTIVE_READER_WAY_ID, 0)
                .apply();
        BootstrapStore.clear();
    }

    @NonNull
    public static String getToken(@NonNull Context context) {
        String token = prefs(context).getString(KEY_TOKEN, "");
        return token != null ? token : "";
    }

    public static int getCustomerId(@NonNull Context context) {
        return prefs(context).getInt(KEY_CUSTOMER_ID, 0);
    }

    public static int getReaderId(@NonNull Context context) {
        return prefs(context).getInt(KEY_READER_ID, 0);
    }

    public static void setReaderId(@NonNull Context context, int readerId) {
        prefs(context).edit().putInt(KEY_READER_ID, readerId).apply();
    }

    @NonNull
    public static String getHandheldId(@NonNull Context context) {
        String id = prefs(context).getString(KEY_HANDHELD_ID, "");
        return id != null ? id : "";
    }

    public static void setHandheldId(@NonNull Context context, @NonNull String handheldId) {
        prefs(context).edit().putString(KEY_HANDHELD_ID, handheldId).apply();
    }

    public static boolean isReaderConnected(Context context) {
        return prefs(context).getBoolean(KEY_READER_CONNECTED, false);
    }

    public static void setReaderConnected(Context context, boolean connected) {
        prefs(context).edit().putBoolean(KEY_READER_CONNECTED, connected).apply();
    }

    public static String getDeviceId(Context context) {
        return prefs(context).getString(KEY_DEVICE_ID, "");
    }

    public static void setDeviceId(Context context, String deviceId) {
        prefs(context).edit().putString(KEY_DEVICE_ID, deviceId != null ? deviceId : "").apply();
    }

    public static void setActiveReaderWayId(@NonNull Context context, int readerWayId) {
        prefs(context).edit().putInt(KEY_ACTIVE_READER_WAY_ID, readerWayId).apply();
    }

    public static int getActiveReaderWayId(@NonNull Context context) {
        return prefs(context).getInt(KEY_ACTIVE_READER_WAY_ID, 0);
    }

    /** Legacy index for fallback UI when bootstrap has no ways. */
    public static void setActiveWorkflow(Context context, WorkflowType workflow) {
        prefs(context).edit()
                .putInt(KEY_ACTIVE_WORKFLOW, workflow != null ? workflow.index : WorkflowType.SORTING.index)
                .apply();
    }

    @NonNull
    public static WorkflowType getActiveWorkflow(Context context) {
        return WorkflowType.fromIndex(prefs(context).getInt(KEY_ACTIVE_WORKFLOW, WorkflowType.SORTING.index));
    }
}
