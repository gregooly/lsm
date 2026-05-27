package com.laundry.r3reader.ui.connect;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.recyclerview.widget.RecyclerView;

import com.laundry.r3reader.R;
import com.laundry.r3reader.ble.BleDiscoveredDevice;

import java.util.ArrayList;
import java.util.List;

public class BleDeviceAdapter extends RecyclerView.Adapter<BleDeviceAdapter.Holder> {

    public interface OnDeviceSelectedListener {
        void onDeviceSelected(@NonNull BleDiscoveredDevice device);
    }

    private final List<BleDiscoveredDevice> items = new ArrayList<>();
    @Nullable
    private final OnDeviceSelectedListener listener;
    @Nullable
    private String selectedAddress;

    public BleDeviceAdapter(@Nullable OnDeviceSelectedListener listener) {
        this.listener = listener;
    }

    public void setDevices(@NonNull List<BleDiscoveredDevice> devices) {
        items.clear();
        items.addAll(devices);
        notifyDataSetChanged();
    }

    public void addDevice(@NonNull BleDiscoveredDevice device) {
        for (BleDiscoveredDevice d : items) {
            if (d.address.equals(device.address)) {
                notifyDataSetChanged();
                return;
            }
        }
        items.add(device);
        notifyDataSetChanged();
    }

    public void setSelectedAddress(@Nullable String address) {
        selectedAddress = address;
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public Holder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_ble_device, parent, false);
        return new Holder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull Holder holder, int position) {
        BleDiscoveredDevice d = items.get(position);
        holder.tvName.setText(d.name);
        holder.tvMac.setText(holder.itemView.getContext().getString(R.string.mac_prefix, d.address));
        boolean selected = d.address.equals(selectedAddress);
        holder.ivSelected.setVisibility(selected ? View.VISIBLE : View.GONE);
        holder.itemView.setOnClickListener(v -> {
            if (listener != null) {
                listener.onDeviceSelected(d);
            }
        });
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class Holder extends RecyclerView.ViewHolder {
        final TextView tvName;
        final TextView tvMac;
        final ImageView ivSelected;

        Holder(@NonNull View itemView) {
            super(itemView);
            tvName = itemView.findViewById(R.id.tvDeviceName);
            tvMac = itemView.findViewById(R.id.tvDeviceMac);
            ivSelected = itemView.findViewById(R.id.ivSelected);
        }
    }
}
