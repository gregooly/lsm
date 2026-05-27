package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class ReaderDto {
    @SerializedName("id")
    public int id;
    @SerializedName("readerName")
    public String readerName;
    @SerializedName("deviceIdentifier")
    public String deviceIdentifier;
    @SerializedName("deviceModel")
    public String deviceModel;
    @SerializedName("readerCategory")
    public String readerCategory;
}
