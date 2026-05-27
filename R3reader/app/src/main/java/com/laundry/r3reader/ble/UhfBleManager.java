package com.laundry.r3reader.ble;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import android.os.Handler;
import android.os.Looper;
import android.text.TextUtils;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.laundry.r3reader.R3Application;
import com.laundry.r3reader.data.PrefsManager;
import com.rscja.deviceapi.RFIDWithUHFBLE;
import com.rscja.deviceapi.entity.UHFTAGInfo;
import com.rscja.deviceapi.interfaces.ConnectionStatus;
import com.rscja.deviceapi.interfaces.ConnectionStatusCallback;
import com.rscja.deviceapi.interfaces.IUHFInventoryCallback;
import com.rscja.deviceapi.interfaces.ScanBTCallback;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Chainway UHF BLE reader (from uhf-ble-demo / DeviceAPI SDK).
 * Single instance shared by Connect and Scan screens.
 */
public final class UhfBleManager {

    private static final String TAG = "UhfBleManager";
    private static final long SCAN_PERIOD_MS = 10_000;

    private static volatile UhfBleManager instance;

    private final RFIDWithUHFBLE uhf = RFIDWithUHFBLE.getInstance();
    private final Handler main = new Handler(Looper.getMainLooper());

    private boolean initialized;
    private boolean scanning;
    private boolean inventoryRunning;

    @Nullable
    private String connectedName = "";
    @Nullable
    private String connectedAddress = "";

    private final Map<String, Integer> rssiByAddress = new LinkedHashMap<>();
    private final List<BleDiscoveredDevice> discovered = new ArrayList<>();

    @Nullable
    private ScanListener scanListener;
    @Nullable
    private ConnectionListener connectionListener;
    @Nullable
    private TagReadListener tagReadListener;

    public interface ScanListener {
        void onDeviceFound(@NonNull BleDiscoveredDevice device);

        void onScanFinished();
    }

    public interface ConnectionListener {
        void onStatusChanged(@NonNull ConnectionStatus status,
                             @Nullable String name,
                             @Nullable String address);
    }

    public interface TagReadListener {
        void onTagRead(@NonNull String epc);
    }

    private UhfBleManager() {
    }

    @NonNull
    public static UhfBleManager getInstance() {
        if (instance == null) {
            synchronized (UhfBleManager.class) {
                if (instance == null) {
                    instance = new UhfBleManager();
                }
            }
        }
        return instance;
    }

    public void init(@NonNull Context context) {
        if (initialized) {
            return;
        }
        try {
            uhf.init(context.getApplicationContext());
            uhf.setConnectionStatusCallback(connectionCallback);
            initialized = true;
            Log.i(TAG, "UHF BLE SDK initialized");
        } catch (Exception e) {
            Log.e(TAG, "UHF init failed", e);
        }
    }

    public void free() {
        stopInventory();
        stopScan();
        disconnect();
        try {
            uhf.free();
        } catch (Exception ignored) {
        }
        initialized = false;
    }

    public boolean isBleConnected() {
        return uhf.getConnectStatus() == ConnectionStatus.CONNECTED;
    }

    @NonNull
    public ConnectionStatus getConnectStatus() {
        return uhf.getConnectStatus();
    }

    @Nullable
    public String getConnectedHandheldId() {
        if (!TextUtils.isEmpty(connectedName)) {
            return connectedName;
        }
        return connectedAddress != null ? connectedAddress : "";
    }

    @NonNull
    public String getConnectedAddress() {
        return connectedAddress != null ? connectedAddress : "";
    }

    @NonNull
    public String getConnectedName() {
        return connectedName != null ? connectedName : "";
    }

    public void setScanListener(@Nullable ScanListener listener) {
        this.scanListener = listener;
    }

    public void setConnectionListener(@Nullable ConnectionListener listener) {
        this.connectionListener = listener;
    }

    public void setTagReadListener(@Nullable TagReadListener listener) {
        this.tagReadListener = listener;
    }

    @NonNull
    public List<BleDiscoveredDevice> getDiscoveredDevices() {
        return Collections.unmodifiableList(new ArrayList<>(discovered));
    }

    public void startScan(@Nullable ScanListener listener) {
        scanListener = listener;
        if (scanning) {
            return;
        }
        discovered.clear();
        rssiByAddress.clear();
        scanning = true;
        uhf.startScanBTDevices(scanCallback);
        main.postDelayed(scanTimeoutRunnable, SCAN_PERIOD_MS);
        Log.i(TAG, "BLE scan started");
    }

    public void stopScan() {
        if (!scanning) {
            return;
        }
        scanning = false;
        main.removeCallbacks(scanTimeoutRunnable);
        uhf.stopScanBTDevices();
        if (scanListener != null) {
            scanListener.onScanFinished();
        }
        Log.i(TAG, "BLE scan stopped");
    }

