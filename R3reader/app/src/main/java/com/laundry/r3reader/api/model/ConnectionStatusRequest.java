package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

/** Table connect: no driverId, no readerId in request. */
public class ConnectionStatusRequest {
    @SerializedName("handheldId")
    public String handheldId;
    @SerializedName("customerId")
    public int customerId;
    @SerializedName("connectedAt")
    public String connectedAt;

    public ConnectionStatusRequest(String handheldId, int customerId, String connectedAt) {
        this.handheldId = handheldId;
        this.customerId = customerId;
        this.connectedAt = connectedAt;
    }
}
