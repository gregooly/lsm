package com.laundry.r3reader.ui.sync;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.card.MaterialCardView;
import com.laundry.r3reader.R;
import com.laundry.r3reader.api.model.MovementBatchRequest;

import java.util.List;

public class SyncQueueAdapter extends RecyclerView.Adapter<SyncQueueAdapter.Holder> {

    private final List<MovementBatchRequest> items;
    private final boolean failedStyle;

    public SyncQueueAdapter(List<MovementBatchRequest> items, boolean failedStyle) {
        this.items = items;
        this.failedStyle = failedStyle;
    }

    @NonNull
    @Override
    public Holder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_sync_queue, parent, false);
        return new Holder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull Holder holder, int position) {
        MovementBatchRequest item = items.get(position);
        int count = item.events != null ? item.events.size() : 0;
        holder.title.setText(holder.itemView.getContext()
                .getString(R.string.movement_batch_title, item.readerWayId));
        holder.items.setText(holder.itemView.getContext()
                .getString(R.string.session_items_format, count));
        holder.time.setText(item.uploadedAt != null ? item.uploadedAt : "");

        if (failedStyle) {
            holder.card.setCardBackgroundColor(holder.itemView.getContext()
                    .getColor(R.color.light_error));
            holder.ivIcon.setBackgroundResource(R.drawable.bg_icon_error);
            holder.ivIcon.setImageResource(R.drawable.ic_warning_outline);
            holder.ivAction.setImageResource(R.drawable.ic_warning_outline);
        } else {
            holder.card.setCardBackgroundColor(holder.itemView.getContext()
                    .getColor(R.color.card_background));
            holder.ivIcon.setBackgroundResource(R.drawable.bg_icon_success);
            holder.ivIcon.setImageResource(R.drawable.ic_sync_outline);
            holder.ivAction.setImageResource(R.drawable.ic_sync_outline);
        }
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class Holder extends RecyclerView.ViewHolder {
        final MaterialCardView card;
        final TextView title;
        final TextView items;
        final TextView time;
        final ImageView ivIcon;
        final ImageView ivAction;

        Holder(@NonNull View itemView) {
            super(itemView);
            card = itemView.findViewById(R.id.cardQueueItem);
            title = itemView.findViewById(R.id.tvQueueTitle);
            items = itemView.findViewById(R.id.tvQueueItems);
            time = itemView.findViewById(R.id.tvQueueTime);
            ivIcon = itemView.findViewById(R.id.ivQueueIcon);
            ivAction = itemView.findViewById(R.id.ivQueueAction);
        }
    }
}
