package com.example.driverapp;

import org.json.JSONException;
import org.json.JSONObject;

public final class JsonUtils {

    private JsonUtils() {
    }

    /**
     * Puts JSON number when {@code raw} parses as long; otherwise puts string (or null if empty).
     */
    public static void putCoercedNumber(JSONObject obj, String key, String raw) throws JSONException {
        if (raw == null || raw.isEmpty()) {
            obj.put(key, JSONObject.NULL);
            return;
        }
        try {
            long v = Long.parseLong(raw.trim());
            obj.put(key, v);
        } catch (NumberFormatException e) {
            obj.put(key, raw);
        }
    }
}
