package com.laundry.r3reader.api.model;

import com.google.gson.annotations.SerializedName;

import java.util.List;

public class LinenLookupResponse {
    @SerializedName("success")
    public boolean success;
    @SerializedName("found")
    public boolean found;
    @SerializedName("item")
    public LinenItemDto item;
    @SerializedName("warnings")
    public List<String> warnings;
}
