package com.laundry.r3reader.ui.workflow;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.content.ContextCompat;

import com.google.android.material.appbar.MaterialToolbar;
import com.google.android.material.card.MaterialCardView;
import com.laundry.r3reader.Global;
import com.laundry.r3reader.R;
import com.laundry.r3reader.api.ApiCallback;
import com.laundry.r3reader.ble.UhfBleManager;
import com.laundry.r3reader.data.BootstrapStore;
import com.laundry.r3reader.api.model.BootstrapResponse;
import com.laundry.r3reader.data.ReaderWayInfo;
import com.laundry.r3reader.data.WorkflowType;
import com.laundry.r3reader.ui.base.BaseFragment;
import com.laundry.r3reader.ui.base.NavControllerSafe;

import java.util.List;

public class WorkflowFragment extends BaseFragment {

    private int selectedWayId;

    private static final int[] CARD_IDS = {
            R.id.cardSorting, R.id.cardShipment, R.id.cardQuarantine, R.id.cardReturn
    };
    private static final int[] ACCENT_COLORS = {
            R.color.workflow_sorting,
            R.color.workflow_shipment,
            R.color.workflow_quarantine,
            R.color.workflow_return
    };
    private static final int[] BG_COLORS = {
            R.color.light_success,
            R.color.light_blue_bg,
            R.color.light_warning,
            R.color.light_blue_bg
    };
    private static final int[] ICON_IDS = {
            R.drawable.ic_rfid_tag,
            R.drawable.ic_shipment,
            R.drawable.ic_warning_outline,
            R.drawable.ic_assignment
    };
    private static final int[] ICON_BGS = {
            R.drawable.bg_icon_success,
            R.drawable.bg_icon_blue,
            R.drawable.bg_icon_warning,
            R.drawable.bg_icon_blue
    };

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_workflow, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        selectedWayId = Global.getActiveReaderWayId();
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, false);

        bindActiveReaderFooter(view);

        if (Global.hasReaderWays()) {
            bindApiWays(view, Global.getReaderWays());
        } else if (Global.hasReaderId()) {
            refreshBootstrapThenBind(view);
        } else {
            bindFallbackCards(view);
        }
    }

    private void bindActiveReaderFooter(@NonNull View view) {
        TextView tvReader = view.findViewById(R.id.tvActiveReader);
        TextView tvLocation = view.findViewById(R.id.tvActiveLocation);
        UhfBleManager ble = UhfBleManager.getInstance();
        if (tvReader != null) {
            String name = ble.getConnectedHandheldId();
            if (name.isEmpty() && BootstrapStore.getReader() != null
                    && BootstrapStore.getReader().readerName != null) {
                name = BootstrapStore.getReader().readerName;
            }
            tvReader.setText(name.isEmpty() ? "—" : name);
        }
        if (tvLocation != null) {
            tvLocation.setText(getString(R.string.location_clean_area));
        }
    }

    private void refreshBootstrapThenBind(@NonNull View view) {
        Global.getRepository().fetchBootstrap(new ApiCallback<BootstrapResponse>() {
            @Override
            public void onSuccess(BootstrapResponse data) {
                if (!isAdded()) return;
                if (Global.hasReaderWays()) {
                    bindApiWays(view, Global.getReaderWays());
                } else {
                    bindFallbackCards(view);
                }
            }

            @Override
            public void onError(String message) {
                if (!isAdded()) return;
                bindFallbackCards(view);
                Toast.makeText(requireContext(), R.string.bootstrap_failed, Toast.LENGTH_LONG).show();
            }
        });
    }

    private void bindApiWays(@NonNull View view, @NonNull List<ReaderWayInfo> ways) {
        for (int i = 0; i < CARD_IDS.length; i++) {
            View cardRoot = view.findViewById(CARD_IDS[i]);
            if (cardRoot == null) continue;
            if (i >= ways.size()) {
                cardRoot.setVisibility(View.GONE);
                continue;
            }
            cardRoot.setVisibility(View.VISIBLE);
            ReaderWayInfo way = ways.get(i);
            int accent = ContextCompat.getColor(requireContext(), ACCENT_COLORS[i % ACCENT_COLORS.length]);
            setupWorkflowCard(cardRoot, way, accent, BG_COLORS[i % BG_COLORS.length],
                    ICON_IDS[i % ICON_IDS.length], ICON_BGS[i % ICON_BGS.length],
                    way.id == selectedWayId);
        }
    }

    private void bindFallbackCards(@NonNull View view) {
        WorkflowType[] types = WorkflowType.values();
        for (int i = 0; i < CARD_IDS.length; i++) {
            View cardRoot = view.findViewById(CARD_IDS[i]);
            if (cardRoot == null || i >= types.length) continue;
            cardRoot.setVisibility(View.VISIBLE);
            WorkflowType type = types[i];
            int accent = ContextCompat.getColor(requireContext(), ACCENT_COLORS[i]);
            ReaderWayInfo pseudo = new ReaderWayInfo(
                    i + 1,
                    getString(type.titleRes),
                    getString(type.movementRes),
                    type.targetProcessStatus);
            setupWorkflowCard(cardRoot, pseudo, accent, BG_COLORS[i],
                    ICON_IDS[i], ICON_BGS[i],
                    Global.getActiveWorkflow() == type);
        }
    }

    private void setupWorkflowCard(@NonNull View cardRoot, @NonNull ReaderWayInfo way,
                                   int accent, int bgColorRes, int iconId, int iconBgId,
                                   boolean selected) {
        MaterialCardView card = asWorkflowCard(cardRoot);
        if (card == null) return;

        TextView tvTitle = cardRoot.findViewById(R.id.tvWorkflowTitle);
        TextView tvDesc = cardRoot.findViewById(R.id.tvWorkflowDesc);
        TextView tvTarget = cardRoot.findViewById(R.id.tvWorkflowTarget);
        View iconFrame = cardRoot.findViewById(R.id.iconFrame);
        ImageView ivIcon = cardRoot.findViewById(R.id.ivWorkflowIcon);
        ImageView ivSelected = cardRoot.findViewById(R.id.ivSelected);

        if (tvTitle != null) {
            tvTitle.setText(way.wayName);
            tvTitle.setTextColor(accent);
        }
        if (tvDesc != null) tvDesc.setText(way.movementLabel);
        if (tvTarget != null) {
            tvTarget.setText(getString(R.string.target_status_label, way.targetProcessStatus));
            tvTarget.setTextColor(accent);
        }
        if (iconFrame != null) iconFrame.setBackgroundResource(iconBgId);
        if (ivIcon != null) ivIcon.setImageResource(iconId);
        updateSelection(card, ivSelected, selected, accent, bgColorRes);

        card.setOnClickListener(v -> {
            if (!Global.isReaderSessionReady()) {
                Toast.makeText(requireContext(), R.string.reader_not_connected, Toast.LENGTH_LONG).show();
                NavControllerSafe.navigate(this, R.id.connectFragment);
                return;
            }
            selectedWayId = way.id;
            Global.setActiveReaderWayId(way.id);
            Global.setActiveWorkflow(WorkflowType.fromIndex(
                    Math.min(way.id - 1, WorkflowType.values().length - 1)));
            refreshSelection();
            NavControllerSafe.navigate(this, R.id.action_workflow_to_scan);
        });
    }

    private void refreshSelection() {
        View root = getView();
        if (root == null) return;
        if (Global.hasReaderWays()) {
            bindApiWays(root, Global.getReaderWays());
        } else {
            bindFallbackCards(root);
        }
    }

    @Nullable
    private static MaterialCardView asWorkflowCard(View cardRoot) {
        if (cardRoot instanceof MaterialCardView) {
            return (MaterialCardView) cardRoot;
        }
        View inner = cardRoot.findViewById(R.id.workflowCard);
        if (inner instanceof MaterialCardView) {
            return (MaterialCardView) inner;
        }
        return null;
    }

    private void updateSelection(MaterialCardView card, @Nullable ImageView iv, boolean selected,
                                 int accent, int bgColorRes) {
        if (selected) {
            card.setStrokeColor(accent);
            card.setStrokeWidth(4);
            card.setCardBackgroundColor(ContextCompat.getColor(requireContext(), bgColorRes));
            if (iv != null) iv.setVisibility(View.VISIBLE);
        } else {
            card.setStrokeWidth(0);
            card.setCardBackgroundColor(ContextCompat.getColor(requireContext(), R.color.card_background));
            if (iv != null) iv.setVisibility(View.GONE);
        }
    }
}
