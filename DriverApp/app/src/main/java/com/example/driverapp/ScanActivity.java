package com.example.driverapp;

import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;
import androidx.core.content.ContextCompat;

import com.example.driverapp.view.CircularCounterView;
import com.rscja.deviceapi.RFIDWithUHFBLE;
import com.rscja.deviceapi.entity.InventoryParameter;
import com.rscja.deviceapi.entity.UHFTAGInfo;
import com.rscja.deviceapi.interfaces.ConnectionStatus;
import com.rscja.deviceapi.interfaces.IUHFInventoryCallback;
import com.rscja.deviceapi.interfaces.KeyEventCallback;

import java.util.HashSet;
import java.util.Locale;
import java.util.Set;

public class ScanActivity extends AppCompatActivity {

    public static final String EXTRA_LOCATION_NAME = "location_name";

    private CircularCounterView circularCounter;
    private TextView tvLocationName;
    private TextView tvScanStatus;
    private TextView tvScanConnStatus;
    private TextView tvScanBattery;
    private Button btnStop;
    private Button btnPause;
    private Button btnConfirmPickup;
    private ImageView ivBack;

    private final Handler handler = new Handler(Looper.getMainLooper());
    private ScanEventStore scanEventStore;
    private SessionStore sessionStore;

    /** Inventory cycle active on reader hardware */
    private boolean inventoryRunning = false;
    /** User tapped Pause */
    private boolean isPaused = false;
    /** User tapped Stop */
    private boolean userStopped = false;

    private final Set<String> sessionSeenEpcs = new HashSet<>();

    private final IUHFInventoryCallback inventoryCallback = this::onTagReadUiThread;

    private RFIDWithUHFBLE getUhf() {
        return Global.uhf();
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_scan);

        circularCounter = findViewById(R.id.circularCounter);
        tvLocationName = findViewById(R.id.tvLocationName);
        tvScanStatus = findViewById(R.id.tvScanStatus);
        tvScanConnStatus = findViewById(R.id.tvScanConnStatus);
        tvScanBattery = findViewById(R.id.tvScanBattery);
        btnStop = findViewById(R.id.btnStop);
        btnPause = findViewById(R.id.btnPause);
        btnConfirmPickup = findViewById(R.id.btnConfirmPickup);
        ivBack = findViewById(R.id.ivBack);
        scanEventStore = new ScanEventStore(this);
        sessionStore = new SessionStore(this);

        String routeLabel = getIntent().getStringExtra(TaskSelectActivity.EXTRA_ROUTE_LABEL);
        String locationName = getIntent().getStringExtra(EXTRA_LOCATION_NAME);
        if (routeLabel != null && !routeLabel.isEmpty()) {
            tvLocationName.setText(routeLabel);
        } else if (locationName != null) {
            tvLocationName.setText(locationName);
        }

        circularCounter.setMaxCount(200f);
        circularCounter.setCount(0);

        if (!sessionStore.hasActiveTask()) {
            Toast.makeText(this, "No active job selected. Go back and choose a task.", Toast.LENGTH_LONG).show();
        }

        ivBack.setOnClickListener(v -> {
            stopInventoryInternal();
            finish();
        });

        btnStop.setOnClickListener(v -> {
            userStopped = true;
            isPaused = false;
            stopInventoryInternal();
            btnPause.setText(getString(R.string.btn_pause));
            tvScanStatus.setText("Stopped");
            Toast.makeText(this, "Scan stopped", Toast.LENGTH_SHORT).show();
        });

        btnPause.setOnClickListener(v -> {
            if (!isBleConnected()) {
                Toast.makeText(this, R.string.scanner_bt_hint, Toast.LENGTH_LONG).show();
                return;
            }
            if (!sessionStore.hasActiveTask()) {
                Toast.makeText(this, "Select an active job before scanning.", Toast.LENGTH_LONG).show();
                return;
            }

            if (!inventoryRunning) {
                userStopped = false;
                isPaused = false;
                btnPause.setText(getString(R.string.btn_pause));
                startInventoryInternal();
                return;
            }

            isPaused = !isPaused;
            if (isPaused) {
                stopInventoryInternal();
                btnPause.setText("RESUME");
                tvScanStatus.setText("Paused");
            } else {
                btnPause.setText(getString(R.string.btn_pause));
                startInventoryInternal();
            }
        });

