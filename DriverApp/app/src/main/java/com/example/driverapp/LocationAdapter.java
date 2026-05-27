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
import java.util.Locale;

public class LocationAdapter extends RecyclerView.Adapter<LocationAdapter.LocationViewHolder> {

    public interface OnLocationClickListener {
        void onClick(LocationItem item);
    }

    private final List<LocationItem> allItems = new ArrayList<>();
    private final List<LocationItem> filteredItems = new ArrayList<>();
    private final OnLocationClickListener listener;

    public LocationAdapter(OnLocationClickListener listener) {
        this.listener = listener;
    }

    public void setItems(List<LocationItem> items) {
        allItems.clear();
        allItems.addAll(items);
        filter("");
    }

    public void filter(String query) {
        String q = query == null ? "" : query.trim().toLowerCase(Locale.US);
        filteredItems.clear();
        if (q.isEmpty()) {
            filteredItems.addAll(allItems);
        } else {
            for (LocationItem item : allItems) {
                String name = item.name == null ? "" : item.name.toLowerCase(Locale.US);
                String address = item.address == null ? "" : item.address.toLowerCase(Locale.US);
                String type = item.type == null ? "" : item.type.toLowerCase(Locale.US);
                if (name.contains(q) || address.contains(q) || type.contains(q)) {
                    filteredItems.add(item);
                }
            }
        }
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public LocationViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_location, parent, false);
        return new LocationViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull LocationViewHolder holder, int position) {
        LocationItem item = filteredItems.get(position);
        holder.tvName.setText(item.name);
        holder.tvAddress.setText(item.address == null || item.address.isEmpty() ? "Unknown address" : item.address);
        holder.ivType.setImageResource(resolveTypeIcon(item.type));
        holder.itemView.setOnClickListener(v -> listener.onClick(item));
    }

    @Override
    public int getItemCount() {
        return filteredItems.size();
    }

    private int resolveTypeIcon(String type) {
        if (type == null) {
            return R.drawable.ic_laundry;
        }
        String lower = type.toLowerCase(Locale.US);
        if (lower.contains("hotel")) {
            return R.drawable.ic_hotel;
        }
        if (lower.contains("hospital")) {
            return R.drawable.ic_hospital;
        }
        if (lower.contains("restaurant")) {
            return R.drawable.ic_restaurant;
        }
        return R.drawable.ic_laundry;
    }

    static class LocationViewHolder extends RecyclerView.ViewHolder {
        final ImageView ivType;
        final TextView tvName;
        final TextView tvAddress;

        LocationViewHolder(@NonNull View itemView) {
            super(itemView);
            ivType = itemView.findViewById(R.id.ivType);
            tvName = itemView.findViewById(R.id.tvLocationName);
            tvAddress = itemView.findViewById(R.id.tvLocationAddress);
        }
    }
}
