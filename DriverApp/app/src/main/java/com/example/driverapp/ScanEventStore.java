package com.example.driverapp;

import android.content.Context;
import android.content.SharedPreferences;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.UUID;

public class ScanEventStore {

    private static final String PREFS = "scan_events";
    private static final String KEY_PENDING_EVENTS = "pending_events";
    private static final String KEY_FAILED_EVENTS = "failed_events";
    private static final String KEY_UPLOADED_EVENTS = "uploaded_events";

    private final SharedPreferences prefs;
    private final SessionStore sessionStore;

    public ScanEventStore(Context context) {
        prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        sessionStore = new SessionStore(context);
    }

    public static final class ApplyMovementOutcome {
        public final int acceptedOrDuplicate;
        public final int rejectedRecorded;

        public ApplyMovementOutcome(int acceptedOrDuplicate, int rejectedRecorded) {
            this.acceptedOrDuplicate = acceptedOrDuplicate;
            this.rejectedRecorded = rejectedRecorded;
        }
    }

    public synchronized boolean addEvent(String rfidTag) {
        if (!sessionStore.hasActiveTask()) {
            return false;
        }
        JSONArray events = getPendingEvents();
        JSONObject event = new JSONObject();
        try {
            event.put("idempotencyKey", UUID.randomUUID().toString());
            event.put("rfidTag", rfidTag);
            event.put("occurredAt", TimeUtils.utcNowIso());
            JsonUtils.putCoercedNumber(event, "jobId", sessionStore.getActiveJobId());
            JsonUtils.putCoercedNumber(event, "readerId", sessionStore.getEffectiveReaderIdForApi());
            JsonUtils.putCoercedNumber(event, "readerWayId", sessionStore.getActiveReaderWayId());
            JsonUtils.putCoercedNumber(event, "driverId", sessionStore.getDriverId());
            events.put(event);
            save(events);
        } catch (JSONException ignored) {
            return false;
        }
        return true;
    }

    public synchronized JSONArray getPendingEvents() {
        String raw = prefs.getString(KEY_PENDING_EVENTS, "[]");
        try {
            return new JSONArray(raw);
        } catch (JSONException e) {
            return new JSONArray();
        }
    }

    public synchronized int getPendingCount() {
        return getPendingEvents().length();
    }

    public synchronized void clearAll() {
        save(new JSONArray());
    }

    public synchronized void clearFailedEvents() {
        prefs.edit().putString(KEY_FAILED_EVENTS, "[]").apply();
    }

    public synchronized void clearUploadedEvents() {
        prefs.edit().putString(KEY_UPLOADED_EVENTS, "[]").apply();
    }

    /**
     * Apply server {@code results} for the uploaded batch. Removes accepted/duplicate from pending.
     * Records rejected events (with reason) in a separate JSON list for troubleshooting.
     */
    public synchronized ApplyMovementOutcome applyMovementUploadResults(JSONArray results, JSONArray uploadedSnapshot) {
        JSONArray pending = getPendingEvents();
        if (results == null || results.length() == 0) {
            JSONArray filtered = removePendingMatchingSnapshot(pending, uploadedSnapshot);
            appendUploadedFromSnapshot(uploadedSnapshot, "accepted");
            save(filtered);
            int n = uploadedSnapshot != null ? uploadedSnapshot.length() : 0;
            return new ApplyMovementOutcome(n, 0);
        }

        int acceptedDup = 0;
        int rejected = 0;

        JSONArray newPending = new JSONArray();
        for (int i = 0; i < pending.length(); i++) {
            JSONObject ev = pending.optJSONObject(i);
            if (ev == null) {
                continue;
            }
            String key = ev.optString("idempotencyKey", "");
            JSONObject row = findResultRow(results, key);
            if (row == null) {
                newPending.put(ev);
                continue;
            }
            String status = row.optString("status", "").toLowerCase();
            if ("accepted".equals(status) || "duplicate".equals(status)) {
                appendUploadedEvent(ev, status);
                acceptedDup++;
                continue;
            }
            if ("rejected".equals(status)) {
                appendFailedEvent(ev, row.optString("reason", ""));
                rejected++;
                continue;
            }
            newPending.put(ev);
        }
        save(newPending);
        return new ApplyMovementOutcome(acceptedDup, rejected);
    }

