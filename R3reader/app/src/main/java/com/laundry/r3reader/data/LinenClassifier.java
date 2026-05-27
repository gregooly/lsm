package com.laundry.r3reader.data;

import androidx.annotation.NonNull;

/**
 * Automatic classification from RFID scan + lookup (mock rules until API is wired).
 */
public final class LinenClassifier {

    private LinenClassifier() {
    }

    /**
     * @param knownInTenant false when tag lookup returns not found
     * @param physicalCondition from API item, e.g. good / damaged
     */
    @NonNull
    public static LinenCategory classify(@NonNull String epc, boolean knownInTenant,
                                         @NonNull String physicalCondition) {
        if (!knownInTenant) {
            return LinenCategory.LOST;
        }
        if ("damaged".equalsIgnoreCase(physicalCondition)
                || "lost".equalsIgnoreCase(physicalCondition)) {
            return "lost".equalsIgnoreCase(physicalCondition)
                    ? LinenCategory.LOST : LinenCategory.DAMAGED;
        }
        int hash = Math.abs(epc.hashCode());
        if (hash % 17 == 0) {
            return LinenCategory.LOST;
        }
        if (hash % 9 == 0) {
            return LinenCategory.DAMAGED;
        }
        return LinenCategory.NORMAL;
    }
}
