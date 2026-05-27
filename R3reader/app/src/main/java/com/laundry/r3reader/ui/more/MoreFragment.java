package com.laundry.r3reader.ui.more;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.google.android.material.appbar.MaterialToolbar;
import com.google.android.material.card.MaterialCardView;
import com.laundry.r3reader.R;
import com.laundry.r3reader.ui.base.BaseFragment;
import com.laundry.r3reader.ui.base.NavControllerSafe;

public class MoreFragment extends BaseFragment {

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_more, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        MaterialToolbar toolbar = view.findViewById(R.id.toolbar);
        setupToolbar(toolbar, false);

        bindNav(view.findViewById(R.id.menuConnect), R.id.action_more_to_connect);
        bindNav(view.findViewById(R.id.menuSync), R.id.action_more_to_sync);
        bindNav(view.findViewById(R.id.menuSettings), R.id.action_more_to_settings);
    }

    private void bindNav(MaterialCardView card, int actionId) {
        if (card == null) return;
        card.setOnClickListener(v -> NavControllerSafe.navigate(this, actionId));
    }
}
