package com.laundry.r3reader.data;

import android.content.Context;
import android.provider.Settings;

import androidx.annotation.NonNull;

/**
 * Resolves a stable device identifier from {@link Settings.Secure#ANDROID_ID}
 * and formats it for display (hyphen every 4 chars when length &gt;= 16).
 */
public final class DeviceIdUtil {

    private static final String FALLBACK = "unknown_device";

    private DeviceIdUtil() {
    }

    @NonNull
    public static String getRawAndroidId(@NonNull Context context) {
        String id = Settings.Secure.getString(
                context.getContentResolver(),
                Settings.Secure.ANDROID_ID);
        if (id == null || id.isEmpty()) {
            return FALLBACK;
        }
        return id;
    }

    @NonNull
    public static String getFormattedDeviceId(@NonNull Context context) {
        return formatDeviceId(getRawAndroidId(context));
    }

    @NonNull
    public static String formatDeviceId(String rawDeviceId) {
        if (rawDeviceId == null || rawDeviceId.isEmpty()) {
            return FALLBACK;
        }
        String raw = stripSeparators(rawDeviceId);
        if (raw.length() < 16) {
            return rawDeviceId;
        }

        StringBuilder formatted = new StringBuilder();
        for (int i = 0; i < raw.length(); i++) {
            if (i > 0 && i % 4 == 0) {
                formatted.append('-');
            }
            formatted.append(raw.charAt(i));
        }
        return formatted.toString();
    }

    /** Device ID as stored in LaundryMS (hyphens every 4 chars). Use for all API requests. */
    @NonNull
    public static String toApiDeviceId(@NonNull String deviceId) {
        return formatDeviceId(deviceId);
    }

    @NonNull
    private static String stripSeparators(@NonNull String deviceId) {
        return deviceId.replace("-", "").replace(" ", "");
    }
}
