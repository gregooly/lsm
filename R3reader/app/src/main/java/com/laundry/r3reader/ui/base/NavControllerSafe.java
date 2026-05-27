package com.laundry.r3reader.ui.base;

import androidx.annotation.IdRes;
import androidx.annotation.NonNull;
import androidx.fragment.app.Fragment;
import androidx.navigation.NavController;
import androidx.navigation.fragment.NavHostFragment;

public final class NavControllerSafe {

    private NavControllerSafe() {
    }

    public static void navigate(@NonNull Fragment fragment, @IdRes int destinationId) {
        try {
            NavController nav = NavHostFragment.findNavController(fragment);
            if (nav.getCurrentDestination() != null
                    && nav.getCurrentDestination().getId() == destinationId) {
                return;
            }
            nav.navigate(destinationId);
        } catch (IllegalStateException | IllegalArgumentException ignored) {
            // Invalid navigation graph state — avoid crash in demo builds
        }
    }
}
