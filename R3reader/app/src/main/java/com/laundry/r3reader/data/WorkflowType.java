package com.laundry.r3reader.data;

import androidx.annotation.IdRes;
import androidx.annotation.NonNull;
import androidx.annotation.StringRes;

import com.laundry.r3reader.R;

/**
 * Reader way selected before scan. All ways use the same post-scan classification screen.
 */
public enum WorkflowType {

    SORTING(0,
            R.string.sorting_preparation,
            R.string.movement_sorting,
            "sorting_ready"),
    SHIPMENT(1,
            R.string.shipment_preparation,
            R.string.movement_shipment,
            "ready_for_dispatch"),
    QUARANTINE(2,
            R.string.quarantine_damage,
            R.string.movement_quarantine,
            "quarantined"),
    RETURN_POOL(3,
            R.string.return_to_pool,
            R.string.movement_return,
            "in_inventory");

    public final int index;
    @StringRes
    public final int titleRes;
    @StringRes
    public final int movementRes;
    @NonNull
    public final String targetProcessStatus;

    @IdRes
    public static final int POST_SCAN_ACTION = R.id.action_scan_to_results;

    WorkflowType(int index, @StringRes int titleRes, @StringRes int movementRes,
                 @NonNull String targetProcessStatus) {
        this.index = index;
        this.titleRes = titleRes;
        this.movementRes = movementRes;
        this.targetProcessStatus = targetProcessStatus;
    }

    @NonNull
    public static WorkflowType fromIndex(int index) {
        for (WorkflowType type : values()) {
            if (type.index == index) {
                return type;
            }
        }
        return SORTING;
    }
}
