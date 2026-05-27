package com.laundry.r3reader.ui.main;

import android.os.Bundle;

import androidx.appcompat.app.AppCompatActivity;
import androidx.navigation.NavController;
import androidx.navigation.NavGraph;
import androidx.navigation.fragment.NavHostFragment;
import androidx.navigation.ui.NavigationUI;

import com.google.android.material.bottomnavigation.BottomNavigationView;
import com.laundry.r3reader.Global;
import com.laundry.r3reader.R;
import com.laundry.r3reader.api.ApiCallback;
import com.laundry.r3reader.api.model.BootstrapResponse;

public class MainActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        if (Global.isReaderConnected() && Global.hasReaderId()) {
            Global.getRepository().fetchBootstrap(new ApiCallback<BootstrapResponse>() {
                @Override
                public void onSuccess(BootstrapResponse data) {
                }

                @Override
                public void onError(String message) {
                }
            });
        }

        NavHostFragment navHost = (NavHostFragment) getSupportFragmentManager()
                .findFragmentById(R.id.nav_host_fragment);
        BottomNavigationView bottomNav = findViewById(R.id.bottomNav);
        if (navHost == null) {
            return;
        }

        NavController navController = navHost.getNavController();
        NavGraph graph = navController.getNavInflater().inflate(R.navigation.nav_graph);
        graph.setStartDestination(Global.isReaderSessionReady()
                ? R.id.workflowFragment
                : R.id.connectFragment);
        navController.setGraph(graph);

        bottomNav.setOnItemSelectedListener(item -> {
            if (item.getItemId() == R.id.workflowFragment && !Global.isReaderSessionReady()) {
                navController.navigate(R.id.connectFragment);
                return true;
            }
            return NavigationUI.onNavDestinationSelected(item, navController);
        });

        navController.addOnDestinationChangedListener((controller, destination, arguments) -> {
            int id = destination.getId();
            boolean topLevel = id == R.id.workflowFragment
                    || id == R.id.moreFragment;
            bottomNav.setVisibility(topLevel ? android.view.View.VISIBLE : android.view.View.GONE);
        });

        bottomNav.setOnItemReselectedListener(item -> {
        });
    }
}
