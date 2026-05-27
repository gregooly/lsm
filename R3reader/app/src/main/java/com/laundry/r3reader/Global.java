package com.laundry.r3reader;

import android.content.Context;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.laundry.r3reader.api.ApiClient;
import com.laundry.r3reader.api.TableRepository;
import com.laundry.r3reader.ble.UhfBleManager;
import com.laundry.r3reader.api.model.MovementBatchRequest;
import com.laundry.r3reader.data.BootstrapStore;
import com.laundry.r3reader.data.DeviceIdUtil;
import com.laundry.r3reader.data.LinenCategory;
import com.laundry.r3reader.data.MovementQueueStore;
import com.laundry.r3reader.data.PrefsManager;
import com.laundry.r3reader.data.ReaderWayInfo;
import com.laundry.r3reader.data.ScanSessionStore;
import com.laundry.r3reader.data.ScannedLinenItem;
import com.laundry.r3reader.data.WorkflowType;

import java.util.List;
import java.util.Locale;

/**
 * Shared configuration, services, and session state used across the app.
 * Use this instead of duplicating URLs, repository instances, or prefs access.
 */
public final class Global {

    /** LaundryMS API host (no trailing slash). Change for your environment. */
    public static final String SERVER_URL = "http://172.20.1.28:5148";

    private static volatile TableRepository repository;

    private Global() {
    }

    public static void init() {
        ApiClient.reset();
        UhfBleManager.getInstance().init(appContext());
        getRepository();
    }

    @NonNull
    public static String getBaseUrl() {
        String url = SERVER_URL.trim();
        if (url.isEmpty()) {
            throw new IllegalStateException("Global.SERVER_URL must not be empty");
        }
        return url.endsWith("/") ? url : url + "/";
    }

    @NonNull
    public static Context appContext() {
        return R3Application.getInstance();
    }

    // --- API / repository ---

    @NonNull
    public static TableRepository getRepository() {
        if (repository == null) {
            synchronized (Global.class) {
                if (repository == null) {
                    repository = new TableRepository();
                }
            }
        }
        return repository;
    }

    public static void resetApi() {
        repository = null;
        ApiClient.reset();
    }

    // --- Auth & device (PrefsManager) ---

    public static boolean isLoggedIn() {
        return PrefsManager.isLoggedIn(appContext());
    }

    @NonNull
    public static String getToken() {
        return PrefsManager.getToken(appContext());
    }

    public static int getCustomerId() {
        return PrefsManager.getCustomerId(appContext());
    }

    public static int getReaderId() {
        return PrefsManager.getReaderId(appContext());
    }

    public static int getActiveReaderWayId() {
        return PrefsManager.getActiveReaderWayId(appContext());
    }

    public static void setActiveReaderWayId(int readerWayId) {
        PrefsManager.setActiveReaderWayId(appContext(), readerWayId);
    }

    @NonNull
    public static WorkflowType getActiveWorkflow() {
        return PrefsManager.getActiveWorkflow(appContext());
    }

    public static void setActiveWorkflow(@NonNull WorkflowType workflow) {
        PrefsManager.setActiveWorkflow(appContext(), workflow);
    }

    @NonNull
    public static String getDeviceIdFormatted() {
        String saved = PrefsManager.getDeviceId(appContext());
        if (saved != null && !saved.isEmpty()) {
            return saved;
        }
        return DeviceIdUtil.getFormattedDeviceId(appContext());
    }

    @NonNull
    public static String getDeviceIdRaw() {
        return DeviceIdUtil.getRawAndroidId(appContext());
    }

    /** Hyphenated device ID for API payloads (matches DB format). */
    @NonNull
    public static String getDeviceIdForApi() {
        return DeviceIdUtil.toApiDeviceId(getDeviceIdFormatted());
    }

    public static void saveDeviceIdFormatted(@NonNull String formatted) {
        PrefsManager.setDeviceId(appContext(), formatted);
    }

    public static boolean isReaderConnected() {
        return PrefsManager.isReaderConnected(appContext());
    }

    public static void setReaderConnected(boolean connected) {
        PrefsManager.setReaderConnected(appContext(), connected);
    }

