package com.laundry.r3reader.ui.login;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;

import com.google.android.material.button.MaterialButton;
import com.google.android.material.textfield.TextInputEditText;
import com.laundry.r3reader.Global;
import com.laundry.r3reader.R;
import com.laundry.r3reader.api.ApiCallback;
import com.laundry.r3reader.api.model.TableLoginResponse;
import com.laundry.r3reader.ui.main.MainActivity;

public class LoginActivity extends AppCompatActivity {

    private String formattedDeviceId;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (Global.isLoggedIn()) {
            goMain();
            return;
        }
        setContentView(R.layout.activity_login);

        formattedDeviceId = Global.getDeviceIdFormatted();

        TextInputEditText etDeviceId = findViewById(R.id.etDeviceId);
        if (etDeviceId != null) {
            etDeviceId.setText(formattedDeviceId);
            etDeviceId.setKeyListener(null);
        }

        android.widget.TextView tvVersion = findViewById(R.id.tvVersion);
        if (tvVersion != null) {
            tvVersion.setText(getString(R.string.version_format, getString(R.string.app_version_label)));
        }

        MaterialButton btnLogin = findViewById(R.id.btnLogin);
        btnLogin.setOnClickListener(v -> performLogin(btnLogin));
    }

    private void performLogin(MaterialButton btnLogin) {
        btnLogin.setEnabled(false);
        Global.getRepository().tableLogin(formattedDeviceId, new ApiCallback<TableLoginResponse>() {
            @Override
            public void onSuccess(TableLoginResponse data) {
                Global.saveDeviceIdFormatted(formattedDeviceId);
                Global.setReaderConnected(false);
                goMain();
            }

            @Override
            public void onError(String message) {
                btnLogin.setEnabled(true);
                Toast.makeText(LoginActivity.this,
                        message != null ? message : getString(R.string.login_failed),
                        Toast.LENGTH_LONG).show();
            }
        });
    }

    private void goMain() {
        startActivity(new Intent(this, MainActivity.class));
        finish();
    }
}
