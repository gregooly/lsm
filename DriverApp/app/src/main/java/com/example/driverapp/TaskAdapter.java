package com.example.driverapp;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;

public class TaskAdapter extends RecyclerView.Adapter<TaskAdapter.TaskViewHolder> {

    public interface OnTaskClickListener {
        void onClick(TaskItem item);
    }

    private final List<TaskItem> items = new ArrayList<>();
    private final OnTaskClickListener listener;

    public TaskAdapter(OnTaskClickListener listener) {
        this.listener = listener;
    }

    public void setTasks(List<TaskItem> tasks) {
        items.clear();
        items.addAll(tasks);
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public TaskViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_task, parent, false);
        return new TaskViewHolder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull TaskViewHolder holder, int position) {
        TaskItem item = items.get(position);
        holder.tvTitle.setText(item.summaryTitle());
        holder.tvRoute.setText(item.routeSubtitle());
        holder.tvStatus.setText(item.jobStatus != null ? item.jobStatus : "");
        holder.itemView.setOnClickListener(v -> listener.onClick(item));
        holder.ivBadge.setImageResource(resolveBadge(item.jobType));
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    private int resolveBadge(String jobType) {
        if (jobType == null) {
            return R.drawable.ic_laundry;
        }
        String lower = jobType.toLowerCase();
        if (lower.contains("pickup") || lower.contains("collect")) {
            return R.drawable.ic_clipboard;
        }
        if (lower.contains("delivery") || lower.contains("dispatch")) {
            return R.drawable.ic_send;
        }
        return R.drawable.ic_laundry;
    }

    static class TaskViewHolder extends RecyclerView.ViewHolder {
        final ImageView ivBadge;
        final TextView tvTitle;
        final TextView tvRoute;
        final TextView tvStatus;

        TaskViewHolder(@NonNull View itemView) {
            super(itemView);
            ivBadge = itemView.findViewById(R.id.ivTaskBadge);
            tvTitle = itemView.findViewById(R.id.tvTaskTitle);
            tvRoute = itemView.findViewById(R.id.tvTaskRoute);
            tvStatus = itemView.findViewById(R.id.tvTaskStatus);
        }
    }
}
