package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class TableLoginRequest {

    @SerializedName("deviceId")
    public String deviceId;

    public TableLoginRequest(String deviceId) {
        this.deviceId = deviceId;
    }
}
