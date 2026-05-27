package com.example.driverapp;

import android.Manifest;
import android.annotation.SuppressLint;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.rscja.deviceapi.RFIDWithUHFBLE;
import com.rscja.deviceapi.interfaces.ConnectionStatus;
import com.rscja.deviceapi.interfaces.ScanBTCallback;

import org.json.JSONObject;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class ScannerConnectActivity extends AppCompatActivity implements DriverApplication.BleConnectionUiObserver {

    private static final int STATE_NOT_CONNECTED = 0;
    private static final int STATE_SEARCHING = 1;
    private static final int STATE_CONNECTING = 2;
    private static final int STATE_CONNECTED = 3;

    private static final int REQ_BLE = 999;
    private static final long SCAN_PERIOD_MS = 10_000L;

    private LinearLayout stateNotConnected;
    private LinearLayout stateSearching;
    private LinearLayout stateConnecting;
    private LinearLayout stateConnected;

    private Button btnSearchDevices;
    private Button btnStopSearch;
    private Button btnCancelConnect;
    private Button btnContinueHome;

    private RecyclerView rvBleDevices;
    private TextView tvBleEmpty;
    private TextView tvConnectingSubtitle;
    private TextView tvConnectedScannerName;
    private TextView tvConnectedBattery;
    private TextView tvConnectedMac;

    private final Handler handler = new Handler(Looper.getMainLooper());
    private final ExecutorService ioExecutor = Executors.newSingleThreadExecutor();
    private final ApiClient apiClient = new ApiClient();
    private SessionStore sessionStore;
    private BleDeviceAdapter bleDeviceAdapter;

    private String deviceId;
    private String selectedHandheldId = "";

    private boolean bleScanning = false;
    private boolean userCanceledConnect = false;

    private int currentState = STATE_NOT_CONNECTED;

    private final Runnable stopBleScanRunnable = () -> {
        bleScanning = false;
        try {
            getUhf().stopScanBTDevices();
        } catch (Throwable ignored) {
        }
        runOnUiThread(() -> {
            if (currentState == STATE_SEARCHING && bleDeviceAdapter != null && bleDeviceAdapter.isEmpty()) {
                tvBleEmpty.setVisibility(View.VISIBLE);
            }
        });
    };

    private final ActivityResultLauncher<Intent> enableBtLauncher =
            registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {
                BluetoothAdapter adapter = BluetoothAdapter.getDefaultAdapter();
                if (adapter != null && adapter.isEnabled()) {
                    beginScanAfterPermissions();
                } else {
                    Toast.makeText(this, R.string.scanner_not_connected_msg, Toast.LENGTH_LONG).show();
                    showState(STATE_NOT_CONNECTED);
                }
            });

    private RFIDWithUHFBLE getUhf() {
        return Global.uhf();
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_scanner_connect);

        sessionStore = new SessionStore(this);
        deviceId = getIntent().getStringExtra("device_id");
        if (deviceId == null || deviceId.isEmpty()) {
            deviceId = sessionStore.getDeviceId();
        }

        initViews();
        bleDeviceAdapter = new BleDeviceAdapter(this::onBleDevicePicked);
        rvBleDevices.setLayoutManager(new LinearLayoutManager(this));
        rvBleDevices.setAdapter(bleDeviceAdapter);

        ImageView ivBack = findViewById(R.id.ivBack);
        ivBack.setOnClickListener(v -> finish());

        setupListeners();
        showState(STATE_NOT_CONNECTED);
    }

    @Override
    protected void onResume() {
        super.onResume();
        DriverApplication.getInstance().setBleConnectionUiObserver(this);
        if (getUhf().getConnectStatus() == ConnectionStatus.CONNECTED
                && !sessionStore.getReaderId().isEmpty()
                && !sessionStore.getScannerHandheldLabel().isEmpty()) {
            selectedHandheldId = sessionStore.getScannerHandheldLabel();
            populateConnectedLabelsFromBle();
            showState(STATE_CONNECTED);
        }
    }

    @Override
    protected void onPause() {
        DriverApplication.getInstance().setBleConnectionUiObserver(null);
        super.onPause();
    }

    @Override
    protected void onStop() {
        stopBleDiscoveryOnly();
        super.onStop();
    }

    private void initViews() {
        stateNotConnected = findViewById(R.id.stateNotConnected);
        stateSearching = findViewById(R.id.stateSearching);
        stateConnecting = findViewById(R.id.stateConnecting);
        stateConnected = findViewById(R.id.stateConnected);

        btnSearchDevices = findViewById(R.id.btnSearchDevices);
        btnStopSearch = findViewById(R.id.btnStopSearch);
        btnCancelConnect = findViewById(R.id.btnCancelConnect);
        btnContinueHome = findViewById(R.id.btnContinueHome);

        rvBleDevices = findViewById(R.id.rvBleDevices);
        tvBleEmpty = findViewById(R.id.tvBleEmpty);
        tvConnectingSubtitle = findViewById(R.id.tvConnectingSubtitle);
        tvConnectedScannerName = findViewById(R.id.tvConnectedScannerName);
        tvConnectedBattery = findViewById(R.id.tvConnectedBattery);
        tvConnectedMac = findViewById(R.id.tvConnectedMac);
    }

    private void setupListeners() {
        btnSearchDevices.setOnClickListener(v -> onSearchClicked());
        btnStopSearch.setOnClickListener(v -> {
            stopBleDiscoveryOnly();
            showState(STATE_NOT_CONNECTED);
        });

        btnCancelConnect.setOnClickListener(v -> {
            userCanceledConnect = true;
            DriverApplication.getInstance().setUserRequestedDisconnect(true);
            handler.removeCallbacksAndMessages(null);
            try {
                getUhf().disconnect();
            } catch (Throwable ignored) {
            }
            showState(STATE_NOT_CONNECTED);
        });

        btnContinueHome.setOnClickListener(v -> {
            Intent intent = new Intent(this, HomeActivity.class);
            startActivity(intent);
            finish();
        });
    }

    private void onSearchClicked() {
        if (!getPackageManager().hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE)) {
            Toast.makeText(this, R.string.ble_not_supported, Toast.LENGTH_LONG).show();
            return;
        }
        BluetoothAdapter adapter = BluetoothAdapter.getDefaultAdapter();
        if (adapter == null) {
            Toast.makeText(this, R.string.scanner_not_connected_msg, Toast.LENGTH_SHORT).show();
            return;
        }
        if (!adapter.isEnabled()) {
            enableBtLauncher.launch(new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE));
            return;
        }
        if (!hasBleScanPermissions()) {
            requestBleScanPermissions();
            return;
        }
        beginScanAfterPermissions();
    }

    private boolean hasBleScanPermissions() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            return ActivityCompat.checkSelfPermission(this, Manifest.permission.BLUETOOTH_SCAN) == PackageManager.PERMISSION_GRANTED
                    && ActivityCompat.checkSelfPermission(this, Manifest.permission.BLUETOOTH_CONNECT) == PackageManager.PERMISSION_GRANTED
                    && ActivityCompat.checkSelfPermission(this, Manifest.permission.BLUETOOTH_ADVERTISE) == PackageManager.PERMISSION_GRANTED;
        }
        return ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED;
    }

    private void requestBleScanPermissions() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            ActivityCompat.requestPermissions(this,
                    new String[]{
                            Manifest.permission.BLUETOOTH_SCAN,
                            Manifest.permission.BLUETOOTH_CONNECT,
                            Manifest.permission.BLUETOOTH_ADVERTISE
                    },
                    REQ_BLE);
        } else {
            ActivityCompat.requestPermissions(this,
                    new String[]{Manifest.permission.ACCESS_FINE_LOCATION},
                    REQ_BLE);
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode != REQ_BLE) {
            return;
        }
        boolean ok = true;
        for (int gr : grantResults) {
            if (gr != PackageManager.PERMISSION_GRANTED) {
                ok = false;
                break;
            }
        }
        if (ok) {
            beginScanAfterPermissions();
        } else {
            Toast.makeText(this, R.string.ble_permission_required, Toast.LENGTH_LONG).show();
            showState(STATE_NOT_CONNECTED);
        }
    }

    private void beginScanAfterPermissions() {
        stopBleDiscoveryOnly();
        bleDeviceAdapter.setItems(null);
        tvBleEmpty.setVisibility(View.VISIBLE);
        showState(STATE_SEARCHING);

        bleScanning = true;
        handler.removeCallbacks(stopBleScanRunnable);
        handler.postDelayed(stopBleScanRunnable, SCAN_PERIOD_MS);

        getUhf().startScanBTDevices(new ScanBTCallback() {
            @Override
            public void getDevices(final BluetoothDevice bluetoothDevice, final int rssi, byte[] bytes) {
                if (bluetoothDevice == null || bluetoothDevice.getAddress() == null) {
                    return;
                }
                runOnUiThread(() -> {
                    if (!bleScanning || currentState != STATE_SEARCHING) {
                        return;
                    }
                    @SuppressLint("MissingPermission")
                    String name = bluetoothDevice.getName();
                    String nm = name != null ? name.trim() : "";
                    BleDeviceRow row = new BleDeviceRow(bluetoothDevice.getAddress(), nm, rssi);
                    bleDeviceAdapter.upsert(row);
                    tvBleEmpty.setVisibility(View.GONE);
                });
            }
        });
    }

    private void stopBleDiscoveryOnly() {
        handler.removeCallbacks(stopBleScanRunnable);
        bleScanning = false;
        try {
            getUhf().stopScanBTDevices();
        } catch (Throwable ignored) {
        }
    }

    @SuppressLint("MissingPermission")
    private void onBleDevicePicked(BleDeviceRow row) {
        stopBleDiscoveryOnly();
        userCanceledConnect = false;

        selectedHandheldId = (row.name != null && !row.name.isEmpty()) ? row.name : row.address;
        if (tvConnectingSubtitle != null) {
            tvConnectingSubtitle.setText(getString(R.string.connect_scanner_to_reader));
        }
        showState(STATE_CONNECTING);

        DriverApplication.getInstance().beginBleSessionSwitch();
        Global.setLastBleMac(this, row.address.trim());

        try {
            getUhf().disconnect();
        } catch (Throwable ignored) {
        }

        getUhf().connect(row.address.trim(), DriverApplication.getInstance().getBleConnectionCallback());
    }

    @Override
    @SuppressLint("MissingPermission")
    public void onBleConnectionEvent(ConnectionStatus connectionStatus, BluetoothDevice device) {
        if (connectionStatus == ConnectionStatus.CONNECTED) {
            if (device != null && device.getAddress() != null) {
                String nm = device.getName();
                if (nm != null && !nm.trim().isEmpty()) {
                    selectedHandheldId = nm.trim();
                }
            }
            if (currentState == STATE_CONNECTING) {
                notifyBackendReaderLinked();
            } else if (!sessionStore.getReaderId().isEmpty()) {
                selectedHandheldId = sessionStore.getScannerHandheldLabel();
                populateConnectedLabelsFromBle();
                showState(STATE_CONNECTED);
            } else if (!sessionStore.getAuthToken().isEmpty()) {
                notifyBackendReaderLinked();
            }
            return;
        }
        if (connectionStatus == ConnectionStatus.DISCONNECTED) {
            if (userCanceledConnect) {
                userCanceledConnect = false;
                return;
            }
            if (currentState == STATE_CONNECTING) {
                Toast.makeText(this, R.string.scanner_not_connected_msg, Toast.LENGTH_SHORT).show();
                showState(STATE_NOT_CONNECTED);
            }
        }
    }

    private void abortReaderLinkFailed() {
        DriverApplication.getInstance().cancelBleReconnect();
        DriverApplication.getInstance().setUserRequestedDisconnect(true);
        DriverApplication.getInstance().endBleSessionSwitch();
        Global.setLastBleMac(this, "");
        try {
            getUhf().disconnect();
        } catch (Throwable ignored) {
        }
    }

    private void notifyBackendReaderLinked() {
        if (selectedHandheldId.isEmpty()) {
            Toast.makeText(this, R.string.scanner_not_connected_msg, Toast.LENGTH_SHORT).show();
            abortReaderLinkFailed();
            showState(STATE_NOT_CONNECTED);
            return;
        }
        String driverId = sessionStore.getDriverId();
        String customerId = sessionStore.getCustomerId();
        String sessionToken = sessionStore.getAuthToken();
        ioExecutor.execute(() -> {
            ApiClient.ApiResult result = apiClient.notifyReaderConnected(
                    selectedHandheldId,
                    driverId,
                    customerId,
                    sessionToken
            );
            runOnUiThread(() -> {
                if (!ApiClient.isHttpOk(result)) {
                    Toast.makeText(this, "Reader status send failed: " + result.error, Toast.LENGTH_LONG).show();
                    abortReaderLinkFailed();
                    showState(STATE_NOT_CONNECTED);
                    return;
                }
                JSONObject body = result.body;
                String readerId = "";
                if (body != null && body.has("readerId") && !body.isNull("readerId")) {
                    Object rv = body.opt("readerId");
                    readerId = rv != null ? String.valueOf(rv) : "";
                }
                if (readerId.isEmpty()) {
                    Toast.makeText(this, "Reader connected, but readerId missing.", Toast.LENGTH_LONG).show();
                    abortReaderLinkFailed();
                    showState(STATE_NOT_CONNECTED);
                    return;
                }
                sessionStore.setScannerHandheldLabel(selectedHandheldId);
                sessionStore.setReaderId(readerId);
                populateConnectedLabelsFromBle();
                showState(STATE_CONNECTED);
                Toast.makeText(this, "Reader linked.", Toast.LENGTH_SHORT).show();
            });
        });
    }

    private void populateConnectedLabelsFromBle() {
        String label = sessionStore.getScannerHandheldLabel();
        if (label.isEmpty()) {
            label = selectedHandheldId.isEmpty() ? "—" : selectedHandheldId;
        }
        if (tvConnectedScannerName != null) {
            tvConnectedScannerName.setText(label);
        }
        if (tvConnectedMac != null) {
            String mac = Global.getLastBleMac(this);
            tvConnectedMac.setText(mac.isEmpty() ? "—" : mac);
        }
        try {
            int bat = getUhf().getBattery();
            if (tvConnectedBattery != null) {
                tvConnectedBattery.setText(bat >= 0 ? bat + "%" : "—");
            }
        } catch (Throwable ignored) {
            if (tvConnectedBattery != null) {
                tvConnectedBattery.setText("—");
            }
        }
    }

    private void showState(int state) {
        currentState = state;
        stateNotConnected.setVisibility(state == STATE_NOT_CONNECTED ? View.VISIBLE : View.GONE);
        stateSearching.setVisibility(state == STATE_SEARCHING ? View.VISIBLE : View.GONE);
        stateConnecting.setVisibility(state == STATE_CONNECTING ? View.VISIBLE : View.GONE);
        stateConnected.setVisibility(state == STATE_CONNECTED ? View.VISIBLE : View.GONE);
        btnContinueHome.setEnabled(state == STATE_CONNECTED);
    }

    @Override
    protected void onDestroy() {
        handler.removeCallbacksAndMessages(null);
        ioExecutor.shutdownNow();
        super.onDestroy();
    }

    @Override
    public void onBackPressed() {
        if (currentState == STATE_SEARCHING) {
            stopBleDiscoveryOnly();
            showState(STATE_NOT_CONNECTED);
        } else if (currentState != STATE_NOT_CONNECTED && currentState != STATE_CONNECTED) {
            userCanceledConnect = true;
            DriverApplication.getInstance().setUserRequestedDisconnect(true);
            try {
                getUhf().disconnect();
            } catch (Throwable ignored) {
            }
            showState(STATE_NOT_CONNECTED);
        } else {
            super.onBackPressed();
        }
    }
}
