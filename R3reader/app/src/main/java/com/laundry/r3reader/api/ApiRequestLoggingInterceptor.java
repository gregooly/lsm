package com.laundry.r3reader.api;

import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import java.io.IOException;
import java.util.Set;
import java.util.concurrent.TimeUnit;

import okhttp3.Interceptor;
import okhttp3.MediaType;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;
import okio.Buffer;

/**
 * Logs outgoing API request parameters (URL, query, body) and response status.
 */
public final class ApiRequestLoggingInterceptor implements Interceptor {

    public static final String TAG = "R3ApiRequest";

    private static final int MAX_RESPONSE_LOG_BYTES = 2048;

    @NonNull
    @Override
    public Response intercept(@NonNull Chain chain) throws IOException {
        Request request = chain.request();
        String bodySnapshot = snapshotRequestBody(request);
        if (bodySnapshot != null) {
            RequestBody original = request.body();
            MediaType contentType = original != null ? original.contentType() : null;
            request = request.newBuilder()
                    .method(request.method(), RequestBody.create(bodySnapshot, contentType))
                    .build();
        }

        logOutgoing(request, bodySnapshot);

        long startNs = System.nanoTime();
        Response response = chain.proceed(request);
        logIncoming(response, TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - startNs));
        return response;
    }

    /**
     * Reads the body for logging; returns null when there is no body.
     */
    @Nullable
    private static String snapshotRequestBody(@NonNull Request request) throws IOException {
        RequestBody body = request.body();
        if (body == null) {
            return null;
        }
        Buffer buffer = new Buffer();
        body.writeTo(buffer);
        return buffer.readUtf8();
    }

    private static void logOutgoing(@NonNull Request request, @Nullable String bodySnapshot) {
        StringBuilder out = new StringBuilder();
        out.append("--> ").append(request.method()).append(' ').append(request.url());

        Set<String> queryNames = request.url().queryParameterNames();
        if (!queryNames.isEmpty()) {
            out.append("\nQuery parameters:");
            for (String name : queryNames) {
                for (String value : request.url().queryParameterValues(name)) {
                    out.append("\n  ").append(name).append('=').append(value);
                }
            }
        }

        if (bodySnapshot != null && !bodySnapshot.isEmpty()) {
            out.append("\nRequest body: ").append(bodySnapshot);
        } else {
            out.append("\nRequest body: (none)");
        }

        for (String name : request.headers().names()) {
            if ("Authorization".equalsIgnoreCase(name)) {
                out.append("\nHeader Authorization: Bearer ***");
            } else {
                out.append("\nHeader ").append(name).append(": ")
                        .append(request.header(name));
            }
        }

        Log.i(TAG, out.toString());
    }

    private static void logIncoming(@NonNull Response response, long elapsedMs) throws IOException {
        StringBuilder out = new StringBuilder();
        out.append("<-- ").append(response.code()).append(' ')
                .append(response.message()).append(' ')
                .append(response.request().url()).append(" (").append(elapsedMs).append("ms)");

        if (response.body() != null) {
            String peek = response.peekBody(MAX_RESPONSE_LOG_BYTES).string();
            if (!peek.isEmpty()) {
                boolean truncated = peek.length() >= MAX_RESPONSE_LOG_BYTES;
                out.append(truncated ? "\nResponse body (truncated): " : "\nResponse body: ")
                        .append(peek);
                if (truncated) {
                    out.append('…');
                }
            }
        }

        Log.i(TAG, out.toString());
    }
}
