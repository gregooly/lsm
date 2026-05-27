package com.example.driverapp;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;

import org.json.JSONArray;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class ConfirmPickupActivity extends AppCompatActivity {

    public static final String EXTRA_LOCATION = "location";
    public static final String EXTRA_COUNT = "count";
    private final ExecutorService ioExecutor = Executors.newSingleThreadExecutor();
    private Button btnSendData;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_confirm_pickup);

        ImageView ivBack = findViewById(R.id.ivBack);
        TextView tvLocation = findViewById(R.id.tvLocation);
        TextView tvItemsCount = findViewById(R.id.tvItemsCount);
        TextView tvDateTime = findViewById(R.id.tvDateTime);
        TextView tvDriverId = findViewById(R.id.tvDriverId);
        btnSendData = findViewById(R.id.btnSendData);
        Button btnBack = findViewById(R.id.btnBack);

        String location = getIntent().getStringExtra(EXTRA_LOCATION);
        int count = getIntent().getIntExtra(EXTRA_COUNT, 0);

        if (location != null) {
            tvLocation.setText(location);
        }
        tvItemsCount.setText(String.valueOf(count));

        SimpleDateFormat sdf = new SimpleDateFormat("MMM dd, yyyy  hh:mm a", Locale.getDefault());
        tvDateTime.setText(sdf.format(new Date()));

        ivBack.setOnClickListener(v -> finish());

        btnBack.setOnClickListener(v -> finish());

        SessionStore sessionStore = new SessionStore(this);
        String driverName = sessionStore.getDriverName();
        if (!driverName.isEmpty()) {
            tvDriverId.setText(driverName);
        } else if (!sessionStore.getDriverId().isEmpty()) {
            tvDriverId.setText(sessionStore.getDriverId());
        } else {
            tvDriverId.setText("—");
        }

        btnSendData.setOnClickListener(v -> sendScannedResult());
    }

    private void sendScannedResult() {
        ScanEventStore eventStore = new ScanEventStore(this);
        if (eventStore.getPendingCount() == 0) {
            Toast.makeText(this, "No pending scans to send.", Toast.LENGTH_SHORT).show();
            return;
        }

        SessionStore sessionStore = new SessionStore(this);
        ApiClient apiClient = new ApiClient();
        btnSendData.setEnabled(false);

        JSONArray snapshot = eventStore.copyPendingEvents();

        ioExecutor.execute(() -> {
            ApiClient.ApiResult result = apiClient.uploadScannedResult(sessionStore, eventStore.getPendingEvents());
            runOnUiThread(() -> {
                btnSendData.setEnabled(true);
                if (!ApiClient.isHttpOk(result)) {
                    Toast.makeText(this, "Send failed: " + result.error, Toast.LENGTH_LONG).show();
                    return;
                }
                JSONArray results = result.body.optJSONArray("results");
                ScanEventStore.ApplyMovementOutcome outcome =
                        eventStore.applyMovementUploadResults(results, snapshot);

                boolean okFlag = result.body.optBoolean("success", true);
                String msg = String.format(Locale.US,
                        "Processed: %d synced, %d rejected.",
                        outcome.acceptedOrDuplicate,
                        outcome.rejectedRecorded);
                if (!okFlag && outcome.rejectedRecorded > 0) {
                    msg = "Completed with rejections. " + msg;
                }
                Toast.makeText(this, msg, Toast.LENGTH_LONG).show();

                Intent intent = new Intent(this, SyncStatusActivity.class);
                intent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_NEW_TASK);
                startActivity(intent);
                finish();
            });
        });
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        ioExecutor.shutdownNow();
    }
}
