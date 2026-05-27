package com.laundry.r3reader.ble;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

/**
 * BLE reader discovered during scan (Chainway UHF over Bluetooth).
 */
public final class BleDiscoveredDevice {

    @NonNull
    public final String name;
    @NonNull
    public final String address;
    public final int rssi;

    public BleDiscoveredDevice(@Nullable String name, @NonNull String address, int rssi) {
        this.name = name != null && !name.isEmpty() ? name : address;
        this.address = address;
        this.rssi = rssi;
    }

    /** Maps to LaundryMS readers.device_identifier / connection-status handheldId. */
    @NonNull
    public String getHandheldId() {
        return name;
    }
}
