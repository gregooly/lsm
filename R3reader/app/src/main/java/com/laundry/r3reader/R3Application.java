package com.laundry.r3reader;

import android.app.Application;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatDelegate;

import com.laundry.r3reader.data.PrefsManager;

public class R3Application extends Application {

    private static R3Application instance;

    @Override
    public void onCreate() {
        super.onCreate();
        instance = this;
        if (PrefsManager.isDarkMode(this)) {
            AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES);
        }
        Global.init();
    }

    @NonNull
    public static R3Application getInstance() {
        return instance;
    }
}
