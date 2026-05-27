package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class SimpleSuccessResponse {
    @SerializedName("success")
    public boolean success;
    @SerializedName("message")
    public String message;
    @SerializedName("readerId")
    public Integer readerId;
}
