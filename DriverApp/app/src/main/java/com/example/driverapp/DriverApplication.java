package com.example.driverapp;

import android.app.Application;
import android.bluetooth.BluetoothDevice;
import android.os.Handler;
import android.os.Looper;

import androidx.annotation.Nullable;

import com.rscja.deviceapi.RFIDWithUHFBLE;
import com.rscja.deviceapi.interfaces.ConnectionStatus;
import com.rscja.deviceapi.interfaces.ConnectionStatusCallback;

/**
 * Process-wide UHF BLE handle (uhf-ble-demo pattern) plus a single {@link ConnectionStatusCallback}
 * so disconnect / auto-reconnect behaves like {@code MainActivity.BTStatus} in the sample.
 */
public class DriverApplication extends Application {

    public interface BleConnectionUiObserver {
        void onBleConnectionEvent(ConnectionStatus status, @Nullable BluetoothDevice device);
    }

    private static DriverApplication instance;
    private RFIDWithUHFBLE uhf;

    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private Runnable pendingReconnect;

    private volatile boolean userRequestedDisconnect;
    private volatile boolean bleSessionSwitchInProgress;

    private volatile BleConnectionUiObserver bleUiObserver;

    public static DriverApplication getInstance() {
        return instance;
    }

    public RFIDWithUHFBLE getUhf() {
        return uhf;
    }

    public void setBleConnectionUiObserver(@Nullable BleConnectionUiObserver observer) {
        bleUiObserver = observer;
    }

    /** Call before disconnect when the user explicitly disconnects or logs out (sample: active disconnect). */
    public void setUserRequestedDisconnect(boolean v) {
        userRequestedDisconnect = v;
        if (v) {
            cancelPendingReconnect();
        }
    }

    /**
     * Call before programmatic disconnect→connect when switching devices so auto-reconnect does not fire.
     */
    public void beginBleSessionSwitch() {
        bleSessionSwitchInProgress = true;
        cancelPendingReconnect();
    }

    public void endBleSessionSwitch() {
        bleSessionSwitchInProgress = false;
    }

    private void cancelPendingReconnect() {
        if (pendingReconnect != null) {
            mainHandler.removeCallbacks(pendingReconnect);
            pendingReconnect = null;
        }
    }

    /** Stop a scheduled auto-reconnect attempt (e.g. reader pairing failed or user clears cache). */
    public void cancelBleReconnect() {
        cancelPendingReconnect();
    }

    public ConnectionStatusCallback<Object> getBleConnectionCallback() {
        return bleConnectionCallback;
    }

    private final ConnectionStatusCallback<Object> bleConnectionCallback = new ConnectionStatusCallback<Object>() {
        @Override
        public void getStatus(ConnectionStatus connectionStatus, Object device1) {
            mainHandler.post(() -> dispatchBleConnection(connectionStatus, device1));
        }
    };

    private void dispatchBleConnection(ConnectionStatus status, Object deviceObj) {
        BluetoothDevice device = deviceObj instanceof BluetoothDevice ? (BluetoothDevice) deviceObj : null;

        if (status == ConnectionStatus.CONNECTED) {
            bleSessionSwitchInProgress = false;
            cancelPendingReconnect();
            if (device != null && device.getAddress() != null) {
                Global.setLastBleMac(this, device.getAddress());
            }
        }

        BleConnectionUiObserver obs = bleUiObserver;
        if (obs != null) {
            obs.onBleConnectionEvent(status, device);
        }

        if (status == ConnectionStatus.DISCONNECTED) {
            if (userRequestedDisconnect) {
                userRequestedDisconnect = false;
                return;
            }
            if (bleSessionSwitchInProgress) {
                return;
            }
            String mac = Global.getLastBleMac(this);
            if (!Global.isAutoReconnect(this) || mac.isEmpty()) {
                return;
            }
            cancelPendingReconnect();
            pendingReconnect = () -> {
                pendingReconnect = null;
                if (uhf.getConnectStatus() != ConnectionStatus.DISCONNECTED) {
                    return;
                }
                if (!Global.isAutoReconnect(DriverApplication.this)) {
                    return;
                }
                String m = Global.getLastBleMac(DriverApplication.this);
                if (m.isEmpty()) {
                    return;
                }
                try {
                    uhf.connect(m, bleConnectionCallback);
                } catch (Throwable ignored) {
                }
            };
            mainHandler.postDelayed(pendingReconnect, 900L);
        }
    }

    @Override
    public void onCreate() {
        super.onCreate();
        instance = this;
        uhf = RFIDWithUHFBLE.getInstance();
        uhf.init(getApplicationContext());
    }
}
