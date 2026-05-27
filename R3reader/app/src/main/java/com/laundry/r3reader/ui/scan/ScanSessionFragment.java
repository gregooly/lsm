package com.laundry.r3reader.ui.scan;

import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.google.android.material.appbar.MaterialToolbar;
import com.google.android.material.button.MaterialButton;
import com.laundry.r3reader.Global;
import com.laundry.r3reader.R;
import com.laundry.r3reader.api.ApiCallback;
import com.laundry.r3reader.ble.UhfBleManager;
import com.laundry.r3reader.data.ReaderWayInfo;
import com.laundry.r3reader.data.ScannedLinenItem;
import com.laundry.r3reader.data.WorkflowType;
import com.laundry.r3reader.ui.base.BaseFragment;
import com.laundry.r3reader.ui.base.NavControllerSafe;
import com.rscja.deviceapi.interfaces.ConnectionStatus;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class ScanSessionFragment extends BaseFragment {

    private final Handler handler = new Handler(Looper.getMainLooper());
    private final Map<String, ScannedLinenItem> scannedByEpc = new LinkedHashMap<>();

    private UhfBleManager ble;
    private boolean scanning = true;
    private int uniqueTags;
    private int totalReads;
    private int duplicates;
    private String sessionId = "";

    private TextView tvUniqueCount;
    private TextView tvTotalReads;
    private TextView tvDuplicates;
    private TextView tvLastEpc;
    private View pulseRing;
    private MaterialButton btnPause;
    private android.animation.ObjectAnimator pulseAnimator;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_scan_session, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        ble = UhfBleManager.getInstance();

        if (!Global.isReaderSessionReady() || !ble.isBleConnected()) {
            Toast.makeText(requireContext(), R.string.reader_not_connected, Toast.LENGTH_LONG).show();
            NavControllerSafe.navigate(this, R.id.workflowFragment);
            return;
        }

        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, true);

        sessionId = Global.newSessionId();
        TextView tvSessionId = view.findViewById(R.id.tvSessionId);
        if (tvSessionId != null) {
            tvSessionId.setText(getString(R.string.session_id_format, sessionId));
        }

        tvUniqueCount = view.findViewById(R.id.tvUniqueCount);
        tvTotalReads = view.findViewById(R.id.tvTotalReads);
        tvDuplicates = view.findViewById(R.id.tvDuplicates);
        tvLastEpc = view.findViewById(R.id.tvLastEpc);
        pulseRing = view.findViewById(R.id.pulseRing);
        btnPause = view.findViewById(R.id.btnPause);
        MaterialButton btnStop = view.findViewById(R.id.btnStop);

        scannedByEpc.clear();
        uniqueTags = 0;
        totalReads = 0;
        duplicates = 0;
        startPulse();

        btnPause.setOnClickListener(v -> togglePause());
        btnStop.setOnClickListener(v -> finishScanAndShowResults());

        ble.setTagReadListener(this::onTagFromReader);
        ble.setConnectionListener((status, name, address) -> {
            if (!isAdded()) return;
            if (status == ConnectionStatus.DISCONNECTED) {
                stopScanning();
                Toast.makeText(requireContext(), R.string.reader_disconnected_scan, Toast.LENGTH_LONG).show();
                NavControllerSafe.navigate(ScanSessionFragment.this, R.id.connectFragment);
            }
        });

        if (!ble.startInventory()) {
            Toast.makeText(requireContext(), R.string.scan_start_failed, Toast.LENGTH_LONG).show();
        }
    }

    private void onTagFromReader(@NonNull String epc) {
        if (!scanning || !isAdded()) {
            return;
        }
        totalReads++;
        if (scannedByEpc.containsKey(epc)) {
            duplicates++;
            updateUi(epc);
            return;
        }
        int index = scannedByEpc.size();
        lookupTag(epc, index);
    }

    private void lookupTag(@NonNull String epc, int index) {
        Global.getRepository().lookupByTag(epc, new ApiCallback<ScannedLinenItem>() {
            @Override
            public void onSuccess(ScannedLinenItem data) {
                if (!isAdded()) return;
                scannedByEpc.put(epc, data);
                uniqueTags = scannedByEpc.size();
                updateUi(epc);
            }

            @Override
            public void onError(String message) {
                if (!isAdded()) return;
                scannedByEpc.put(epc, com.laundry.r3reader.data.MockData.lookupAndClassify(epc, index));
                uniqueTags = scannedByEpc.size();
                updateUi(epc);
            }
        });
    }

    private void finishScanAndShowResults() {
        stopScanning();
        if (scannedByEpc.isEmpty()) {
            Toast.makeText(requireContext(), R.string.no_tags_scanned, Toast.LENGTH_SHORT).show();
            return;
        }

        List<ScannedLinenItem> classified = new ArrayList<>(scannedByEpc.values());
        int wayId = Global.getActiveReaderWayId();
        String wayTitle;
        String movement;
        String target;

        ReaderWayInfo way = Global.findReaderWayById(wayId);
        if (way != null) {
            wayTitle = way.wayName;
            movement = way.movementLabel;
            target = way.targetProcessStatus;
        } else {
            WorkflowType fallback = Global.getActiveWorkflow();
            wayTitle = getString(fallback.titleRes);
            movement = getString(fallback.movementRes);
            target = fallback.targetProcessStatus;
        }

        Global.setLastScanSession(sessionId, wayId, wayTitle, movement, target, classified);
        NavControllerSafe.navigate(this, WorkflowType.POST_SCAN_ACTION);
    }

    private void togglePause() {
        scanning = !scanning;
        if (scanning) {
            btnPause.setText(R.string.pause);
            startPulse();
            ble.startInventory();
        } else {
            btnPause.setText(R.string.resume);
            stopPulse();
            ble.stopInventory();
        }
    }

    private void stopScanning() {
        scanning = false;
        stopPulse();
        ble.stopInventory();
        ble.setTagReadListener(null);
        ble.setConnectionListener(null);
    }

    private void updateUi(String lastEpc) {
        if (tvUniqueCount != null) tvUniqueCount.setText(String.valueOf(uniqueTags));
        if (tvTotalReads != null) tvTotalReads.setText(String.valueOf(totalReads));
        if (tvDuplicates != null) tvDuplicates.setText(String.valueOf(duplicates));
        if (tvLastEpc != null) tvLastEpc.setText(lastEpc);
    }

    private void startPulse() {
        if (pulseRing == null) return;
        Animation anim = AnimationUtils.loadAnimation(requireContext(), android.R.anim.fade_in);
        pulseRing.startAnimation(anim);
        pulseAnimator = android.animation.ObjectAnimator.ofFloat(pulseRing, View.ALPHA, 0.4f, 1f);
        pulseAnimator.setDuration(800);
        pulseAnimator.setRepeatCount(android.animation.ObjectAnimator.INFINITE);
        pulseAnimator.setRepeatMode(android.animation.ObjectAnimator.REVERSE);
        pulseAnimator.start();
    }

    private void stopPulse() {
        if (pulseAnimator != null) {
            pulseAnimator.cancel();
            pulseAnimator = null;
        }
        if (pulseRing != null) {
            pulseRing.clearAnimation();
            pulseRing.setAlpha(1f);
        }
    }

    @Override
    public void onDestroyView() {
        stopScanning();
        super.onDestroyView();
    }
}
