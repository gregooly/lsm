package com.laundry.r3reader.data;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.laundry.r3reader.api.model.ReaderDto;
import com.laundry.r3reader.api.model.ReaderWayDto;
import com.laundry.r3reader.api.model.SyncPolicyDto;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public final class BootstrapStore {

    @Nullable
    private static ReaderDto reader;
    private static final List<ReaderWayInfo> readerWays = new ArrayList<>();
    @Nullable
    private static SyncPolicyDto syncPolicy;

    private BootstrapStore() {
    }

    public static void apply(@Nullable ReaderDto readerDto,
                             @Nullable List<ReaderWayDto> ways,
                             @Nullable SyncPolicyDto policy) {
        reader = readerDto;
        readerWays.clear();
        if (ways != null) {
            for (ReaderWayDto way : ways) {
                readerWays.add(toInfo(way));
            }
        }
        syncPolicy = policy;
    }

    public static void clear() {
        reader = null;
        readerWays.clear();
        syncPolicy = null;
    }

    @Nullable
    public static ReaderDto getReader() {
        return reader;
    }

    @NonNull
    public static List<ReaderWayInfo> getReaderWays() {
        return Collections.unmodifiableList(readerWays);
    }

    public static boolean hasReaderWays() {
        return !readerWays.isEmpty();
    }

    @Nullable
    public static ReaderWayInfo findWayById(int id) {
        for (ReaderWayInfo way : readerWays) {
            if (way.id == id) {
                return way;
            }
        }
        return null;
    }

    @Nullable
    public static SyncPolicyDto getSyncPolicy() {
        return syncPolicy;
    }

    public static int getMaxBatchSize() {
        return syncPolicy != null && syncPolicy.maxBatchSize > 0
                ? syncPolicy.maxBatchSize : 200;
    }

    @NonNull
    private static ReaderWayInfo toInfo(@NonNull ReaderWayDto dto) {
        String from = dto.fromLocation != null ? dto.fromLocation.name : "?";
        String to = dto.toLocation != null ? dto.toLocation.name : "?";
        String movement = from + " → " + to;
        String target = dto.targetProcessStatus != null ? dto.targetProcessStatus : "";
        String name = dto.wayName != null ? dto.wayName : movement;
        return new ReaderWayInfo(dto.id, name, movement, target);
    }
}
