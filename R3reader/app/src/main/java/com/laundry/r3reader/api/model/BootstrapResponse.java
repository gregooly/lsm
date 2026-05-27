package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

import java.util.List;

public class BootstrapResponse {
    @SerializedName("success")
    public boolean success;
    @SerializedName("reader")
    public ReaderDto reader;
    @SerializedName("readerWays")
    public List<ReaderWayDto> readerWays;
    @SerializedName("syncPolicy")
    public SyncPolicyDto syncPolicy;
}
