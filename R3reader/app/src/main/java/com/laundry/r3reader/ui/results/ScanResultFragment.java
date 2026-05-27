package com.laundry.r3reader.ui.results;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.appbar.MaterialToolbar;
import com.google.android.material.button.MaterialButton;
import com.laundry.r3reader.Global;
import com.laundry.r3reader.R;
import com.laundry.r3reader.api.ApiCallback;
import com.laundry.r3reader.api.ApiMapper;
import com.laundry.r3reader.api.model.MovementBatchRequest;
import com.laundry.r3reader.api.model.MovementBatchResponse;
import com.laundry.r3reader.data.LinenCategory;
import com.laundry.r3reader.data.ScannedLinenItem;
import com.laundry.r3reader.ui.base.BaseFragment;
import com.laundry.r3reader.ui.base.NavControllerSafe;

import java.util.ArrayList;
import java.util.List;

public class ScanResultFragment extends BaseFragment {

    private final List<ScannedLinenItem> items = new ArrayList<>();

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_scan_results, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, true);

        items.clear();
        items.addAll(Global.getScannedItems());

        TextView tvSessionId = view.findViewById(R.id.tvSessionId);
        TextView tvReaderWay = view.findViewById(R.id.tvReaderWay);
        TextView tvReaderMovement = view.findViewById(R.id.tvReaderMovement);
        TextView tvCountNormal = view.findViewById(R.id.tvCountNormal);
        TextView tvCountDamaged = view.findViewById(R.id.tvCountDamaged);
        TextView tvCountLost = view.findViewById(R.id.tvCountLost);

        if (tvSessionId != null) {
            tvSessionId.setText(getString(R.string.session_id_format, Global.getScanSessionId()));
        }
        if (tvReaderWay != null) {
            tvReaderWay.setText(Global.getScanReaderWayTitle());
        }
        if (tvReaderMovement != null) {
            tvReaderMovement.setText(getString(R.string.target_status_label,
                    Global.getScanReaderWayTarget()));
        }

        if (tvCountNormal != null) {
            tvCountNormal.setText(String.valueOf(Global.countScanned(LinenCategory.NORMAL)));
        }
        if (tvCountDamaged != null) {
            tvCountDamaged.setText(String.valueOf(Global.countScanned(LinenCategory.DAMAGED)));
        }
        if (tvCountLost != null) {
            tvCountLost.setText(String.valueOf(Global.countScanned(LinenCategory.LOST)));
        }

        RecyclerView rv = view.findViewById(R.id.rvResults);
        rv.setLayoutManager(new LinearLayoutManager(requireContext()));
        rv.setAdapter(new ScanResultAdapter(items));

        MaterialButton btnTransmit = view.findViewById(R.id.btnTransmit);
        btnTransmit.setOnClickListener(v -> transmitSession(btnTransmit));

        MaterialButton btnScanAgain = view.findViewById(R.id.btnScanAgain);
        btnScanAgain.setOnClickListener(v ->
                NavControllerSafe.navigate(this, R.id.action_results_to_workflow));
    }

    private void transmitSession(MaterialButton btnTransmit) {
        if (items.isEmpty()) {
            Toast.makeText(requireContext(), R.string.no_tags_scanned, Toast.LENGTH_SHORT).show();
            return;
        }
        int customerId = Global.getCustomerId();
        int readerId = Global.getReaderId();
        int readerWayId = Global.getScanReaderWayId();
        if (customerId == 0 || readerId == 0 || readerWayId == 0) {
            Toast.makeText(requireContext(), R.string.transmit_missing_config, Toast.LENGTH_LONG).show();
            return;
        }

        btnTransmit.setEnabled(false);
        MovementBatchRequest request = ApiMapper.toMovementBatch(
                customerId, readerId, readerWayId, items);

        Global.getRepository().postMovementEvents(request, false, new ApiCallback<MovementBatchResponse>() {
            @Override
            public void onSuccess(MovementBatchResponse data) {
                if (!isAdded()) return;
                btnTransmit.setEnabled(true);
                int accepted = countAccepted(data);
                Toast.makeText(requireContext(),
                        getString(R.string.transmit_success_count, accepted, items.size()),
                        Toast.LENGTH_LONG).show();
            }

            @Override
            public void onError(String message) {
                if (!isAdded()) return;
                btnTransmit.setEnabled(true);
                Global.enqueuePendingMovement(request);
                Toast.makeText(requireContext(),
                        getString(R.string.transmit_queued, message != null ? message : ""),
                        Toast.LENGTH_LONG).show();
            }
        });
    }

    private int countAccepted(@NonNull MovementBatchResponse data) {
        if (data.results == null) return items.size();
        int n = 0;
        for (com.laundry.r3reader.api.model.MovementResultDto r : data.results) {
            if ("accepted".equalsIgnoreCase(r.status) || "duplicate".equalsIgnoreCase(r.status)) {
                n++;
            }
        }
        return n;
    }
}
