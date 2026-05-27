package com.laundry.r3reader.data;

import android.content.Context;

import androidx.annotation.NonNull;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.laundry.r3reader.api.model.MovementBatchRequest;

import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public final class MovementQueueStore {

    private static final String PREFS = "movement_queue_prefs";
    private static final String KEY_PENDING = "pending_batches";
    private static final String KEY_FAILED = "failed_batches";
    private static final Gson GSON = new Gson();
    private static final Type LIST_TYPE = new TypeToken<List<MovementBatchRequest>>() {
    }.getType();

    private MovementQueueStore() {
    }

    public static void enqueuePending(@NonNull Context context, @NonNull MovementBatchRequest batch) {
        List<MovementBatchRequest> list = new ArrayList<>(getPending(context));
        list.add(batch);
        saveList(context, KEY_PENDING, list);
    }

    public static void enqueueFailed(@NonNull Context context, @NonNull MovementBatchRequest batch) {
        List<MovementBatchRequest> list = new ArrayList<>(getFailed(context));
        list.add(batch);
        saveList(context, KEY_FAILED, list);
    }

    public static void removePendingAt(@NonNull Context context, int index) {
        List<MovementBatchRequest> list = new ArrayList<>(getPending(context));
        if (index >= 0 && index < list.size()) {
            list.remove(index);
            saveList(context, KEY_PENDING, list);
        }
    }

    public static void removeFailedAt(@NonNull Context context, int index) {
        List<MovementBatchRequest> list = new ArrayList<>(getFailed(context));
        if (index >= 0 && index < list.size()) {
            list.remove(index);
            saveList(context, KEY_FAILED, list);
        }
    }

    @NonNull
    public static List<MovementBatchRequest> getPending(@NonNull Context context) {
        return loadList(context, KEY_PENDING);
    }

    @NonNull
    public static List<MovementBatchRequest> getFailed(@NonNull Context context) {
        return loadList(context, KEY_FAILED);
    }

    public static void clearAll(@NonNull Context context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().clear().apply();
    }

    @NonNull
    private static List<MovementBatchRequest> loadList(@NonNull Context context, String key) {
        String json = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
                .getString(key, "[]");
        if (json == null || json.isEmpty()) {
            return new ArrayList<>();
        }
        List<MovementBatchRequest> list = GSON.fromJson(json, LIST_TYPE);
        return list != null ? list : new ArrayList<>();
    }

    private static void saveList(@NonNull Context context, String key,
                                 @NonNull List<MovementBatchRequest> list) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
                .edit()
                .putString(key, GSON.toJson(list))
                .apply();
    }
}
