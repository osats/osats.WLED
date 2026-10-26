# Changelog

## 2.0.0

- Portuguese configuration panel with colour picker, HEX preview, brightness, priorities, blink intervals and timed per-effect tests.
- Configurable green-flag/test duration, RPM range and four strip directions.
- Versioned XML profiles without the sender's IP; import preserves local connection and strip layout.
- Migrate existing RGB/enabled settings into eight configurable effects, including a separate shift alert.
- Fix repeated green-flag triggering, low-speed suppression of alerts, odd strip lengths and centre calculations with offsets.
- Validate settings, save on Apply, synchronise updates and dispose sockets.
- Bound UDP frequency, keepalive and recovery from send errors.
- Build/package script and deterministic regression tests with loopback UDP integration.

Physical WLED and in-game validation remain pending. Current UDP format supports LED indices 0–255.
