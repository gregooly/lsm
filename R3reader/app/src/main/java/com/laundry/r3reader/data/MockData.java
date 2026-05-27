package com.laundry.r3reader.data;

import androidx.annotation.NonNull;

import com.laundry.r3reader.api.ApiMapper;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

public final class MockData {

    public static final int SCAN_UNIQUE_TAGS = 128;
    public static final int SCAN_TOTAL_READS = 468;
    public static final int SCAN_DUPLICATES = 340;

    private static final String[] SAMPLE_EPCS = {
            "E200001722110144",
            "E200001722110145",
            "E200001722110146",
            "E200001722110147",
            "E200001722110148"
    };

    public static String[] sampleEpcs() {
        return SAMPLE_EPCS;
    }

    private static final String[] SAMPLE_LINEN_NAMES = {
            "Towel - White (Large)",
            "Sheet - King White",
            "Pillowcase - Standard",
            "Bath Mat - Grey",
            "Napkin - White"
    };

    private static final String[] MOCK_PHYSICAL = {
            "good", "good", "damaged", "good", "good"
    };

    /** Mock lookup + auto-classify for each EPC from a real scan session. */
    @NonNull
    public static List<ScannedLinenItem> buildScannedItemsFromEpcs(@NonNull Set<String> epcs) {
        List<ScannedLinenItem> list = new ArrayList<>();
        int i = 0;
        for (String epc : epcs) {
            list.add(lookupAndClassify(epc, i));
            i++;
        }
        return list;
    }

    /** Demo: synthesize a set of EPCs when the simulated scan ends. */
    @NonNull
    public static Set<String> synthesizeEpcsForDemo(int uniqueCount) {
        LinkedHashSet<String> set = new LinkedHashSet<>();
        String[] base = sampleEpcs();
        for (int i = 0; i < uniqueCount; i++) {
            if (i < base.length) {
                set.add(base[i]);
            } else {
                set.add("E20000172211" + String.format("%05d", 144 + i));
            }
        }
        return set;
    }

    @NonNull
    public static ScannedLinenItem lookupAndClassify(@NonNull String epc, int index) {
        String[] base = sampleEpcs();
        boolean known = false;
        String physical = "good";
        for (int j = 0; j < base.length; j++) {
            if (base[j].equals(epc)) {
                known = true;
                physical = MOCK_PHYSICAL[j];
                break;
            }
        }
        if (!known && epc.startsWith("E20000172211")) {
            known = true;
            physical = (index % 11 == 0) ? "damaged" : "good";
        }
        String name = known
                ? SAMPLE_LINEN_NAMES[index % SAMPLE_LINEN_NAMES.length]
                : "Unknown tag";
        String linenId = known ? String.format("LIN-%07d", 4567 + index) : "—";
        long itemId = known ? 1000L + index : 0L;
        LinenCategory category = LinenClassifier.classify(epc, known, physical);
        return new ScannedLinenItem(
                itemId, name, epc, linenId, known, category,
                ApiMapper.newIdempotencyKey(),
                ApiMapper.nowUtcIso(),
                new ArrayList<>());
    }

    public static final class BleDevice {
        public final String name;
        public final String mac;
        /** Chainway id → readers.device_identifier (connection-status handheldId). */
        public final String handheldId;
        public final boolean connected;

        public BleDevice(String name, String mac, String handheldId, boolean connected) {
            this.name = name;
            this.mac = mac;
            this.handheldId = handheldId;
            this.connected = connected;
        }
    }

    public static final class SyncItem {
        public final String sessionId;
        public final int itemCount;
        public final String time;
        public final boolean failed;

        public SyncItem(String sessionId, int itemCount, String time, boolean failed) {
            this.sessionId = sessionId;
            this.itemCount = itemCount;
            this.time = time;
            this.failed = failed;
        }
    }

    public static List<BleDevice> bleDevices() {
        List<BleDevice> list = new ArrayList<>();
        list.add(new BleDevice("TABLE-RFID-01", "D4:31:3C:2A:11:01", "TABLE-RFID-01", true));
        list.add(new BleDevice("TABLE-RFID-02", "D4:31:3C:2A:11:02", "TABLE-RFID-02", false));
        list.add(new BleDevice("TABLE-RFID-03", "D4:31:3C:2A:11:03", "TABLE-RFID-03", false));
        return list;
    }

    public static List<SyncItem> pendingUploads() {
        return Arrays.asList(
                new SyncItem("SS-000123", 128, "10:18 AM", false),
                new SyncItem("SS-000124", 90, "10:20 AM", false)
        );
    }

    public static List<SyncItem> failedUploads() {
        return Arrays.asList(
                new SyncItem("SS-000122", 56, "May 6, 04:35 PM", true)
        );
    }

    public static String[][] itemInfoRows(@NonNull LinenCategory category) {
        return new String[][]{
                {"Item Type", "Bath Towel"},
                {"Size", "Large"},
                {"Owner Customer", "Hotel Sakura"},
                {"Current Location", "Sort bench"},
                {"Classification", category.name()},
                {"Wash Count", "28"},
                {"Assignment", "Unassigned"}
        };
    }
}
