# Changelog

## Unreleased

- Created a clean archival release from the original Kinect4FOG research source.
- Removed generated build outputs, checked-in package binaries and legacy Visual Studio source-control metadata.
- Standardised filenames, class names, method names and spelling.
- Replaced selected opaque identifiers with descriptive names.
- Added authorship, copyright and publication DOI notices.
- Aligned repository and source headers with the Apache-2.0 licence selected for the release.
- Removed the broken prototype Excel/Office path and retained the working text-based research data recorder.
- Corrected shared serial-controller wiring and shoulder-joint mapping used for body orientation.
- Corrected FOG status reset behaviour and UDP command reception lifecycle.
- Added repository hygiene, citation and build documentation.
- Preserved the published research thresholds and calculations; no intentional methodological changes.
- Added a cleaned Arduino controller component with documented servo pins, 9600-baud protocol and research attribution.
- Added a cleaned Windows remote-monitoring client as a self-contained WPF project.
- Corrected the Windows client to decode JPEG monitoring frames emitted by the main application rather than treating them as raw BGRA pixels.
- Replaced the Windows client's prototype receive loop and `Thread.Abort` lifecycle with controlled UDP shutdown and resource disposal.
- Documented the historical WebClient repository as an incomplete stub and FallDetectHeuristic as related, non-runtime research code.
