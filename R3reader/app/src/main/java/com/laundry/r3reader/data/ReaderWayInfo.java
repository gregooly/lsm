package com.laundry.r3reader.data;

import androidx.annotation.NonNull;

public class ReaderWayInfo {

    public final int id;
    @NonNull
    public final String wayName;
    @NonNull
    public final String movementLabel;
    @NonNull
    public final String targetProcessStatus;

    public ReaderWayInfo(int id, @NonNull String wayName, @NonNull String movementLabel,
                         @NonNull String targetProcessStatus) {
        this.id = id;
        this.wayName = wayName;
        this.movementLabel = movementLabel;
        this.targetProcessStatus = targetProcessStatus;
    }
}
