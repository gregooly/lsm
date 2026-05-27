package com.laundry.r3reader.ui.connect;

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
import com.laundry.r3reader.api.model.BootstrapResponse;
import com.laundry.r3reader.ble.BleDiscoveredDevice;
import com.laundry.r3reader.ble.BlePermissionHelper;
import com.laundry.r3reader.ble.UhfBleManager;
import com.laundry.r3reader.ui.base.BaseFragment;
import com.laundry.r3reader.ui.base.NavControllerSafe;
import com.rscja.deviceapi.interfaces.ConnectionStatus;

public class ConnectFragment extends BaseFragment {

    private MaterialButton btnConnectAction;
    private RecyclerView rvDevices;
    private BleDeviceAdapter deviceAdapter;
    private UhfBleManager ble;

    private boolean connecting;
    @Nullable
    private BleDiscoveredDevice selectedDevice;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_connect, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        ble = UhfBleManager.getInstance();
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, Global.isReaderConnected());

        bindStat(view.findViewById(R.id.statFirmware), R.string.firmware, "1.2.5");
        bindStat(view.findViewById(R.id.statAntPower), R.string.ant_power, "29 dBm");
        bindStat(view.findViewById(R.id.statAntenna), R.string.antenna, getString(R.string.four_port));

        rvDevices = view.findViewById(R.id.rvDevices);
        rvDevices.setLayoutManager(new LinearLayoutManager(requireContext()));
        deviceAdapter = new BleDeviceAdapter(this::onDeviceSelected);
        rvDevices.setAdapter(deviceAdapter);

        if (ble.isBleConnected()) {
            deviceAdapter.setSelectedAddress(ble.getConnectedAddress());
        }

        btnConnectAction = view.findViewById(R.id.btnDisconnect);
        updateConnectButton();
        btnConnectAction.setOnClickListener(v -> {
            if (connecting) {
                return;
            }
            if (Global.isReaderConnected() || ble.isBleConnected()) {
                Global.clearReaderConnection();
                selectedDevice = null;
                deviceAdapter.setSelectedAddress(null);
                updateConnectButton();
                setConnectUiBusy(false);
                Toast.makeText(requireContext(), R.string.disconnected, Toast.LENGTH_SHORT).show();
            } else if (selectedDevice != null) {
                connectBleThenBackend(selectedDevice);
            } else {
                Toast.makeText(requireContext(), R.string.select_ble_device, Toast.LENGTH_SHORT).show();
            }
        });

        view.findViewById(R.id.btnRefreshScan).setOnClickListener(v -> startBleScan());

        ble.setConnectionListener(this::onBleConnectionChanged);

        if (BlePermissionHelper.hasBlePermissions(requireActivity())) {
            startBleScan();
        } else {
            BlePermissionHelper.requestBlePermissions(requireActivity());
        }
    }

    @Override
    public void onDestroyView() {
        ble.stopScan();
        ble.setConnectionListener(null);
        super.onDestroyView();
    }

    private void startBleScan() {
        if (!BlePermissionHelper.hasBlePermissions(requireActivity())) {
            BlePermissionHelper.requestBlePermissions(requireActivity());
            return;
        }
        deviceAdapter.setDevices(ble.getDiscoveredDevices());
        ble.setScanListener(new UhfBleManager.ScanListener() {
            @Override
            public void onDeviceFound(@NonNull BleDiscoveredDevice device) {
                if (!isAdded()) return;
                deviceAdapter.addDevice(device);
            }

            @Override
            public void onScanFinished() {
                if (!isAdded()) return;
                Toast.makeText(requireContext(), R.string.scan_finished, Toast.LENGTH_SHORT).show();
            }
        });
        ble.startScan(null);
        Toast.makeText(requireContext(), R.string.scanning, Toast.LENGTH_SHORT).show();
    }

    private void onDeviceSelected(@NonNull BleDiscoveredDevice device) {
        selectedDevice = device;
        deviceAdapter.setSelectedAddress(device.address);
        connectBleThenBackend(device);
    }

    private void connectBleThenBackend(@NonNull BleDiscoveredDevice device) {
        if (Global.getCustomerId() <= 0) {
            Toast.makeText(requireContext(), R.string.login_required, Toast.LENGTH_LONG).show();
            return;
        }
        if (connecting) {
            return;
        }
        connecting = true;
        setConnectUiBusy(true);
        Global.setHandheldId(device.getHandheldId());
        ble.connect(device.address);
    }

    private void onBleConnectionChanged(@NonNull ConnectionStatus status,
                                        @Nullable String name,
                                        @Nullable String address) {
        if (!isAdded()) return;
        if (status == ConnectionStatus.CONNECTED) {
            String handheldId = ble.getConnectedHandheldId();
            if (handheldId.isEmpty() && selectedDevice != null) {
                handheldId = selectedDevice.getHandheldId();
            }
            Global.setHandheldId(handheldId);
            registerWithBackend(handheldId);
        } else if (status == ConnectionStatus.DISCONNECTED && !connecting) {
            updateConnectButton();
            setConnectUiBusy(false);
        }
    }

    private void registerWithBackend(@NonNull String handheldId) {
        Global.getRepository().connectReaderAndBootstrap(handheldId,
                new ApiCallback<BootstrapResponse>() {
                    @Override
                    public void onSuccess(BootstrapResponse bootstrap) {
                        if (!isAdded()) return;
                        connecting = false;
                        setConnectUiBusy(false);
                        updateConnectButton();
                        if (ble.getConnectedAddress() != null) {
                            deviceAdapter.setSelectedAddress(ble.getConnectedAddress());
                        }
                        Toast.makeText(requireContext(), R.string.reader_connected, Toast.LENGTH_SHORT).show();
                        goWorkflow();
                    }

                    @Override
                    public void onError(String message) {
                        if (!isAdded()) return;
                        connecting = false;
                        Global.clearReaderConnection();
                        ble.disconnect();
                        setConnectUiBusy(false);
                        updateConnectButton();
                        Toast.makeText(requireContext(),
                                message != null ? message : getString(R.string.connection_failed),
                                Toast.LENGTH_LONG).show();
                    }
                });
    }

    private void updateConnectButton() {
        if (btnConnectAction == null) return;
        if (connecting) {
            btnConnectAction.setText(R.string.connecting);
        } else {
            btnConnectAction.setText(Global.isReaderSessionReady() || ble.isBleConnected()
                    ? R.string.disconnect : R.string.connect);
        }
    }

    private void setConnectUiBusy(boolean busy) {
        connecting = busy;
        if (btnConnectAction != null) {
            btnConnectAction.setEnabled(!busy || Global.isReaderConnected());
        }
        if (rvDevices != null) {
            rvDevices.setEnabled(!busy);
        }
        updateConnectButton();
    }

    private void goWorkflow() {
        NavControllerSafe.navigate(ConnectFragment.this, R.id.action_connect_to_workflow);
    }

    private void bindStat(View statRoot, int labelRes, String value) {
        if (statRoot == null) return;
        TextView label = statRoot.findViewById(R.id.tvStatLabel);
        TextView val = statRoot.findViewById(R.id.tvStatValue);
        if (label != null) label.setText(labelRes);
        if (val != null) val.setText(value);
    }
}