    private static JSONObject findResultRow(JSONArray results, String idempotencyKey) {
        if (idempotencyKey.isEmpty()) {
            return null;
        }
        for (int i = 0; i < results.length(); i++) {
            JSONObject row = results.optJSONObject(i);
            if (row != null && idempotencyKey.equals(row.optString("idempotencyKey", ""))) {
                return row;
            }
        }
        return null;
    }

    private static JSONArray removePendingMatchingSnapshot(JSONArray pending, JSONArray snapshot) {
        JSONArray keep = new JSONArray();
        if (snapshot == null || snapshot.length() == 0) {
            return pending;
        }
        for (int i = 0; i < pending.length(); i++) {
            JSONObject ev = pending.optJSONObject(i);
            if (ev == null) {
                continue;
            }
            String key = ev.optString("idempotencyKey", "");
            if (snapshotContainsKey(snapshot, key)) {
                continue;
            }
            keep.put(ev);
        }
        return keep;
    }

    private static boolean snapshotContainsKey(JSONArray snapshot, String key) {
        if (key.isEmpty()) {
            return false;
        }
        for (int i = 0; i < snapshot.length(); i++) {
            JSONObject o = snapshot.optJSONObject(i);
            if (o != null && key.equals(o.optString("idempotencyKey", ""))) {
                return true;
            }
        }
        return false;
    }

    private void appendFailedEvent(JSONObject originalEvent, String reason) {
        try {
            JSONArray failed = getFailedEvents();
            JSONObject row = new JSONObject();
            row.put("idempotencyKey", originalEvent.optString("idempotencyKey", ""));
            row.put("rfidTag", originalEvent.optString("rfidTag", ""));
            row.put("occurredAt", originalEvent.optString("occurredAt", ""));
            row.put("reason", reason);
            row.put("recordedAt", TimeUtils.utcNowIso());
            failed.put(row);
            prefs.edit().putString(KEY_FAILED_EVENTS, failed.toString()).apply();
        } catch (JSONException ignored) {
        }
    }

    private JSONArray getFailedEvents() {
        String raw = prefs.getString(KEY_FAILED_EVENTS, "[]");
        try {
            return new JSONArray(raw);
        } catch (JSONException e) {
            return new JSONArray();
        }
    }

    public synchronized JSONArray getUploadedEvents() {
        String raw = prefs.getString(KEY_UPLOADED_EVENTS, "[]");
        try {
            return new JSONArray(raw);
        } catch (JSONException e) {
            return new JSONArray();
        }
    }

    public synchronized JSONArray getFailedEventsForUi() {
        return getFailedEvents();
    }

    private void appendUploadedFromSnapshot(JSONArray snapshot, String status) {
        if (snapshot == null) {
            return;
        }
        for (int i = 0; i < snapshot.length(); i++) {
            JSONObject ev = snapshot.optJSONObject(i);
            if (ev != null) {
                appendUploadedEvent(ev, status);
            }
        }
    }

    private void appendUploadedEvent(JSONObject event, String status) {
        try {
            JSONArray uploaded = getUploadedEvents();
            JSONObject row = new JSONObject();
            row.put("idempotencyKey", event.optString("idempotencyKey", ""));
            row.put("rfidTag", event.optString("rfidTag", ""));
            row.put("occurredAt", event.optString("occurredAt", ""));
            row.put("status", status);
            row.put("recordedAt", TimeUtils.utcNowIso());
            uploaded.put(row);
            prefs.edit().putString(KEY_UPLOADED_EVENTS, uploaded.toString()).apply();
        } catch (JSONException ignored) {
        }
    }

    private void save(JSONArray events) {
        prefs.edit().putString(KEY_PENDING_EVENTS, events.toString()).apply();
    }

    /**
     * Snapshot pending queue before upload (deep copy).
     */
    public JSONArray copyPendingEvents() {
        try {
            return new JSONArray(getPendingEvents().toString());
        } catch (JSONException e) {
            return new JSONArray();
        }
    }
}
