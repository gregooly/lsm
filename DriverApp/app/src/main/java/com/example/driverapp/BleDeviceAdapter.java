package com.example.driverapp;

import android.annotation.SuppressLint;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class BleDeviceAdapter extends RecyclerView.Adapter<BleDeviceAdapter.Vh> {

    public interface OnDeviceClickListener {
        void onDeviceClick(BleDeviceRow device);
    }

    private final List<BleDeviceRow> items = new ArrayList<>();
    private final OnDeviceClickListener listener;

    public BleDeviceAdapter(OnDeviceClickListener listener) {
        this.listener = listener;
    }

    public void setItems(List<BleDeviceRow> next) {
        items.clear();
        if (next != null) {
            items.addAll(next);
            Collections.sort(items);
        }
        notifyDataSetChanged();
    }

    public void upsert(BleDeviceRow row) {
        for (int i = 0; i < items.size(); i++) {
            if (items.get(i).address.equalsIgnoreCase(row.address)) {
                items.get(i).name = row.name;
                items.get(i).rssi = row.rssi;
                Collections.sort(items);
                notifyDataSetChanged();
                return;
            }
        }
        items.add(row);
        Collections.sort(items);
        notifyDataSetChanged();
    }

    public boolean isEmpty() {
        return items.isEmpty();
    }

    @NonNull
    @Override
    public Vh onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_ble_device, parent, false);
        return new Vh(v);
    }

    @Override
    public void onBindViewHolder(@NonNull Vh holder, int position) {
        BleDeviceRow d = items.get(position);
        holder.tvName.setText(d.name.isEmpty() ? "—" : d.name);
        holder.tvAddress.setText(d.address);
        holder.tvRssi.setText(holder.itemView.getContext().getString(R.string.ble_rssi_format, d.rssi));
        holder.itemView.setOnClickListener(v -> {
            if (listener != null) {
                listener.onDeviceClick(d);
            }
        });
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static final class Vh extends RecyclerView.ViewHolder {
        final TextView tvName;
        final TextView tvAddress;
        final TextView tvRssi;

        Vh(@NonNull View itemView) {
            super(itemView);
            tvName = itemView.findViewById(R.id.tvBleName);
            tvAddress = itemView.findViewById(R.id.tvBleAddress);
            tvRssi = itemView.findViewById(R.id.tvBleRssi);
        }
    }
}
