package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class SyncPolicyDto {
    @SerializedName("maxBatchSize")
    public int maxBatchSize;
    @SerializedName("recommendedSyncIntervalSeconds")
    public int recommendedSyncIntervalSeconds;
    @SerializedName("maxRetrySeconds")
    public int maxRetrySeconds;
}
