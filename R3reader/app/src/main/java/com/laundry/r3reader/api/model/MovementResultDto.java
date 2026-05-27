package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class MovementResultDto {
    @SerializedName("idempotencyKey")
    public String idempotencyKey;
    @SerializedName("status")
    public String status;
    @SerializedName("reason")
    public String reason;
}