    public void connect(@NonNull String deviceAddress) {
        if (uhf.getConnectStatus() == ConnectionStatus.CONNECTING) {
            return;
        }
        Log.i(TAG, "Connecting to " + deviceAddress);
        uhf.connect(deviceAddress.trim(), connectionCallback);
    }

    public void disconnect() {
        stopInventory();
        uhf.disconnect();
        connectedName = "";
        connectedAddress = "";
    }

    /** Apply reader RF settings after connect (from sample app). */
    public void applyDefaultReaderConfig() {
        try {
            uhf.setSupportRssi(true);
            uhf.setFrequencyMode(0x08);
            uhf.setPower(30);
        } catch (Exception e) {
            Log.w(TAG, "Reader config: " + e.getMessage());
        }
    }

    public boolean startInventory() {
        if (!isBleConnected()) {
            return false;
        }
        if (inventoryRunning) {
            return true;
        }
        uhf.setInventoryCallback(inventoryCallback);
        boolean started = uhf.startInventoryTag();
        inventoryRunning = started;
        Log.i(TAG, "startInventoryTag: " + started);
        return started;
    }

    public boolean stopInventory() {
        if (!inventoryRunning) {
            return true;
        }
        boolean stopped = uhf.stopInventory();
        inventoryRunning = false;
        uhf.setInventoryCallback(null);
        Log.i(TAG, "stopInventory: " + stopped);
        return stopped;
    }

    private final ScanBTCallback scanCallback = new ScanBTCallback() {
        @Override
        public void getDevices(final BluetoothDevice device, final int rssi, byte[] bytes) {
            if (device == null || device.getName() == null || device.getName().isEmpty()) {
                return;
            }
            main.post(() -> addDiscoveredDevice(device, rssi));
        }
    };

    private void addDiscoveredDevice(@NonNull BluetoothDevice device, int rssi) {
        String address = device.getAddress();
        if (address == null) {
            return;
        }
        rssiByAddress.put(address, rssi);
        boolean found = false;
        for (BleDiscoveredDevice d : discovered) {
            if (d.address.equals(address)) {
                found = true;
                break;
            }
        }
        if (!found) {
            BleDiscoveredDevice entry = new BleDiscoveredDevice(device.getName(), address, rssi);
            discovered.add(entry);
            sortByRssi();
            if (scanListener != null) {
                scanListener.onDeviceFound(entry);
            }
        }
    }

    private void sortByRssi() {
        Collections.sort(discovered, new Comparator<BleDiscoveredDevice>() {
            @Override
            public int compare(BleDiscoveredDevice a, BleDiscoveredDevice b) {
                int ra = rssiByAddress.containsKey(a.address) ? rssiByAddress.get(a.address) : 0;
                int rb = rssiByAddress.containsKey(b.address) ? rssiByAddress.get(b.address) : 0;
                return Integer.compare(rb, ra);
            }
        });
    }

    private final Runnable scanTimeoutRunnable = new Runnable() {
        @Override
        public void run() {
            stopScan();
        }
    };

    private final ConnectionStatusCallback<Object> connectionCallback =
            new ConnectionStatusCallback<Object>() {
                @Override
                public void getStatus(final ConnectionStatus status, final Object device1) {
                    main.post(() -> {
                        String name = "";
                        String address = "";
                        if (device1 instanceof BluetoothDevice) {
                            BluetoothDevice device = (BluetoothDevice) device1;
                            try {
                                name = device.getName() != null ? device.getName() : "";
                            } catch (SecurityException ignored) {
                            }
                            address = device.getAddress() != null ? device.getAddress() : "";
                        }
                        if (status == ConnectionStatus.CONNECTED) {
                            connectedName = name;
                            connectedAddress = address;
                            applyDefaultReaderConfig();
                        } else if (status == ConnectionStatus.DISCONNECTED) {
                            connectedName = "";
                            connectedAddress = "";
                            stopInventory();
                            PrefsManager.clearReaderConnection(R3Application.getInstance());
                        }
                        if (connectionListener != null) {
                            connectionListener.onStatusChanged(status, name, address);
                        }
                        Log.i(TAG, "BLE status: " + status + " " + name + " " + address);
                    });
                }
            };

    private final IUHFInventoryCallback inventoryCallback = new IUHFInventoryCallback() {
        @Override
        public void callback(UHFTAGInfo info) {
            if (info == null || info.getEPC() == null || info.getEPC().isEmpty()) {
                return;
            }
            final String epc = info.getEPC();
            main.post(() -> {
                if (tagReadListener != null) {
                    tagReadListener.onTagRead(epc);
                }
            });
        }
    };
}
