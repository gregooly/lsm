package com.laundry.r3reader.api;

import android.util.Log;

import androidx.annotation.NonNull;

import com.laundry.r3reader.Global;

import okhttp3.OkHttpClient;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public final class ApiClient {

    private static final String TAG = "ApiClient";

    private static volatile TableApiService service;

    private ApiClient() {
    }

    @NonNull
    public static TableApiService getService() {
        if (service == null) {
            synchronized (ApiClient.class) {
                if (service == null) {
                    service = buildRetrofit().create(TableApiService.class);
                }
            }
        }
        return service;
    }

    public static void reset() {
        service = null;
    }

    @NonNull
    private static Retrofit buildRetrofit() {
        String baseUrl = Global.getBaseUrl();
        Log.i(TAG, "Retrofit base URL: " + baseUrl);

        OkHttpClient client = new OkHttpClient.Builder()
                .addInterceptor(new AuthInterceptor())
                .addInterceptor(new ApiRequestLoggingInterceptor())
                .build();

        return new Retrofit.Builder()
                .baseUrl(baseUrl)
                .client(client)
                .addConverterFactory(GsonConverterFactory.create())
                .build();
    }
}
