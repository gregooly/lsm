package com.laundry.r3reader.ui.settings;

import android.content.Intent;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.google.android.material.appbar.MaterialToolbar;
import com.google.android.material.button.MaterialButton;
import com.laundry.r3reader.R;
import com.laundry.r3reader.Global;
import com.laundry.r3reader.ui.base.BaseFragment;
import com.laundry.r3reader.ui.login.LoginActivity;

public class SettingsFragment extends BaseFragment {

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_settings, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, true);

        LinearLayout readerLayout = view.findViewById(R.id.layoutReaderSettings);
        addSettingRow(readerLayout, R.string.reader_name, "TABLE-RFID-01", false);
        addSettingRow(readerLayout, R.string.reader_type, getString(R.string.table_reader), false);
        addSettingRow(readerLayout, R.string.action_mode, getString(R.string.entry), true);
        addSettingRow(readerLayout, R.string.workflow_stage, getString(R.string.sorting_preparation), true);
        addSettingRow(readerLayout, R.string.location, "Clean Area", true);
        addSettingRow(readerLayout, R.string.read_mode, getString(R.string.single_session), true);
        addSettingRow(readerLayout, R.string.rf_power, "29 dBm", true);
        addSettingRow(readerLayout, R.string.antenna, getString(R.string.four_port), true);

        LinearLayout appLayout = view.findViewById(R.id.layoutAppSettings);
        addSettingRow(appLayout, R.string.sound, getString(R.string.on), true);
        addSettingRow(appLayout, R.string.vibration, getString(R.string.on), true);

        MaterialButton btnSave = view.findViewById(R.id.btnSaveSettings);
        btnSave.setOnClickListener(v ->
                Toast.makeText(requireContext(), R.string.save_settings, Toast.LENGTH_SHORT).show());

        MaterialButton btnLogout = view.findViewById(R.id.btnLogout);
        btnLogout.setOnClickListener(v -> logout());
    }

    private void logout() {
        Global.clearSession();
        Intent intent = new Intent(requireContext(), LoginActivity.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
        requireActivity().finish();
    }

    private void addSettingRow(LinearLayout parent, int labelRes, String value, boolean showChevron) {
        View row = LayoutInflater.from(requireContext())
                .inflate(R.layout.item_settings_row, parent, false);
        TextView label = row.findViewById(R.id.tvSettingLabel);
        TextView val = row.findViewById(R.id.tvSettingValue);
        TextView chevron = row.findViewById(R.id.tvChevron);
        if (label != null) label.setText(labelRes);
        if (val != null) val.setText(value);
        if (chevron != null) chevron.setVisibility(showChevron ? View.VISIBLE : View.GONE);
        parent.addView(row);
    }
}
