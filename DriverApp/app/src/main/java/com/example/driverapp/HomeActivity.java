package com.example.driverapp;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.core.content.ContextCompat;

import com.rscja.deviceapi.RFIDWithUHFBLE;
import com.rscja.deviceapi.interfaces.ConnectionStatus;

public class HomeActivity extends AppCompatActivity {

    private Button btnStartPickup;
    private Button btnSyncData;
    private LinearLayout btnSettings;
    private LinearLayout cardScanner;
    private TextView tvHomeScannerStatus;
    private TextView tvHomeScannerName;
    private TextView tvHomeBattery;

    private TextView tvPendingUploadCount;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_home);

        btnStartPickup = findViewById(R.id.btnStartPickup);
        btnSyncData = findViewById(R.id.btnSyncData);
        btnSettings = findViewById(R.id.btnSettings);
        cardScanner = findViewById(R.id.cardScanner);
        tvHomeScannerStatus = findViewById(R.id.tvHomeScannerStatus);
        tvHomeScannerName = findViewById(R.id.tvHomeScannerName);
        tvHomeBattery = findViewById(R.id.tvHomeBattery);
        tvPendingUploadCount = findViewById(R.id.tvPendingUploadCount);

        btnStartPickup.setOnClickListener(v -> {
            Intent intent = new Intent(this, TaskSelectActivity.class);
            startActivity(intent);
        });

        btnSyncData.setOnClickListener(v -> {
            Intent intent = new Intent(this, SyncStatusActivity.class);
            startActivity(intent);
        });

        btnSettings.setOnClickListener(v -> {
            Intent intent = new Intent(this, SettingsActivity.class);
            startActivity(intent);
        });

        cardScanner.setOnClickListener(v -> {
            Intent intent = new Intent(this, ScannerConnectActivity.class);
            startActivity(intent);
        });
    }

    @Override
    protected void onResume() {
        super.onResume();
        refreshScannerCard();
        tvPendingUploadCount.setText(String.valueOf(new ScanEventStore(this).getPendingCount()));
    }

    private void refreshScannerCard() {
        RFIDWithUHFBLE uhf = Global.uhf();
        SessionStore sessionStore = new SessionStore(this);
        boolean bleOk = uhf.getConnectStatus() == ConnectionStatus.CONNECTED;
        boolean backendLinked = !sessionStore.getReaderId().isEmpty();

        if (bleOk && backendLinked) {
            tvHomeScannerStatus.setText(getString(R.string.connected));
            tvHomeScannerStatus.setTextColor(ContextCompat.getColor(this, R.color.white));
            String label = sessionStore.getScannerHandheldLabel();
            tvHomeScannerName.setText(!label.isEmpty() ? label : "—");
            try {
                int bat = uhf.getBattery();
                tvHomeBattery.setText(bat >= 0 ? bat + "%" : "—");
            } catch (Throwable ignored) {
                tvHomeBattery.setText("—");
            }
        } else {
            tvHomeScannerStatus.setText(getString(R.string.not_connected));
            tvHomeScannerStatus.setTextColor(ContextCompat.getColor(this, R.color.white));
            tvHomeScannerName.setText("—");
            tvHomeBattery.setText("—");
        }
    }

    @Override
    public void onBackPressed() {
        // Prevent going back to login with double-press
        moveTaskToBack(true);
    }
}
