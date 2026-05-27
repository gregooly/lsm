package com.laundry.r3reader.ui.base;

import android.view.View;

import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.navigation.NavController;
import androidx.navigation.fragment.NavHostFragment;

import com.google.android.material.appbar.MaterialToolbar;

public abstract class BaseFragment extends Fragment {

    protected void setupToolbar(MaterialToolbar toolbar, boolean showBack) {
        if (toolbar == null) return;
        if (showBack) {
            toolbar.setNavigationIcon(com.laundry.r3reader.R.drawable.ic_back);
            toolbar.setNavigationOnClickListener(v -> {
                NavController nav = NavHostFragment.findNavController(this);
                if (!nav.navigateUp()) {
                    requireActivity().getOnBackPressedDispatcher().onBackPressed();
                }
            });
        } else {
            toolbar.setNavigationIcon(null);
        }
    }

    @Nullable
    protected NavController nav() {
        try {
            return NavHostFragment.findNavController(this);
        } catch (Exception e) {
            return null;
        }
    }

    protected void bindSummaryRow(View row, String label, String value, int valueColor) {
        if (row == null) return;
        android.widget.TextView tvLabel = row.findViewById(com.laundry.r3reader.R.id.tvLabel);
        android.widget.TextView tvValue = row.findViewById(com.laundry.r3reader.R.id.tvValue);
        if (tvLabel != null) tvLabel.setText(label);
        if (tvValue != null) {
            tvValue.setText(value);
            tvValue.setTextColor(valueColor);
        }
    }
}
