package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class LinenItemDto {
    @SerializedName("id")
    public long id;
    @SerializedName("rfidTag")
    public String rfidTag;
    @SerializedName("itemType")
    public String itemType;
    @SerializedName("sizeLabel")
    public String sizeLabel;
    @SerializedName("ownerCustomerName")
    public String ownerCustomerName;
    @SerializedName("physicalCondition")
    public String physicalCondition;
    @SerializedName("isActive")
    public boolean isActive;
}
