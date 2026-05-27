package com.laundry.r3reader.api;

import androidx.annotation.NonNull;

import com.laundry.r3reader.Global;

import java.io.IOException;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;

public class AuthInterceptor implements Interceptor {

    @NonNull
    @Override
    public Response intercept(@NonNull Chain chain) throws IOException {
        Request request = chain.request();
        String token = Global.getToken();
        if (!token.isEmpty()) {
            request = request.newBuilder()
                    .header("Authorization", "Bearer " + token)
                    .build();
        }
        return chain.proceed(request);
    }
}
