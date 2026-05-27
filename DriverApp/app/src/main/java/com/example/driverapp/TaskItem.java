package com.example.driverapp;

public class TaskItem {
    public final int jobId;
    public final String jobType;
    public final String jobStatus;
    public final int readerWayId;
    public final int readerId;
    public final String fromLocationName;
    public final String toLocationName;
    public final String priority;

    public TaskItem(int jobId, String jobType, String jobStatus, int readerWayId, int readerId,
                    String fromLocationName, String toLocationName, String priority) {
        this.jobId = jobId;
        this.jobType = jobType;
        this.jobStatus = jobStatus;
        this.readerWayId = readerWayId;
        this.readerId = readerId;
        this.fromLocationName = fromLocationName;
        this.toLocationName = toLocationName;
        this.priority = priority;
    }

    public String summaryTitle() {
        String type = jobType == null ? "" : jobType.trim();
        if (type.equalsIgnoreCase("collection")) {
            type = "Pickup";
        } else if (type.equalsIgnoreCase("delivery")) {
            type = "Delivery";
        } else if (type.isEmpty()) {
            type = "Job";
        } else {
            type = capitalize(type);
        }
        return String.format("%s · #%s", type, jobId);
    }

    public String routeSubtitle() {
        String from = (fromLocationName == null || fromLocationName.trim().isEmpty()) ? "-" : fromLocationName;
        String to = (toLocationName == null || toLocationName.trim().isEmpty()) ? "-" : toLocationName;
        return from + " → " + to;
    }

    private static String capitalize(String s) {
        if (s == null || s.isEmpty()) {
            return s;
        }
        return Character.toUpperCase(s.charAt(0)) + s.substring(1);
    }
}
