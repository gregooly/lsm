package com.laundry.r3reader.ui.item;

import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.content.ContextCompat;

import com.google.android.material.appbar.MaterialToolbar;
import com.laundry.r3reader.R;
import com.laundry.r3reader.data.MockData;
import com.laundry.r3reader.ui.base.BaseFragment;

public class ItemDetailsFragment extends BaseFragment {

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_item_details, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, true);

        TextView tvEpc = view.findViewById(R.id.tvEpc);
        if (tvEpc != null) {
            tvEpc.setText(MockData.sampleEpcs()[0]);
        }
        view.findViewById(R.id.btnCopyEpc).setOnClickListener(v -> {
            ClipboardManager cm = (ClipboardManager) requireContext()
                    .getSystemService(Context.CLIPBOARD_SERVICE);
            if (cm != null && tvEpc != null) {
                cm.setPrimaryClip(ClipData.newPlainText("epc", tvEpc.getText()));
                Toast.makeText(requireContext(), R.string.epc_rfid_tag, Toast.LENGTH_SHORT).show();
            }
        });

        LinearLayout layout = view.findViewById(R.id.layoutItemInfo);
        int green = ContextCompat.getColor(requireContext(), R.color.success_green);
        for (String[] row : MockData.itemInfoRows(com.laundry.r3reader.data.LinenCategory.NORMAL)) {
            View rowView = LayoutInflater.from(requireContext())
                    .inflate(R.layout.item_batch_kv_row, layout, false);
            TextView tvLabel = rowView.findViewById(R.id.tvLabel);
            TextView tvValue = rowView.findViewById(R.id.tvValue);
            if (tvLabel != null) tvLabel.setText(row[0]);
            if (tvValue != null) {
                tvValue.setText(row[1]);
                if ("Condition".equals(row[0])) {
                    tvValue.setTextColor(green);
                } else {
                    tvValue.setTextColor(ContextCompat.getColor(requireContext(), R.color.primary_dark));
                }
            }
            layout.addView(rowView);
        }
    }
}
