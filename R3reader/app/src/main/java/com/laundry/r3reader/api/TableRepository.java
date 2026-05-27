package com.laundry.r3reader.api;

import android.os.Handler;
import android.os.Looper;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.laundry.r3reader.Global;
import com.laundry.r3reader.api.model.BootstrapResponse;
import com.laundry.r3reader.api.model.ConnectionStatusRequest;
import com.laundry.r3reader.api.model.ConnectionStatusResponse;
import com.laundry.r3reader.api.model.LinenLookupResponse;
import com.laundry.r3reader.api.model.MovementBatchRequest;
import com.laundry.r3reader.api.model.MovementBatchResponse;
import com.laundry.r3reader.api.model.TableLoginRequest;
import com.laundry.r3reader.api.model.TableLoginResponse;
import com.laundry.r3reader.data.BootstrapStore;
import com.laundry.r3reader.data.DeviceIdUtil;
import com.laundry.r3reader.data.ScannedLinenItem;

import java.io.IOException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import retrofit2.Call;
import retrofit2.Response;

public class TableRepository {

    private static final ExecutorService EXECUTOR = Executors.newFixedThreadPool(3);
    private static final Handler MAIN = new Handler(Looper.getMainLooper());

    private final TableApiService api;

    public TableRepository() {
        this.api = ApiClient.getService();
    }

    public void tableLogin(@NonNull String deviceId, @NonNull ApiCallback<TableLoginResponse> callback) {
        EXECUTOR.execute(() -> {
            try {
                String apiDeviceId = DeviceIdUtil.toApiDeviceId(deviceId);
                Response<TableLoginResponse> response = api.tableLogin(
                        new TableLoginRequest(apiDeviceId)).execute();
                if (!response.isSuccessful() || response.body() == null) {
                    postError(callback, errorMessage(response, "Login failed"));
                    return;
                }
                TableLoginResponse body = response.body();
                if (!body.success || body.token == null || body.token.isEmpty()) {
                    postError(callback, body.message != null ? body.message : "Login rejected");
                    return;
                }
                Global.saveAuthAfterLogin(body.token, body.customerId, apiDeviceId);
                postSuccess(callback, body);
            } catch (IOException e) {
                postError(callback, e.getMessage());
            }
        });
    }

    public void fetchBootstrap(@NonNull ApiCallback<BootstrapResponse> callback) {
        int readerId = Global.getReaderId();
        if (readerId <= 0) {
            postError(callback, "Reader not connected");
            return;
        }
        fetchBootstrap(readerId, callback);
    }

    public void fetchBootstrap(int readerId, @NonNull ApiCallback<BootstrapResponse> callback) {
        EXECUTOR.execute(() -> {
            try {
                Response<BootstrapResponse> response = api.bootstrap(readerId).execute();
                if (!response.isSuccessful() || response.body() == null) {
                    postError(callback, errorMessage(response, "Bootstrap failed"));
                    return;
                }
                BootstrapResponse body = response.body();
                if (!body.success) {
                    postError(callback, "Bootstrap unavailable");
                    return;
                }
                BootstrapStore.apply(body.reader, body.readerWays, body.syncPolicy);
                if (!BootstrapStore.getReaderWays().isEmpty()
                        && Global.getActiveReaderWayId() == 0) {
                    Global.setActiveReaderWayId(BootstrapStore.getReaderWays().get(0).id);
                }
                postSuccess(callback, body);
            } catch (IOException e) {
                postError(callback, e.getMessage());
            }
        });
    }

    public void lookupByTag(@NonNull String rfidTag, @NonNull ApiCallback<ScannedLinenItem> callback) {
        int readerId = Global.getReaderId();
        if (readerId <= 0) {
            postSuccess(callback, ApiMapper.mockFallback(rfidTag, rfidTag.hashCode()));
            return;
        }
        EXECUTOR.execute(() -> {
            try {
                Response<LinenLookupResponse> response = api.linenByTag(rfidTag, readerId).execute();
                if (!response.isSuccessful() || response.body() == null) {
                    postSuccess(callback, ApiMapper.mockFallback(rfidTag, rfidTag.hashCode()));
                    return;
                }
                LinenLookupResponse body = response.body();
                if (!body.success) {
                    postSuccess(callback, ApiMapper.mockFallback(rfidTag, rfidTag.hashCode()));
                    return;
                }
                postSuccess(callback, ApiMapper.fromLookup(rfidTag, body));
            } catch (IOException e) {
                postSuccess(callback, ApiMapper.mockFallback(rfidTag, rfidTag.hashCode()));
            }
        });
    }

