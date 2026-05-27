package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

import java.util.List;

public class MovementBatchRequest {
    @SerializedName("customerId")
    public int customerId;
    @SerializedName("readerId")
    public int readerId;
    @SerializedName("readerWayId")
    public int readerWayId;
    @SerializedName("uploadedAt")
    public String uploadedAt;
    @SerializedName("events")
    public List<MovementEventDto> events;

    public MovementBatchRequest(int customerId, int readerId, int readerWayId,
                                String uploadedAt, List<MovementEventDto> events) {
        this.customerId = customerId;
        this.readerId = readerId;
        this.readerWayId = readerWayId;
        this.uploadedAt = uploadedAt;
        this.events = events;
    }
}
