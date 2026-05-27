package com.laundry.r3reader.ui.results;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.RecyclerView;

import com.laundry.r3reader.R;
import com.laundry.r3reader.data.LinenCategory;
import com.laundry.r3reader.data.ScannedLinenItem;

import java.util.List;

public class ScanResultAdapter extends RecyclerView.Adapter<ScanResultAdapter.Holder> {

    private final List<ScannedLinenItem> items;

    public ScanResultAdapter(@NonNull List<ScannedLinenItem> items) {
        this.items = items;
    }

    @NonNull
    @Override
    public Holder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_scan_result_row, parent, false);
        return new Holder(v);
    }

    @Override
    public void onBindViewHolder(@NonNull Holder holder, int position) {
        ScannedLinenItem item = items.get(position);
        holder.tvName.setText(item.displayName);
        holder.tvEpc.setText(holder.itemView.getContext()
                .getString(R.string.epc_prefix, item.epc));
        holder.chip.setText(holder.itemView.getContext().getString(item.category.labelRes));
        styleChip(holder, item.category);
    }

    private void styleChip(@NonNull Holder holder, @NonNull LinenCategory category) {
        int textColor;
        int bg;
        switch (category) {
            case DAMAGED:
                textColor = R.color.warning_orange;
                bg = R.drawable.chip_completed_orange;
                break;
            case LOST:
                textColor = R.color.error_red;
                bg = R.drawable.chip_category_lost;
                break;
            case NORMAL:
            default:
                textColor = R.color.success_green;
                bg = R.drawable.chip_completed;
                break;
        }
        holder.chip.setTextColor(ContextCompat.getColor(holder.itemView.getContext(), textColor));
        holder.chip.setBackgroundResource(bg);
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class Holder extends RecyclerView.ViewHolder {
        final TextView tvName;
        final TextView tvEpc;
        final TextView chip;

        Holder(@NonNull View itemView) {
            super(itemView);
            tvName = itemView.findViewById(R.id.tvItemName);
            tvEpc = itemView.findViewById(R.id.tvItemEpc);
            chip = itemView.findViewById(R.id.chipCategory);
        }
    }
}