        btnConfirmPickup.setOnClickListener(v -> {
            stopInventoryInternal();
            Intent intent = new Intent(this, ConfirmPickupActivity.class);
            intent.putExtra(ConfirmPickupActivity.EXTRA_LOCATION, tvLocationName.getText().toString());
            intent.putExtra(ConfirmPickupActivity.EXTRA_COUNT, circularCounter.getCount());
            startActivity(intent);
        });
    }

    @Override
    protected void onResume() {
        super.onResume();
        updateConnectionUi();
        refreshBatteryUi();
        registerScannerKeys();

        if (shouldAutoStartInventory()) {
            startInventoryInternal();
        }
    }

    @Override
    protected void onPause() {
        handler.removeCallbacksAndMessages(null);
        getUhf().setKeyEventCallback(null);
        stopInventoryInternal();
        super.onPause();
    }

    private boolean shouldAutoStartInventory() {
        return isBleConnected()
                && sessionStore.hasActiveTask()
                && !isPaused
                && !userStopped;
    }

    private boolean isBleConnected() {
        return getUhf().getConnectStatus() == ConnectionStatus.CONNECTED;
    }

    private void updateConnectionUi() {
        boolean ok = isBleConnected();
        tvScanConnStatus.setText(ok ? getString(R.string.connected) : getString(R.string.not_connected));
        int color = ContextCompat.getColor(this, ok ? R.color.success : R.color.error);
        tvScanConnStatus.setTextColor(color);
    }

    private void refreshBatteryUi() {
        if (!isBleConnected()) {
            tvScanBattery.setText("—");
            return;
        }
        try {
            int bat = getUhf().getBattery();
            tvScanBattery.setText(bat >= 0 ? bat + "%" : "—");
        } catch (Throwable ignored) {
            tvScanBattery.setText("—");
        }
    }

    private void registerScannerKeys() {
        handler.postDelayed(() -> {
            if (isFinishing()) {
                return;
            }
            getUhf().setKeyEventCallback(new KeyEventCallback() {
                @Override
                public void onKeyDown(int keycode) {
                    if (!hasWindowFocus()) {
                        return;
                    }
                    if (getUhf().getConnectStatus() != ConnectionStatus.CONNECTED) {
                        return;
                    }
                    if (!sessionStore.hasActiveTask()) {
                        return;
                    }
                    // Match uhf-ble-demo pattern: trigger key toggles inventory.
                    if (keycode == 1) {
                        if (inventoryRunning) {
                            stopInventoryInternal();
                            isPaused = true;
                            runOnUiThread(() -> {
                                btnPause.setText("RESUME");
                                tvScanStatus.setText("Paused");
                            });
                        } else {
                            userStopped = false;
                            isPaused = false;
                            runOnUiThread(() -> btnPause.setText(getString(R.string.btn_pause)));
                            startInventoryInternal();
                        }
                    }
                }

                @Override
                public void onKeyUp(int keycode) {
                    if (keycode == 4) {
                        stopInventoryInternal();
                    }
                }
            });
        }, 200);
    }

    private void startInventoryInternal() {
        if (!sessionStore.hasActiveTask()) {
            Toast.makeText(this, "Select an active job before scanning.", Toast.LENGTH_LONG).show();
            tvScanStatus.setText("No active job");
            return;
        }
        if (!isBleConnected()) {
            Toast.makeText(this, R.string.scanner_bt_hint, Toast.LENGTH_LONG).show();
            tvScanStatus.setText(getString(R.string.not_connected));
            return;
        }
        if (inventoryRunning) {
            return;
        }

        getUhf().setInventoryCallback(inventoryCallback);
        InventoryParameter inventoryParameter = new InventoryParameter();
        inventoryParameter.setResultData(new InventoryParameter.ResultData().setNeedPhase(false));

        boolean ok = getUhf().startInventoryTag(inventoryParameter);
        if (!ok) {
            Toast.makeText(this, "Could not start inventory", Toast.LENGTH_SHORT).show();
            getUhf().setInventoryCallback(null);
            return;
        }

        inventoryRunning = true;
        tvScanStatus.setText(getString(R.string.scanning_status));
        refreshBatteryUi();
    }

    private void stopInventoryInternal() {
        if (!inventoryRunning) {
            getUhf().setInventoryCallback(null);
            return;
        }
        try {
            getUhf().stopInventory();
        } catch (Throwable ignored) {
        }
        inventoryRunning = false;
        getUhf().setInventoryCallback(null);
    }

    private void onTagReadUiThread(UHFTAGInfo uhftagInfo) {
        runOnUiThread(() -> applyTag(uhftagInfo));
    }

    private void applyTag(UHFTAGInfo uhftagInfo) {
        String epc = normalizeEpc(uhftagInfo);
        if (epc.isEmpty()) {
            return;
        }
        if (!sessionSeenEpcs.add(epc)) {
            return;
        }
        if (!scanEventStore.addEvent(epc)) {
            stopInventoryInternal();
            tvScanStatus.setText("Stopped");
            Toast.makeText(this, "Select an active job before scanning.", Toast.LENGTH_LONG).show();
            return;
        }
        circularCounter.incrementCount();
        ScanFeedback.onUniqueTagScanned(this);
        refreshBatteryUi();
    }

    private static String normalizeEpc(UHFTAGInfo info) {
        if (info == null) {
            return "";
        }
        String epc = info.getEPC();
        if (epc == null) {
            return "";
        }
        return epc.replace(" ", "").trim().toUpperCase(Locale.US);
    }

    @Override
    protected void onDestroy() {
        handler.removeCallbacksAndMessages(null);
        super.onDestroy();
    }
}
