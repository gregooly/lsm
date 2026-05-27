package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

import java.util.List;

public class MovementBatchResponse {
    @SerializedName("success")
    public boolean success;
    @SerializedName("message")
    public String message;
    @SerializedName("results")
    public List<MovementResultDto> results;
}
