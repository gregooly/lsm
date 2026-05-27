package com.example.driverapp;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.widget.Button;
import android.widget.CompoundButton;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.Switch;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;
import androidx.core.content.ContextCompat;

import com.rscja.deviceapi.RFIDWithUHFBLE;
import com.rscja.deviceapi.interfaces.ConnectionStatus;

/**
 * Preferences persisted like uhf-ble-demo {@code SPUtils} / {@code UHFSetFragment} auto-reconnect,
 * plus sound/vibration gates for scan feedback (Driver extension).
 */
public class SettingsActivity extends AppCompatActivity {

    private TextView tvSettingsScannerName;
    private TextView tvSettingsScannerMac;
    private TextView tvSettingsBattery;
    private TextView tvSettingsConnBadge;
    private Switch switchAutoConnect;
    private Switch switchVibration;
    private Switch switchSound;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_settings);

        ImageView ivBack = findViewById(R.id.ivBack);
        tvSettingsScannerName = findViewById(R.id.tvSettingsScannerName);
        tvSettingsScannerMac = findViewById(R.id.tvSettingsScannerMac);
        tvSettingsBattery = findViewById(R.id.tvSettingsBattery);
        tvSettingsConnBadge = findViewById(R.id.tvSettingsConnBadge);
        switchAutoConnect = findViewById(R.id.switchAutoConnect);
        switchVibration = findViewById(R.id.switchVibration);
        switchSound = findViewById(R.id.switchSound);
        LinearLayout actionClearCache = findViewById(R.id.actionClearCache);
        LinearLayout actionAbout = findViewById(R.id.actionAbout);
        Button btnLogout = findViewById(R.id.btnLogout);

        ivBack.setOnClickListener(v -> finish());

        bindPreferenceToggles();

        actionClearCache.setOnClickListener(v -> {
            Global.clearScannerLinkPrefs(this);
            DriverApplication.getInstance().cancelBleReconnect();
            Toast.makeText(this, R.string.scanner_cache_cleared, Toast.LENGTH_SHORT).show();
        });

        actionAbout.setOnClickListener(v -> showAbout());

        btnLogout.setOnClickListener(v -> {
            DriverApplication.getInstance().cancelBleReconnect();
            DriverApplication.getInstance().setUserRequestedDisconnect(true);
            Global.clearScannerLinkPrefs(this);

            RFIDWithUHFBLE uhf = Global.uhf();
            try {
                uhf.stopInventory();
            } catch (Throwable ignored) {
            }
            uhf.setInventoryCallback(null);
            uhf.setKeyEventCallback(null);
            try {
                uhf.disconnect();
            } catch (Throwable ignored) {
            }

            ScanEventStore scanEventStore = new ScanEventStore(this);
            scanEventStore.clearAll();
            scanEventStore.clearFailedEvents();
            scanEventStore.clearUploadedEvents();
            new SessionStore(this).clear();
            Intent intent = new Intent(this, LoginActivity.class);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
            startActivity(intent);
        });
    }

    @Override
    protected void onResume() {
        super.onResume();
        refreshScannerCard();
    }

    private void bindPreferenceToggles() {
        switchAutoConnect.setOnCheckedChangeListener(null);
        switchVibration.setOnCheckedChangeListener(null);
        switchSound.setOnCheckedChangeListener(null);

        switchAutoConnect.setChecked(Global.isAutoReconnect(this));
        switchVibration.setChecked(Global.isVibrationEnabled(this));
        switchSound.setChecked(Global.isSoundEnabled(this));

        switchAutoConnect.setOnCheckedChangeListener((CompoundButton buttonView, boolean isChecked) ->
                Global.setAutoReconnect(SettingsActivity.this, isChecked));

        switchVibration.setOnCheckedChangeListener((CompoundButton buttonView, boolean isChecked) ->
                Global.setVibrationEnabled(SettingsActivity.this, isChecked));

        switchSound.setOnCheckedChangeListener((CompoundButton buttonView, boolean isChecked) ->
                Global.setSoundEnabled(SettingsActivity.this, isChecked));
    }

    @SuppressLint("MissingPermission")
    private void refreshScannerCard() {
        RFIDWithUHFBLE uhf = Global.uhf();
        SessionStore sessionStore = new SessionStore(this);
        boolean bleOk = uhf.getConnectStatus() == ConnectionStatus.CONNECTED;
        boolean backendLinked = !sessionStore.getReaderId().isEmpty();

        if (bleOk && backendLinked) {
            tvSettingsConnBadge.setText(getString(R.string.connected));
            tvSettingsConnBadge.setTextColor(ContextCompat.getColor(this, R.color.success));
            String label = sessionStore.getScannerHandheldLabel();
            tvSettingsScannerName.setText(!label.isEmpty() ? label : "—");
            String mac = Global.getLastBleMac(this);
            tvSettingsScannerMac.setText(!mac.isEmpty() ? mac : "—");
            try {
                int bat = uhf.getBattery();
                tvSettingsBattery.setText(bat >= 0 ? bat + "%" : "—");
            } catch (Throwable ignored) {
                tvSettingsBattery.setText("—");
            }
        } else {
            tvSettingsConnBadge.setText(getString(R.string.not_connected));
            tvSettingsConnBadge.setTextColor(ContextCompat.getColor(this, R.color.error));
            tvSettingsScannerName.setText("—");
            tvSettingsScannerMac.setText("—");
            tvSettingsBattery.setText("—");
        }
    }

    private void showAbout() {
        try {
            String vn = getPackageManager().getPackageInfo(getPackageName(), 0).versionName;
            Toast.makeText(this, getString(R.string.about_app_line, getString(R.string.app_name), vn), Toast.LENGTH_LONG).show();
        } catch (PackageManager.NameNotFoundException e) {
            Toast.makeText(this, getString(R.string.app_name), Toast.LENGTH_SHORT).show();
        }
    }
}
