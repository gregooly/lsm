package com.example.driverapp;

public class SyncRecordItem {
    public static final String STATUS_PENDING = "pending";
    public static final String STATUS_FAILED = "failed";
    public static final String STATUS_UPLOADED = "uploaded";

    public final String title;
    public final String subtitle;
    public final String timeText;
    public final String status;

    public SyncRecordItem(String title, String subtitle, String timeText, String status) {
        this.title = title;
        this.subtitle = subtitle;
        this.timeText = timeText;
        this.status = status;
    }
}
