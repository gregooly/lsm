package com.example.driverapp;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public class SyncStatusAdapter extends RecyclerView.Adapter<SyncStatusAdapter.RecordViewHolder> {
    private final List<SyncRecordItem> items = new ArrayList<>();

    public void setItems(List<SyncRecordItem> next) {
        items.clear();
        items.addAll(next);
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public RecordViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_sync_record, parent, false);
        return new RecordViewHolder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull RecordViewHolder holder, int position) {
        SyncRecordItem item = items.get(position);
        holder.tvTitle.setText(item.title);
        holder.tvSubtitle.setText(item.subtitle);
        holder.tvTime.setText(item.timeText);

        String status = item.status == null ? "" : item.status.toLowerCase(Locale.US);
        if (SyncRecordItem.STATUS_PENDING.equals(status)) {
            holder.badgeContainer.setBackgroundResource(R.drawable.bg_badge_pending);
            holder.ivBadgeIcon.setVisibility(View.GONE);
            holder.tvBadgeText.setText(holder.itemView.getContext().getString(R.string.status_pending));
            holder.tvBadgeText.setTextColor(holder.itemView.getContext().getColor(R.color.warning));
        } else if (SyncRecordItem.STATUS_FAILED.equals(status)) {
            holder.badgeContainer.setBackgroundResource(R.drawable.bg_badge_failed);
            holder.ivBadgeIcon.setVisibility(View.VISIBLE);
            holder.ivBadgeIcon.setImageResource(R.drawable.ic_error_circle);
            holder.tvBadgeText.setText(holder.itemView.getContext().getString(R.string.status_failed));
            holder.tvBadgeText.setTextColor(holder.itemView.getContext().getColor(R.color.error));
        } else {
            holder.badgeContainer.setBackgroundResource(R.drawable.bg_badge_uploaded);
            holder.ivBadgeIcon.setVisibility(View.VISIBLE);
            holder.ivBadgeIcon.setImageResource(R.drawable.ic_check_circle);
            holder.tvBadgeText.setText(holder.itemView.getContext().getString(R.string.status_uploaded));
            holder.tvBadgeText.setTextColor(holder.itemView.getContext().getColor(R.color.success));
        }
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static final class RecordViewHolder extends RecyclerView.ViewHolder {
        final TextView tvTitle;
        final TextView tvSubtitle;
        final TextView tvTime;
        final LinearLayout badgeContainer;
        final ImageView ivBadgeIcon;
        final TextView tvBadgeText;

        RecordViewHolder(@NonNull View itemView) {
            super(itemView);
            tvTitle = itemView.findViewById(R.id.tvRecordTitle);
            tvSubtitle = itemView.findViewById(R.id.tvRecordSubtitle);
            tvTime = itemView.findViewById(R.id.tvRecordTime);
            badgeContainer = itemView.findViewById(R.id.badgeContainer);
            ivBadgeIcon = itemView.findViewById(R.id.ivBadgeIcon);
            tvBadgeText = itemView.findViewById(R.id.tvBadgeText);
        }
    }
}
