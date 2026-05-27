package com.example.driverapp;

import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class SyncStatusActivity extends AppCompatActivity {

    private Button tabAll, tabPending, tabFailed, tabUploaded;
    private Button btnSyncAll;
    private int selectedTab = 0;
    private TextView tvEmptySync;
    private SyncStatusAdapter adapter;
    private final ExecutorService ioExecutor = Executors.newSingleThreadExecutor();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_sync_status);

        ImageView ivBack = findViewById(R.id.ivBack);
        tabAll = findViewById(R.id.tabAll);
        tabPending = findViewById(R.id.tabPending);
        tabFailed = findViewById(R.id.tabFailed);
        tabUploaded = findViewById(R.id.tabUploaded);
        btnSyncAll = findViewById(R.id.btnSyncAll);
        tvEmptySync = findViewById(R.id.tvEmptySync);
        RecyclerView rvSyncRecords = findViewById(R.id.rvSyncRecords);
        rvSyncRecords.setLayoutManager(new LinearLayoutManager(this));
        adapter = new SyncStatusAdapter();
        rvSyncRecords.setAdapter(adapter);

        ivBack.setOnClickListener(v -> finish());

        tabAll.setOnClickListener(v -> selectTab(0));
        tabPending.setOnClickListener(v -> selectTab(1));
        tabFailed.setOnClickListener(v -> selectTab(2));
        tabUploaded.setOnClickListener(v -> selectTab(3));

        btnSyncAll.setOnClickListener(v -> syncAllPending());

        selectTab(0);
    }

    private void selectTab(int index) {
        selectedTab = index;
        resetTabStyles();
        Button[] tabs = {tabAll, tabPending, tabFailed, tabUploaded};
        tabs[index].setBackgroundResource(R.drawable.bg_tab_selected);
        tabs[index].setTextColor(getColor(R.color.white));
        renderRecords();
    }

    private void resetTabStyles() {
        Button[] tabs = {tabAll, tabPending, tabFailed, tabUploaded};
        for (Button tab : tabs) {
            tab.setBackgroundResource(android.R.color.transparent);
            tab.setTextColor(getColor(R.color.text_secondary));
        }
    }

    private void syncAllPending() {
        ScanEventStore eventStore = new ScanEventStore(this);
        int pendingCount = eventStore.getPendingCount();
        if (pendingCount == 0) {
            Toast.makeText(this, "No pending events to sync.", Toast.LENGTH_SHORT).show();
            return;
        }
        btnSyncAll.setEnabled(false);
        Toast.makeText(this, "Syncing " + pendingCount + " events…", Toast.LENGTH_SHORT).show();

        SessionStore sessionStore = new SessionStore(this);
        ApiClient apiClient = new ApiClient();
        JSONArray snapshot = eventStore.copyPendingEvents();

        ioExecutor.execute(() -> {
            ApiClient.ApiResult result = apiClient.syncAll(sessionStore, eventStore.getPendingEvents());
            runOnUiThread(() -> {
                btnSyncAll.setEnabled(true);
                if (!ApiClient.isHttpOk(result)) {
                    Toast.makeText(this, "Sync failed: " + result.error, Toast.LENGTH_LONG).show();
                    return;
                }
                JSONArray results = result.body.optJSONArray("results");
                ScanEventStore.ApplyMovementOutcome outcome =
                        eventStore.applyMovementUploadResults(results, snapshot);

                boolean okFlag = result.body.optBoolean("success", true);
                String msg = String.format(Locale.US,
                        "Sync: %d synced, %d rejected.",
                        outcome.acceptedOrDuplicate,
                        outcome.rejectedRecorded);
                if (!okFlag && outcome.rejectedRecorded > 0) {
                    msg = "Completed with rejections. " + msg;
                }
                Toast.makeText(this, msg, Toast.LENGTH_LONG).show();
                renderRecords();
            });
        });
    }

    private void renderRecords() {
        List<SyncRecordItem> rows = loadRowsForTab(selectedTab);
        adapter.setItems(rows);
        tvEmptySync.setVisibility(rows.isEmpty() ? View.VISIBLE : View.GONE);
    }

    private List<SyncRecordItem> loadRowsForTab(int tab) {
        ScanEventStore store = new ScanEventStore(this);
        List<SyncRecordItem> out = new ArrayList<>();
        if (tab == 0 || tab == 1) {
            appendPendingRows(out, store.getPendingEvents());
        }
        if (tab == 0 || tab == 2) {
            appendFailedRows(out, store.getFailedEventsForUi());
        }
        if (tab == 0 || tab == 3) {
            appendUploadedRows(out, store.getUploadedEvents());
        }
        return out;
    }

    private void appendPendingRows(List<SyncRecordItem> out, JSONArray arr) {
        for (int i = 0; i < arr.length(); i++) {
            JSONObject o = arr.optJSONObject(i);
            if (o == null) {
                continue;
            }
            String tag = o.optString("rfidTag", "");
            String occurredAt = o.optString("occurredAt", "");
            out.add(new SyncRecordItem(
                    "Tag " + shortenTag(tag),
                    "Pending upload",
                    occurredAt,
                    SyncRecordItem.STATUS_PENDING
            ));
        }
    }

    private void appendFailedRows(List<SyncRecordItem> out, JSONArray arr) {
        for (int i = 0; i < arr.length(); i++) {
            JSONObject o = arr.optJSONObject(i);
            if (o == null) {
                continue;
            }
            String tag = o.optString("rfidTag", "");
            String reason = o.optString("reason", "Rejected by server");
            String recordedAt = o.optString("recordedAt", "");
            out.add(new SyncRecordItem(
                    "Tag " + shortenTag(tag),
                    reason,
                    recordedAt,
                    SyncRecordItem.STATUS_FAILED
            ));
        }
    }

    private void appendUploadedRows(List<SyncRecordItem> out, JSONArray arr) {
        for (int i = 0; i < arr.length(); i++) {
            JSONObject o = arr.optJSONObject(i);
            if (o == null) {
                continue;
            }
            String tag = o.optString("rfidTag", "");
            String status = o.optString("status", "accepted");
            String recordedAt = o.optString("recordedAt", "");
            out.add(new SyncRecordItem(
                    "Tag " + shortenTag(tag),
                    "Server status: " + status,
                    recordedAt,
                    SyncRecordItem.STATUS_UPLOADED
            ));
        }
    }

    private String shortenTag(String tag) {
        if (tag == null || tag.isEmpty()) {
            return "unknown";
        }
        if (tag.length() <= 10) {
            return tag;
        }
        return tag.substring(tag.length() - 10);
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        ioExecutor.shutdownNow();
    }
}
