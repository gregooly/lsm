package com.example.driverapp;

/**
 * Discovered BLE row for R2 device picker (uhf-ble-demo DeviceListActivity pattern).
 */
public final class BleDeviceRow implements Comparable<BleDeviceRow> {
    public final String address;
    public String name;
    public int rssi;

    public BleDeviceRow(String address, String name, int rssi) {
        this.address = address != null ? address : "";
        this.name = name != null ? name : "";
        this.rssi = rssi;
    }

    @Override
    public int compareTo(BleDeviceRow o) {
        int other = o != null ? o.rssi : Integer.MIN_VALUE;
        return Integer.compare(other, this.rssi);
    }
}
