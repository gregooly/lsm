package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

public class ReaderWayDto {
    @SerializedName("id")
    public int id;
    @SerializedName("readerId")
    public int readerId;
    @SerializedName("wayName")
    public String wayName;
    @SerializedName("movementDirection")
    public String movementDirection;
    @SerializedName("businessPurposeKey")
    public String businessPurposeKey;
    @SerializedName("targetProcessStatus")
    public String targetProcessStatus;
    @SerializedName("fromLocation")
    public LocationDto fromLocation;
    @SerializedName("toLocation")
    public LocationDto toLocation;
}
