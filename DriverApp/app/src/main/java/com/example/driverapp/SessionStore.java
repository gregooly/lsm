package com.example.driverapp;

import android.content.Context;
import android.content.SharedPreferences;

public class SessionStore {

    private static final String PREFS = "driver_session";
    private static final String KEY_DEVICE_ID = "device_id";
    private static final String KEY_DRIVER_ID = "driver_id";
    private static final String KEY_DRIVER_NAME = "driver_name";
    private static final String KEY_CUSTOMER_ID = "customer_id";
    private static final String KEY_AUTH_TOKEN = "auth_token";
    private static final String KEY_READER_ID = "reader_id";
    private static final String KEY_LOCATIONS_JSON = "locations_json";
    private static final String KEY_BOOTSTRAP_JSON = "bootstrap_json";
    private static final String KEY_DRIVER_HANDHELD_DEVICE_ID = "driver_handheld_device_id";
    private static final String KEY_SCANNER_HANDHELD_LABEL = "scanner_handheld_label";
    private static final String KEY_ACTIVE_JOB_ID = "active_job_id";
    private static final String KEY_ACTIVE_READER_ID = "active_reader_id";
    private static final String KEY_ACTIVE_READER_WAY_ID = "active_reader_way_id";

    private final SharedPreferences prefs;

    public SessionStore(Context context) {
        prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    public void setDeviceId(String value) {
        prefs.edit().putString(KEY_DEVICE_ID, value).apply();
    }

    public String getDeviceId() {
        return prefs.getString(KEY_DEVICE_ID, "");
    }

    public void setDriverId(String value) {
        prefs.edit().putString(KEY_DRIVER_ID, value).apply();
    }

    public String getDriverId() {
        return prefs.getString(KEY_DRIVER_ID, "");
    }

    public void setDriverName(String value) {
        prefs.edit().putString(KEY_DRIVER_NAME, value).apply();
    }

    public String getDriverName() {
        return prefs.getString(KEY_DRIVER_NAME, "");
    }

    public void setCustomerId(String value) {
        prefs.edit().putString(KEY_CUSTOMER_ID, value).apply();
    }

    public String getCustomerId() {
        return prefs.getString(KEY_CUSTOMER_ID, "");
    }

    public void setAuthToken(String value) {
        prefs.edit().putString(KEY_AUTH_TOKEN, value).apply();
    }

    public String getAuthToken() {
        return prefs.getString(KEY_AUTH_TOKEN, "");
    }

    public void setReaderId(String value) {
        prefs.edit().putString(KEY_READER_ID, value).apply();
    }

    public String getReaderId() {
        return prefs.getString(KEY_READER_ID, "");
    }

    public void setLocationsJson(String value) {
        prefs.edit().putString(KEY_LOCATIONS_JSON, value).apply();
    }

    public String getLocationsJson() {
        return prefs.getString(KEY_LOCATIONS_JSON, "[]");
    }

    public void setBootstrapJson(String value) {
        prefs.edit().putString(KEY_BOOTSTRAP_JSON, value).apply();
    }

    public String getBootstrapJson() {
        return prefs.getString(KEY_BOOTSTRAP_JSON, "{}");
    }

    /** From bootstrap/login driver.deviceId — used for linen envelope before scanner connects. */
    public void setDriverHandheldDeviceId(String value) {
        prefs.edit().putString(KEY_DRIVER_HANDHELD_DEVICE_ID, value).apply();
    }

    public String getDriverHandheldDeviceId() {
        return prefs.getString(KEY_DRIVER_HANDHELD_DEVICE_ID, "");
    }

    /** Selected R2 scanner label after BLE flow — matches connection-status handheldId. */
    public void setScannerHandheldLabel(String value) {
        prefs.edit().putString(KEY_SCANNER_HANDHELD_LABEL, value).apply();
    }

    public String getScannerHandheldLabel() {
        return prefs.getString(KEY_SCANNER_HANDHELD_LABEL, "");
    }

    public void setActiveJobId(String value) {
        prefs.edit().putString(KEY_ACTIVE_JOB_ID, value).apply();
    }

    public String getActiveJobId() {
        return prefs.getString(KEY_ACTIVE_JOB_ID, "");
    }

    public void setActiveReaderId(String value) {
        prefs.edit().putString(KEY_ACTIVE_READER_ID, value).apply();
    }

    public String getActiveReaderId() {
        return prefs.getString(KEY_ACTIVE_READER_ID, "");
    }

    public void setActiveReaderWayId(String value) {
        prefs.edit().putString(KEY_ACTIVE_READER_WAY_ID, value).apply();
    }

    public String getActiveReaderWayId() {
        return prefs.getString(KEY_ACTIVE_READER_WAY_ID, "");
    }

    public boolean hasActiveTask() {
        return !getActiveJobId().isEmpty();
    }

    /** BLE-confirmed reader id, or task reader id before connection completes. */
    public String getEffectiveReaderIdForApi() {
        String fromBle = getReaderId();
        if (!fromBle.isEmpty()) {
            return fromBle;
        }
        return getActiveReaderId();
    }

    /**
     * Linen-movement envelope handheldDeviceId: scanner label if connected, else driver.deviceId from bootstrap, else login device id.
     */
    public String getHandheldDeviceIdForEnvelope() {
        String label = getScannerHandheldLabel();
        if (!label.isEmpty()) {
            return label;
        }
        String driverHandheld = getDriverHandheldDeviceId();
        if (!driverHandheld.isEmpty()) {
            return driverHandheld;
        }
        return getDeviceId();
    }

    public void clearActiveTask() {
        prefs.edit()
                .remove(KEY_ACTIVE_JOB_ID)
                .remove(KEY_ACTIVE_READER_ID)
                .remove(KEY_ACTIVE_READER_WAY_ID)
                .apply();
    }

    public void clear() {
        prefs.edit().clear().apply();
    }
}
