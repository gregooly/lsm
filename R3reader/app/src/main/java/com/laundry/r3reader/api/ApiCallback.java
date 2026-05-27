package com.laundry.r3reader.api;

import androidx.annotation.Nullable;

public interface ApiCallback<T> {
    void onSuccess(T data);

    void onError(@Nullable String message);
}
