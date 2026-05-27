package com.laundry.r3reader.ui.sync;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
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
import com.laundry.r3reader.api.model.MovementBatchRequest;
import com.laundry.r3reader.api.model.MovementBatchResponse;
import com.laundry.r3reader.ui.base.BaseFragment;

import java.util.ArrayList;
import java.util.List;

public class SyncFragment extends BaseFragment {

    private SyncQueueAdapter pendingAdapter;
    private SyncQueueAdapter failedAdapter;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_sync, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, true);

        RecyclerView rvPending = view.findViewById(R.id.rvPending);
        RecyclerView rvFailed = view.findViewById(R.id.rvFailed);
        rvPending.setLayoutManager(new LinearLayoutManager(requireContext()));
        rvFailed.setLayoutManager(new LinearLayoutManager(requireContext()));

        reloadQueues(rvPending, rvFailed);

        MaterialButton btnSync = view.findViewById(R.id.btnSyncNow);
        btnSync.setOnClickListener(v -> syncAllPending(btnSync, rvPending, rvFailed));
    }

    private void reloadQueues(RecyclerView rvPending, RecyclerView rvFailed) {
        pendingAdapter = new SyncQueueAdapter(
                new ArrayList<>(Global.getPendingMovements()), false);
        failedAdapter = new SyncQueueAdapter(
                new ArrayList<>(Global.getFailedMovements()), true);
        rvPending.setAdapter(pendingAdapter);
        rvFailed.setAdapter(failedAdapter);
    }

    private void syncAllPending(MaterialButton btnSync, RecyclerView rvPending, RecyclerView rvFailed) {
        List<MovementBatchRequest> pending = new ArrayList<>(Global.getPendingMovements());
        if (pending.isEmpty()) {
            Toast.makeText(requireContext(), R.string.sync_nothing_pending, Toast.LENGTH_SHORT).show();
            return;
        }
        btnSync.setEnabled(false);
        syncNext(pending, 0, btnSync, rvPending, rvFailed);
    }

    private void syncNext(@NonNull List<MovementBatchRequest> queue, int index,
                          MaterialButton btnSync, RecyclerView rvPending, RecyclerView rvFailed) {
        if (index >= queue.size()) {
            btnSync.setEnabled(true);
            Toast.makeText(requireContext(), R.string.sync_complete, Toast.LENGTH_SHORT).show();
            reloadQueues(rvPending, rvFailed);
            return;
        }
        MovementBatchRequest batch = queue.get(index);
        Global.getRepository().postMovementEvents(batch, true, new ApiCallback<MovementBatchResponse>() {
            @Override
            public void onSuccess(MovementBatchResponse data) {
                Global.removePendingMovementAt(0);
                syncNext(queue, index + 1, btnSync, rvPending, rvFailed);
            }

            @Override
            public void onError(String message) {
                Global.removePendingMovementAt(0);
                Global.enqueueFailedMovement(batch);
                syncNext(queue, index + 1, btnSync, rvPending, rvFailed);
            }
        });
    }
}
