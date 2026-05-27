package com.laundry.r3reader.data;

import androidx.annotation.NonNull;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/** In-memory holder for the active / last RFID scan session. */
public final class ScanSessionStore {

    private static String sessionId = "";
    private static int readerWayId;
    private static int customerId;
    private static int readerId;
    private static String readerWayTitle = "";
    private static String readerWayMovement = "";
    private static String readerWayTarget = "";
    private static final List<ScannedLinenItem> items = new ArrayList<>();

    private ScanSessionStore() {
    }

    public static void setLastSession(@NonNull String id,
                                      int wayId,
                                      int custId,
                                      int rId,
                                      @NonNull String wayTitle,
                                      @NonNull String movement,
                                      @NonNull String targetStatus,
                                      @NonNull List<ScannedLinenItem> scanned) {
        sessionId = id;
        readerWayId = wayId;
        customerId = custId;
        readerId = rId;
        readerWayTitle = wayTitle;
        readerWayMovement = movement;
        readerWayTarget = targetStatus;
        items.clear();
        items.addAll(scanned);
    }

    @NonNull
    public static String getSessionId() {
        return sessionId;
    }

    public static int getReaderWayId() {
        return readerWayId;
    }

    public static int getCustomerId() {
        return customerId;
    }

    public static int getReaderId() {
        return readerId;
    }

    @NonNull
    public static String getReaderWayTitle() {
        return readerWayTitle;
    }

    @NonNull
    public static String getReaderWayMovement() {
        return readerWayMovement;
    }

    @NonNull
    public static String getReaderWayTarget() {
        return readerWayTarget;
    }

    @NonNull
    public static List<ScannedLinenItem> getItems() {
        return Collections.unmodifiableList(items);
    }

    public static boolean hasItems() {
        return !items.isEmpty();
    }

    public static int count(@NonNull LinenCategory category) {
        int n = 0;
        for (ScannedLinenItem item : items) {
            if (item.category == category) {
                n++;
            }
        }
        return n;
    }

    public static void clear() {
        sessionId = "";
        readerWayId = 0;
        customerId = 0;
        readerId = 0;
        readerWayTitle = "";
        readerWayMovement = "";
        readerWayTarget = "";
        items.clear();
    }
}
