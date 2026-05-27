package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class TableLoginResponse {
    @SerializedName("success")
    public boolean success;
    @SerializedName("message")
    public String message;
    @SerializedName("customerId")
    public int customerId;
    @SerializedName("token")
    public String token;
    @SerializedName("reader")
    public ReaderDto reader;
}
