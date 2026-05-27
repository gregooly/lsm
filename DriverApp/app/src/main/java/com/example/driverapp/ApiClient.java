package com.example.driverapp;

import android.util.Log;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

public class ApiClient {

    private static final String TAG = "BackendApi";

    private static final String API_PREFIX = "/api";

    public static class ApiResult {
        public final boolean success;
        public final int code;
        public final JSONObject body;
        public final String error;

        public ApiResult(boolean success, int code, JSONObject body, String error) {
            this.success = success;
            this.code = code;
            this.body = body;
            this.error = error;
        }
    }

    public ApiResult loginWithDevice(String handheldDeviceId) {
        JSONObject payload = new JSONObject();
        try {
            payload.put("handheldDeviceId", handheldDeviceId);
        } catch (JSONException ignored) {
        }
        logEvent("LOGIN_DEVICE", API_PREFIX + "/auth/device-login", payload);
        return postJson(API_PREFIX + "/auth/device-login", payload, "");
    }

    public ApiResult getBootstrap(String sessionToken) {
        logEventEmptyPayload("BOOTSTRAP_GET", API_PREFIX + "/driver/bootstrap");
        return getJson(API_PREFIX + "/driver/bootstrap", sessionToken);
    }

    public ApiResult notifyReaderConnected(String handheldId, String driverId, String customerId, String sessionToken) {
        JSONObject payload = new JSONObject();
        try {
            payload.put("handheldId", handheldId);
            JsonUtils.putCoercedNumber(payload, "driverId", driverId);
            JsonUtils.putCoercedNumber(payload, "customerId", customerId);
            payload.put("connectedAt", TimeUtils.utcNowIso());
            payload.put("connectionType", "bluetooth");
        } catch (JSONException ignored) {
        }
        logEvent("READER_CONNECTED", API_PREFIX + "/readers/r2/connection-status", payload);
        return postJson(API_PREFIX + "/readers/r2/connection-status", payload, sessionToken);
    }

    public ApiResult uploadScannedResult(SessionStore sessionStore, JSONArray events) {
        JSONObject payload = buildBatchPayload(sessionStore, events);
        logEvent("UPLOAD_SCANNED_RESULT", API_PREFIX + "/linen-movement-events", payload);
        return postJson(API_PREFIX + "/linen-movement-events", payload, sessionStore.getAuthToken());
    }

    public ApiResult syncAll(SessionStore sessionStore, JSONArray events) {
        JSONObject payload = buildBatchPayload(sessionStore, events);
        logEvent("SYNC_ALL", API_PREFIX + "/linen-movement-events/sync", payload);
        return postJson(API_PREFIX + "/linen-movement-events/sync", payload, sessionStore.getAuthToken());
    }

    /**
     * HTTP transport succeeded (2xx). Movement endpoints may still return partial rejections in body.
     */
    public static boolean isHttpOk(ApiResult r) {
        return r != null && r.code >= 200 && r.code < 300;
    }

    private JSONObject buildBatchPayload(SessionStore sessionStore, JSONArray events) {
        JSONObject payload = new JSONObject();
        try {
            payload.put("handheldDeviceId", sessionStore.getHandheldDeviceIdForEnvelope());
            JsonUtils.putCoercedNumber(payload, "driverId", sessionStore.getDriverId());
            JsonUtils.putCoercedNumber(payload, "customerId", sessionStore.getCustomerId());
            JsonUtils.putCoercedNumber(payload, "jobId", sessionStore.getActiveJobId());
            JsonUtils.putCoercedNumber(payload, "readerId", sessionStore.getEffectiveReaderIdForApi());
            JsonUtils.putCoercedNumber(payload, "readerWayId", sessionStore.getActiveReaderWayId());
            payload.put("uploadedAt", TimeUtils.utcNowIso());
            payload.put("events", events);
        } catch (JSONException ignored) {
        }
        return payload;
    }

    private ApiResult getJson(String path, String token) {
        HttpURLConnection conn = null;
        String fullUrl = AppConfig.BASE_URL + path;
        try {
            URL url = new URL(fullUrl);
            conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("GET");
            conn.setConnectTimeout(15000);
            conn.setReadTimeout(15000);
            conn.setRequestProperty("Accept", "application/json");
            if (token != null && !token.isEmpty()) {
                conn.setRequestProperty("Authorization", "Bearer " + token);
            }

            int code = conn.getResponseCode();
            InputStream stream = code >= 200 && code < 300 ? conn.getInputStream() : conn.getErrorStream();
            String response = readStream(stream);
            JSONObject body = parseObject(response);
            boolean success = code >= 200 && code < 300;
            String error = success ? "" : response;
            Log.d(TAG, "RESPONSE <- GET " + fullUrl + " code=" + code + " body=" + response);
            return new ApiResult(success, code, body, error);
        } catch (Exception e) {
            Log.e(TAG, "REQUEST FAILED <- GET " + fullUrl + " error=" + e.getMessage(), e);
            return new ApiResult(false, -1, new JSONObject(), e.getMessage());
        } finally {
            if (conn != null) {
                conn.disconnect();
            }
        }
    }

    private ApiResult postJson(String path, JSONObject payload, String token) {
        HttpURLConnection conn = null;
        String fullUrl = AppConfig.BASE_URL + path;
        try {
            URL url = new URL(fullUrl);
            conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("POST");
            conn.setConnectTimeout(15000);
            conn.setReadTimeout(15000);
            conn.setDoOutput(true);
            conn.setRequestProperty("Content-Type", "application/json");
            conn.setRequestProperty("Accept", "application/json");
            if (token != null && !token.isEmpty()) {
                conn.setRequestProperty("Authorization", "Bearer " + token);
            }

            try (OutputStream os = conn.getOutputStream();
                 BufferedWriter writer = new BufferedWriter(
                         new OutputStreamWriter(os, StandardCharsets.UTF_8))) {
                writer.write(payload.toString());
                writer.flush();
            }

            int code = conn.getResponseCode();
            InputStream stream = code >= 200 && code < 300 ? conn.getInputStream() : conn.getErrorStream();
            String response = readStream(stream);
            JSONObject body = parseObject(response);
            boolean success = code >= 200 && code < 300;
            String error = success ? "" : response;
            Log.d(TAG, "RESPONSE <- " + fullUrl + " code=" + code + " body=" + response);
            return new ApiResult(success, code, body, error);
        } catch (Exception e) {
            Log.e(TAG, "REQUEST FAILED <- " + fullUrl + " error=" + e.getMessage(), e);
            return new ApiResult(false, -1, new JSONObject(), e.getMessage());
        } finally {
            if (conn != null) {
                conn.disconnect();
            }
        }
    }

    private void logEvent(String event, String path, JSONObject payload) {
        Log.d(TAG, "EVENT[" + event + "] -> " + AppConfig.BASE_URL + path + " payload=" + payload);
    }

    private void logEventEmptyPayload(String event, String path) {
        Log.d(TAG, "EVENT[" + event + "] -> " + AppConfig.BASE_URL + path);
    }

    private String readStream(InputStream stream) throws IOException {
        if (stream == null) {
            return "";
        }
        StringBuilder sb = new StringBuilder();
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(stream, StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                sb.append(line);
            }
        }
        return sb.toString();
    }

    private JSONObject parseObject(String raw) {
        if (raw == null || raw.isEmpty()) {
            return new JSONObject();
        }
        try {
            return new JSONObject(raw);
        } catch (JSONException e) {
            return new JSONObject();
        }
    }
}
