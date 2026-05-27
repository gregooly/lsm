package com.example.driverapp;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;

public class TaskSelectActivity extends AppCompatActivity {

    public static final String EXTRA_ROUTE_LABEL = "route_label";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_task_select);

        ImageView ivBack = findViewById(R.id.ivBack);
        ivBack.setOnClickListener(v -> finish());

        TextView tvNoTasks = findViewById(R.id.tvNoTasks);
        RecyclerView rvTasks = findViewById(R.id.rvTasks);
        rvTasks.setLayoutManager(new LinearLayoutManager(this));

        List<TaskItem> tasks = loadTasksFromBootstrap();
        TaskAdapter adapter = new TaskAdapter(item -> onTaskSelected(item));
        rvTasks.setAdapter(adapter);
        adapter.setTasks(tasks);

        tvNoTasks.setVisibility(tasks.isEmpty() ? View.VISIBLE : View.GONE);
    }

    private void onTaskSelected(TaskItem item) {
        SessionStore sessionStore = new SessionStore(this);
        sessionStore.setActiveJobId(String.valueOf(item.jobId));
        sessionStore.setActiveReaderWayId(String.valueOf(item.readerWayId));
        sessionStore.setActiveReaderId(String.valueOf(item.readerId));

        Intent intent = new Intent(this, ScanActivity.class);
        intent.putExtra(EXTRA_ROUTE_LABEL, item.routeSubtitle());
        intent.putExtra(ScanActivity.EXTRA_LOCATION_NAME, item.summaryTitle());
        startActivity(intent);
    }

    private List<TaskItem> loadTasksFromBootstrap() {
        List<TaskItem> out = new ArrayList<>();
        SessionStore sessionStore = new SessionStore(this);
        try {
            JSONObject root = new JSONObject(sessionStore.getBootstrapJson());
            JSONArray arr = root.optJSONArray("activeTasks");
            if (arr == null) {
                return out;
            }
            for (int i = 0; i < arr.length(); i++) {
                JSONObject o = arr.optJSONObject(i);
                if (o == null) {
                    continue;
                }
                String status = o.optString("jobStatus", "").toLowerCase();
                if (!status.equals("open") && !status.equals("in_progress")) {
                    continue;
                }
                int jobId = o.optInt("jobId", 0);
                if (jobId == 0) {
                    jobId = o.optInt("id", 0);
                }

                JSONObject fromObj = o.optJSONObject("fromLocation");
                JSONObject toObj = o.optJSONObject("toLocation");
                String fromName = o.optString("fromLocationName", "");
                String toName = o.optString("toLocationName", "");
                if (fromName.isEmpty() && fromObj != null) {
                    fromName = fromObj.optString("name", "");
                }
                if (toName.isEmpty() && toObj != null) {
                    toName = toObj.optString("name", "");
                }

                out.add(new TaskItem(
                        jobId,
                        o.optString("jobType", ""),
                        o.optString("jobStatus", ""),
                        o.optInt("readerWayId", 0),
                        o.optInt("readerId", 0),
                        fromName,
                        toName,
                        o.optString("priority", "")
                ));
            }
        } catch (Exception ignored) {
        }
        return out;
    }
}
