package com.laundry.r3reader.data;

import androidx.annotation.NonNull;
import androidx.annotation.StringRes;

import com.laundry.r3reader.R;

/** Automatic scan classification: normal, damaged, or lost. */
public enum LinenCategory {
    NORMAL(R.string.category_normal, "good"),
    DAMAGED(R.string.category_damaged, "damaged"),
    LOST(R.string.category_lost, "lost");

    @StringRes
    public final int labelRes;

    /** Value for API {@code conditionAfterEvent}. */
    @NonNull
    public final String apiValue;

    LinenCategory(@StringRes int labelRes, @NonNull String apiValue) {
        this.labelRes = labelRes;
        this.apiValue = apiValue;
    }
}