    /** After table-login: stores token + customerId only (readerId = 0 until connect). */
    public static void saveAuthAfterLogin(@NonNull String token, int customerId,
                                          @NonNull String deviceId) {
        PrefsManager.saveAuthAfterLogin(appContext(), token, customerId, deviceId);
    }

    public static void saveReaderIdFromConnection(int readerId) {
        PrefsManager.setReaderId(appContext(), readerId);
    }

    /** BLE up, API readerId + bootstrap ways loaded. */
    public static boolean isReaderSessionReady() {
        return UhfBleManager.getInstance().isBleConnected()
                && isReaderConnected()
                && hasReaderId()
                && BootstrapStore.hasReaderWays();
    }

    public static void setHandheldId(@NonNull String handheldId) {
        PrefsManager.setHandheldId(appContext(), handheldId);
    }

    @NonNull
    public static String getHandheldId() {
        return PrefsManager.getHandheldId(appContext());
    }

    public static boolean hasReaderId() {
        return getReaderId() > 0;
    }

    /** Disconnect BLE reader: stops inventory, SDK disconnect, clears readerId/bootstrap. */
    public static void clearReaderConnection() {
        UhfBleManager.getInstance().stopInventory();
        UhfBleManager.getInstance().disconnect();
        PrefsManager.clearReaderConnection(appContext());
    }

    /** Logout: clears auth, bootstrap, scan session, offline queue, and API client. */
    public static void clearSession() {
        PrefsManager.clearAuth(appContext());
        MovementQueueStore.clearAll(appContext());
        ScanSessionStore.clear();
        resetApi();
    }

    // --- Bootstrap ---

    @NonNull
    public static List<ReaderWayInfo> getReaderWays() {
        return BootstrapStore.getReaderWays();
    }

    public static boolean hasReaderWays() {
        return BootstrapStore.hasReaderWays();
    }

    @Nullable
    public static ReaderWayInfo findReaderWayById(int id) {
        return BootstrapStore.findWayById(id);
    }

    public static int getMaxBatchSize() {
        return BootstrapStore.getMaxBatchSize();
    }

    // --- Active scan session ---

    @NonNull
    public static String newSessionId() {
        return String.format(Locale.US, "SS-%06d", System.currentTimeMillis() % 1_000_000L);
    }

    public static void setLastScanSession(@NonNull String sessionId,
                                          int readerWayId,
                                          @NonNull String wayTitle,
                                          @NonNull String movement,
                                          @NonNull String targetStatus,
                                          @NonNull List<ScannedLinenItem> items) {
        ScanSessionStore.setLastSession(
                sessionId,
                readerWayId,
                getCustomerId(),
                getReaderId(),
                wayTitle,
                movement,
                targetStatus,
                items);
    }

    @NonNull
    public static List<ScannedLinenItem> getScannedItems() {
        return ScanSessionStore.getItems();
    }

    @NonNull
    public static String getScanSessionId() {
        return ScanSessionStore.getSessionId();
    }

    public static int getScanReaderWayId() {
        return ScanSessionStore.getReaderWayId();
    }

    @NonNull
    public static String getScanReaderWayTitle() {
        return ScanSessionStore.getReaderWayTitle();
    }

    @NonNull
    public static String getScanReaderWayTarget() {
        return ScanSessionStore.getReaderWayTarget();
    }

    public static int countScanned(@NonNull LinenCategory category) {
        return ScanSessionStore.count(category);
    }

    // --- Offline movement queue ---

    public static void enqueuePendingMovement(@NonNull MovementBatchRequest batch) {
        MovementQueueStore.enqueuePending(appContext(), batch);
    }

    @NonNull
    public static List<MovementBatchRequest> getPendingMovements() {
        return MovementQueueStore.getPending(appContext());
    }

    @NonNull
    public static List<MovementBatchRequest> getFailedMovements() {
        return MovementQueueStore.getFailed(appContext());
    }

    public static void removePendingMovementAt(int index) {
        MovementQueueStore.removePendingAt(appContext(), index);
    }

    public static void enqueueFailedMovement(@NonNull MovementBatchRequest batch) {
        MovementQueueStore.enqueueFailed(appContext(), batch);
    }
}
