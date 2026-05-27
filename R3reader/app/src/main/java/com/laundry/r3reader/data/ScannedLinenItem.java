package com.laundry.r3reader.data;

import androidx.annotation.NonNull;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class ScannedLinenItem {

    public final long linenItemId;
    @NonNull
    public final String displayName;
    @NonNull
    public final String epc;
    @NonNull
    public final String linenId;
    public final boolean knownInTenant;

    @NonNull
    public final LinenCategory category;

    @NonNull
    public final String idempotencyKey;

    @NonNull
    public final String occurredAtIso;

    @NonNull
    public final List<String> warnings;

    public ScannedLinenItem(long linenItemId, @NonNull String displayName, @NonNull String epc,
                            @NonNull String linenId, boolean knownInTenant,
                            @NonNull LinenCategory category,
                            @NonNull String idempotencyKey,
                            @NonNull String occurredAtIso,
                            @NonNull List<String> warnings) {
        this.linenItemId = linenItemId;
        this.displayName = displayName;
        this.epc = epc;
        this.linenId = linenId;
        this.knownInTenant = knownInTenant;
        this.category = category;
        this.idempotencyKey = idempotencyKey;
        this.occurredAtIso = occurredAtIso;
        this.warnings = warnings;
    }

    @NonNull
    public List<String> warnings() {
        return Collections.unmodifiableList(warnings);
    }
}
