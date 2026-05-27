package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class MovementEventDto {
    @SerializedName("idempotencyKey")
    public String idempotencyKey;
    @SerializedName("rfidTag")
    public String rfidTag;
    @SerializedName("occurredAt")
    public String occurredAt;
    @SerializedName("conditionAfterEvent")
    public String conditionAfterEvent;

    public MovementEventDto(String idempotencyKey, String rfidTag, String occurredAt,
                            String conditionAfterEvent) {
        this.idempotencyKey = idempotencyKey;
        this.rfidTag = rfidTag;
        this.occurredAt = occurredAt;
        this.conditionAfterEvent = conditionAfterEvent;
    }
}
