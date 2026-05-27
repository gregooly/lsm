package com.example.driverapp;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

public final class TimeUtils {

    private static final String UTC_PATTERN = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private TimeUtils() {
    }

    public static String utcNowIso() {
        SimpleDateFormat sdf = new SimpleDateFormat(UTC_PATTERN, Locale.US);
        sdf.setTimeZone(TimeZone.getTimeZone("UTC"));
        return sdf.format(new Date());
    }
}
