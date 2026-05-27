package com.example.driverapp;

import android.content.Intent;
import android.os.Bundle;
import android.provider.Settings;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class LoginActivity extends AppCompatActivity {

    private TextView tvDeviceId;
    private Button btnLogin;
    private String deviceId;
    private final ExecutorService ioExecutor = Executors.newSingleThreadExecutor();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);

        tvDeviceId = findViewById(R.id.tvDeviceId);
        btnLogin = findViewById(R.id.btnLogin);

        deviceId = resolveDeviceId();
        tvDeviceId.setText(deviceId);

        btnLogin.setOnClickListener(v -> loginWithBackend());
    }

    private String resolveDeviceId() {
        String id = Settings.Secure.getString(
                getContentResolver(),
                Settings.Secure.ANDROID_ID
        );
        if (id == null || id.isEmpty()) {
            return "unknown_device";
        }
        return formatDeviceId(id);
    }

    private String formatDeviceId(String rawDeviceId) {
        if (rawDeviceId == null || rawDeviceId.length() < 16) {
            return rawDeviceId;
        }

        StringBuilder formatted = new StringBuilder();
        for (int i = 0; i < rawDeviceId.length(); i++) {
            if (i > 0 && i % 4 == 0) {
                formatted.append("-");
            }
            formatted.append(rawDeviceId.charAt(i));
        }
        return formatted.toString();
    }

    private void loginWithBackend() {
        btnLogin.setEnabled(false);
        ApiClient apiClient = new ApiClient();
        ioExecutor.execute(() -> {
            ApiClient.ApiResult loginResult = apiClient.loginWithDevice(deviceId);
            if (!loginResult.success) {
                runOnUiThread(() -> {
                    btnLogin.setEnabled(true);
                    Toast.makeText(this, "Login failed: " + loginResult.error, Toast.LENGTH_LONG).show();
                });
                return;
            }

            SessionStore sessionStore = new SessionStore(this);
            applyLoginResponse(sessionStore, loginResult.body);

            ApiClient.ApiResult bootstrapResult = apiClient.getBootstrap(sessionStore.getAuthToken());

            runOnUiThread(() -> {
                btnLogin.setEnabled(true);
                if (ApiClient.isHttpOk(bootstrapResult)
                        && bootstrapResult.body.optBoolean("success", false)) {
                    applyBootstrapResponse(sessionStore, bootstrapResult.body);
                } else if (!ApiClient.isHttpOk(bootstrapResult)) {
                    Toast.makeText(this, "Bootstrap failed; locations may be outdated.", Toast.LENGTH_SHORT).show();
                }
                proceedToApp();
            });
        });
    }

    private void applyLoginResponse(SessionStore sessionStore, JSONObject body) {
        sessionStore.setDeviceId(deviceId);
        JSONObject driverObj = body.optJSONObject("driver");
        String driverId = "";
        String driverName = "";
        String driverHandheld = "";
        if (driverObj != null) {
            int id = driverObj.optInt("id", 0);
            if (id > 0) {
                driverId = String.valueOf(id);
            } else {
                driverId = driverObj.optString("id", "");
            }
            driverName = driverObj.optString("name", "");
            driverHandheld = driverObj.optString("deviceId", "");
        }
        sessionStore.setDriverId(driverId);
        sessionStore.setDriverName(driverName);
        if (!driverHandheld.isEmpty()) {
            sessionStore.setDriverHandheldDeviceId(driverHandheld);
        }
        sessionStore.setCustomerId(jsonScalarToString(body, "customerId"));
        sessionStore.setAuthToken(body.optString("token", ""));
        JSONArray locations = body.optJSONArray("locations");
        if (locations != null) {
            sessionStore.setLocationsJson(locations.toString());
        }
    }

    private void applyBootstrapResponse(SessionStore sessionStore, JSONObject body) {
        sessionStore.setBootstrapJson(body.toString());
        JSONArray locations = body.optJSONArray("locations");
        if (locations != null) {
            sessionStore.setLocationsJson(locations.toString());
        }
        JSONObject driver = body.optJSONObject("driver");
        if (driver != null) {
            String dh = driver.optString("deviceId", "");
            if (!dh.isEmpty()) {
                sessionStore.setDriverHandheldDeviceId(dh);
            }
            String driverName = driver.optString("name", "");
            if (!driverName.isEmpty()) {
                sessionStore.setDriverName(driverName);
            }
            int did = driver.optInt("id", 0);
            if (did > 0) {
                sessionStore.setDriverId(String.valueOf(did));
            }
        }
        String cid = jsonScalarToString(body, "customerId");
        if (!cid.isEmpty()) {
            sessionStore.setCustomerId(cid);
        }
    }

    private static String jsonScalarToString(JSONObject o, String key) {
        if (o == null || !o.has(key) || o.isNull(key)) {
            return "";
        }
        Object v = o.opt(key);
        return v == null ? "" : String.valueOf(v);
    }

    private void proceedToApp() {
        Intent intent = new Intent(this, ScannerConnectActivity.class);
        intent.putExtra("device_id", deviceId);
        startActivity(intent);
        finish();
        overridePendingTransition(android.R.anim.fade_in, android.R.anim.fade_out);
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        ioExecutor.shutdownNow();
    }
}
