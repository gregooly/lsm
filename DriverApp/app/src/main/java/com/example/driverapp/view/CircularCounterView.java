package com.example.driverapp.view;

import android.animation.ValueAnimator;
import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.RectF;
import android.graphics.Shader;
import android.graphics.Typeface;
import android.util.AttributeSet;
import android.view.View;
import android.view.animation.AccelerateDecelerateInterpolator;

public class CircularCounterView extends View {

    private Paint trackPaint;
    private Paint progressPaint;
    private Paint countTextPaint;
    private Paint labelTextPaint;

    private RectF arcRect;
    private float strokeWidth = 0f;
    private int count = 0;
    private float sweepAngle = 270f; // Default progress arc (out of 360)
    private float maxCount = 200f;

    private static final float START_ANGLE = 135f;
    private static final float MAX_SWEEP = 270f;
    private static final int COLOR_START = 0xFF22C55E;
    private static final int COLOR_END = 0xFF1976D2;
    private static final int TRACK_COLOR = 0xFFE5E7EB;
    private static final int COUNT_TEXT_COLOR = 0xFF111827;
    private static final int LABEL_TEXT_COLOR = 0xFF687280;

    public CircularCounterView(Context context) {
        super(context);
        init();
    }

    public CircularCounterView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public CircularCounterView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }

    private void init() {
        trackPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        trackPaint.setStyle(Paint.Style.STROKE);
        trackPaint.setStrokeCap(Paint.Cap.ROUND);
        trackPaint.setColor(TRACK_COLOR);

        progressPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        progressPaint.setStyle(Paint.Style.STROKE);
        progressPaint.setStrokeCap(Paint.Cap.ROUND);

        countTextPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        countTextPaint.setTextAlign(Paint.Align.CENTER);
        countTextPaint.setColor(COUNT_TEXT_COLOR);
        countTextPaint.setTypeface(Typeface.create(Typeface.DEFAULT, Typeface.BOLD));

        labelTextPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        labelTextPaint.setTextAlign(Paint.Align.CENTER);
        labelTextPaint.setColor(LABEL_TEXT_COLOR);
        labelTextPaint.setTypeface(Typeface.DEFAULT);
    }

    @Override
    protected void onSizeChanged(int w, int h, int oldw, int oldh) {
        super.onSizeChanged(w, h, oldw, oldh);
        float minDim = Math.min(w, h);
        strokeWidth = minDim * 0.055f;

        trackPaint.setStrokeWidth(strokeWidth);
        progressPaint.setStrokeWidth(strokeWidth);

        float inset = strokeWidth / 2f + 4f;
        arcRect = new RectF(inset, inset, w - inset, h - inset);

        float centerX = w / 2f;
        float centerY = h / 2f;
        LinearGradient gradient = new LinearGradient(
                centerX - minDim / 2f, centerY,
                centerX + minDim / 2f, centerY,
                COLOR_START, COLOR_END,
                Shader.TileMode.CLAMP
        );
        progressPaint.setShader(gradient);

        countTextPaint.setTextSize(minDim * 0.26f);
        labelTextPaint.setTextSize(minDim * 0.09f);
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);
        if (arcRect == null) return;

        // Draw track
        canvas.drawArc(arcRect, START_ANGLE, MAX_SWEEP, false, trackPaint);

        // Draw progress arc
        float progress = maxCount > 0 ? Math.min(count / maxCount, 1f) : 0f;
        float currentSweep = MAX_SWEEP * progress;
        if (currentSweep > 0) {
            canvas.drawArc(arcRect, START_ANGLE, currentSweep, false, progressPaint);
        }

        float cx = getWidth() / 2f;
        float cy = getHeight() / 2f;

        // Draw count number
        Paint.FontMetrics fm = countTextPaint.getFontMetrics();
        float countY = cy - (fm.ascent + fm.descent) / 2f - labelTextPaint.getTextSize() * 0.4f;
        canvas.drawText(String.valueOf(count), cx, countY, countTextPaint);

        // Draw "Items Scanned" label
        float labelY = countY + (-fm.ascent) * 0.15f + labelTextPaint.getTextSize() * 1.4f;
        canvas.drawText("Items Scanned", cx, labelY, labelTextPaint);
    }

    public void setCount(int count) {
        this.count = count;
        invalidate();
    }

    public int getCount() {
        return count;
    }

    public void incrementCount() {
        this.count++;
        invalidate();
    }

    public void setMaxCount(float maxCount) {
        this.maxCount = maxCount;
        invalidate();
    }

    public void reset() {
        this.count = 0;
        invalidate();
    }

    public void animateTo(int targetCount) {
        ValueAnimator animator = ValueAnimator.ofInt(count, targetCount);
        animator.setDuration(800);
        animator.setInterpolator(new AccelerateDecelerateInterpolator());
        animator.addUpdateListener(animation -> {
            count = (int) animation.getAnimatedValue();
            invalidate();
        });
        animator.start();
    }
}