    public void postMovementEvents(@NonNull MovementBatchRequest request,
                                   boolean syncEndpoint,
                                   @NonNull ApiCallback<MovementBatchResponse> callback) {
        EXECUTOR.execute(() -> {
            try {
                Call<MovementBatchResponse> call = syncEndpoint
                        ? api.postMovementEventsSync(request)
                        : api.postMovementEvents(request);
                Response<MovementBatchResponse> response = call.execute();
                if (!response.isSuccessful() || response.body() == null) {
                    postError(callback, errorMessage(response, "Upload failed"));
                    return;
                }
                MovementBatchResponse body = response.body();
                if (!body.success) {
                    postError(callback, body.message != null ? body.message : "Upload rejected");
                    return;
                }
                postSuccess(callback, body);
            } catch (IOException e) {
                postError(callback, e.getMessage());
            }
        });
    }

    /**
     * After BLE connect: connection-status → store readerId → GET bootstrap?readerId=.
     * Marks reader connected only when bootstrap succeeds.
     */
    public void connectReaderAndBootstrap(@NonNull String handheldId,
                                        @NonNull ApiCallback<BootstrapResponse> callback) {
        postConnectionStatus(handheldId, new ApiCallback<ConnectionStatusResponse>() {
            @Override
            public void onSuccess(ConnectionStatusResponse data) {
                fetchBootstrap(new ApiCallback<BootstrapResponse>() {
                    @Override
                    public void onSuccess(BootstrapResponse bootstrap) {
                        Global.setReaderConnected(true);
                        callback.onSuccess(bootstrap);
                    }

                    @Override
                    public void onError(String message) {
                        Global.clearReaderConnection();
                        String err = message != null ? message : "Bootstrap failed";
                        callback.onError(err);
                    }
                });
            }

            @Override
            public void onError(String message) {
                Global.clearReaderConnection();
                callback.onError(message);
            }
        });
    }

    public void postConnectionStatus(@NonNull String handheldId,
                                     @NonNull ApiCallback<ConnectionStatusResponse> callback) {
        int customerId = Global.getCustomerId();
        if (customerId <= 0) {
            postError(callback, "Not logged in");
            return;
        }
        ConnectionStatusRequest request = new ConnectionStatusRequest(
                handheldId,
                customerId,
                ApiMapper.nowUtcIso());
        EXECUTOR.execute(() -> {
            try {
                Response<ConnectionStatusResponse> response =
                        api.postConnectionStatus(request).execute();
                if (!response.isSuccessful() || response.body() == null) {
                    postError(callback, errorMessage(response, "Connection status failed"));
                    return;
                }
                ConnectionStatusResponse body = response.body();
                if (!body.success) {
                    postError(callback, body.message != null ? body.message : "Connection rejected");
                    return;
                }
                int readerId = body.readerId != null ? body.readerId
                        : (body.reader != null ? body.reader.id : 0);
                if (readerId <= 0) {
                    postError(callback, "No reader returned");
                    return;
                }
                Global.saveReaderIdFromConnection(readerId);
                if (body.reader != null) {
                    BootstrapStore.apply(body.reader, null, null);
                }
                postSuccess(callback, body);
            } catch (IOException e) {
                postError(callback, e.getMessage());
            }
        });
    }

    @Nullable
    private static <T> String errorMessage(@NonNull Response<T> response, @NonNull String fallback) {
        try {
            if (response.errorBody() != null) {
                String err = response.errorBody().string();
                if (!err.isEmpty()) {
                    return err;
                }
            }
        } catch (IOException ignored) {
        }
        return fallback + " (" + response.code() + ")";
    }

    private static <T> void postSuccess(@NonNull ApiCallback<T> callback, @NonNull T data) {
        MAIN.post(() -> callback.onSuccess(data));
    }

    private static <T> void postError(@NonNull ApiCallback<T> callback, @Nullable String message) {
        MAIN.post(() -> callback.onError(message));
    }
}
