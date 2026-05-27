package com.laundry.r3reader.api;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.laundry.r3reader.api.model.LinenItemDto;
import com.laundry.r3reader.api.model.LinenLookupResponse;
import com.laundry.r3reader.api.model.MovementBatchRequest;
import com.laundry.r3reader.api.model.MovementEventDto;
import com.laundry.r3reader.data.LinenCategory;
import com.laundry.r3reader.data.LinenClassifier;
import com.laundry.r3reader.data.MockData;
import com.laundry.r3reader.data.ScannedLinenItem;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;
import java.util.UUID;

public final class ApiMapper {

    private static final SimpleDateFormat ISO_UTC;

    static {
        ISO_UTC = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'", Locale.US);
        ISO_UTC.setTimeZone(TimeZone.getTimeZone("UTC"));
    }

    private ApiMapper() {
    }

    @NonNull
    public static String nowUtcIso() {
        return ISO_UTC.format(new Date());
    }

    @NonNull
    public static String newIdempotencyKey() {
        return UUID.randomUUID().toString();
    }

    @NonNull
    public static ScannedLinenItem fromLookup(@NonNull String epc, @NonNull LinenLookupResponse response) {
        if (response.found && response.item != null) {
            LinenItemDto item = response.item;
            String physical = item.physicalCondition != null ? item.physicalCondition : "good";
            boolean active = item.isActive;
            boolean known = active;
            LinenCategory category = LinenClassifier.classify(epc, known, physical);
            String name = buildDisplayName(item);
            String linenId = item.id > 0 ? String.format(Locale.US, "LIN-%07d", item.id) : "—";
            List<String> warnings = response.warnings != null
                    ? new ArrayList<>(response.warnings) : new ArrayList<>();
            if (!active) {
                warnings.add("Inactive RFID tag for this tenant.");
            }
            return new ScannedLinenItem(
                    item.id,
                    name,
                    epc,
                    linenId,
                    known && active,
                    category,
                    newIdempotencyKey(),
                    nowUtcIso(),
                    warnings);
        }
        List<String> warnings = response.warnings != null
                ? new ArrayList<>(response.warnings)
                : new ArrayList<>();
        if (warnings.isEmpty()) {
            warnings.add("Unknown or inactive RFID tag for this tenant.");
        }
        LinenCategory category = LinenCategory.LOST;
        return new ScannedLinenItem(
                0L,
                "Unknown tag",
                epc,
                "—",
                false,
                category,
                newIdempotencyKey(),
                nowUtcIso(),
                warnings);
    }

    @NonNull
    public static ScannedLinenItem mockFallback(@NonNull String epc, int index) {
        return MockData.lookupAndClassify(epc, index);
    }

    @NonNull
    private static String buildDisplayName(@NonNull LinenItemDto item) {
        String type = item.itemType != null ? capitalize(item.itemType) : "Item";
        String size = item.sizeLabel != null && !item.sizeLabel.isEmpty()
                ? " (" + item.sizeLabel + ")" : "";
        return type + size;
    }

    @NonNull
    private static String capitalize(@NonNull String raw) {
        if (raw.isEmpty()) return raw;
        return Character.toUpperCase(raw.charAt(0)) + raw.substring(1);
    }

    @NonNull
    public static MovementBatchRequest toMovementBatch(int customerId, int readerId, int readerWayId,
                                                         @NonNull List<ScannedLinenItem> items) {
        List<MovementEventDto> events = new ArrayList<>();
        for (ScannedLinenItem item : items) {
            String condition = item.category.apiValue;
            events.add(new MovementEventDto(
                    item.idempotencyKey,
                    item.epc,
                    item.occurredAtIso,
                    condition));
        }
        return new MovementBatchRequest(
                customerId,
                readerId,
                readerWayId,
                nowUtcIso(),
                events);
    }
}
