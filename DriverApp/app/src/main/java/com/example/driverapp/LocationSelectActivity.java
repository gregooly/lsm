package com.example.driverapp;

import android.content.Intent;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;

public class LocationSelectActivity extends AppCompatActivity {

    private LocationAdapter adapter;
    private final List<LocationItem> locations = new ArrayList<>();
    private TextView tvNoLocations;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_location_select);

        ImageView ivBack = findViewById(R.id.ivBack);
        ivBack.setOnClickListener(v -> finish());

        tvNoLocations = findViewById(R.id.tvNoLocations);
        EditText etSearch = findViewById(R.id.etSearch);
        RecyclerView rvLocations = findViewById(R.id.rvLocations);
        rvLocations.setLayoutManager(new LinearLayoutManager(this));

        adapter = new LocationAdapter(item -> navigateToScan(item.name));
        rvLocations.setAdapter(adapter);

        loadLocationsFromSession();
        adapter.setItems(locations);
        updateEmptyState(locations.isEmpty());

        etSearch.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {
            }

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {
                adapter.filter(s.toString());
                updateEmptyState(adapter.getItemCount() == 0);
            }

            @Override
            public void afterTextChanged(Editable s) {
            }
        });

        Button btnScanQr = findViewById(R.id.btnScanQr);
        btnScanQr.setOnClickListener(v -> navigateToScan("QR Scanned Location"));
    }

    private void navigateToScan(String locationName) {
        Intent intent = new Intent(this, ScanActivity.class);
        intent.putExtra(ScanActivity.EXTRA_LOCATION_NAME, locationName);
        startActivity(intent);
    }

    private void loadLocationsFromSession() {
        locations.clear();
        SessionStore sessionStore = new SessionStore(this);
        String raw = sessionStore.getLocationsJson();
        try {
            JSONArray arr = new JSONArray(raw);
            for (int i = 0; i < arr.length(); i++) {
                JSONObject obj = arr.optJSONObject(i);
                if (obj == null) {
                    continue;
                }
                if (!obj.optBoolean("isActive", true)) {
                    continue;
                }
                locations.add(new LocationItem(
                        obj.optInt("id", 0),
                        obj.optString("name", "Unknown"),
                        obj.optString("type", ""),
                        obj.optString("address", "")
                ));
            }
        } catch (Exception ignored) {
        }
    }

    private void updateEmptyState(boolean showEmpty) {
        tvNoLocations.setVisibility(showEmpty ? View.VISIBLE : View.GONE);
    }
}
