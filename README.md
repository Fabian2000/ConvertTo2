# ConvertTo2 - Video, Image, and Audio Converter

ConvertTo2 is a file conversion utility that leverages FFmpeg and FFprobe for video, image, and audio conversions. Ensure that both FFmpeg and FFprobe are in the same folder as the `ConvertTo2.exe` for the program to function correctly.

## Installation

To add ConvertTo2 to the file context menu for easy access:

- Click on the `Install` option in the menu bar.
- ConvertTo2 will then be added to the right-click context menu under the name `ConvertTo2`.

To remove ConvertTo2 from the context menu:

- Click on the `Uninstall` option.
- This will reverse the installation and remove ConvertTo2 from the context menu.

## Usage

### General Workflow
1. **Add Input File**: You can add a file either by using the context menu (right-click on a file and choose `ConvertTo2`) or through `File > Open` in the menu bar.
2. **Select Output File**: In the Settings tab (gear icon), choose a location for the output file (the destination file after the conversion). You can also set the number of threads for the conversion process here.
3. **Automatic Tab Selection**: The program automatically displays only the relevant tabs based on the type of input file (video, image, or audio).
4. **Configure Settings**: Adjust the desired resolution, bitrate, codec, and other parameters in the respective tabs.
5. **Start Conversion**: After configuring your settings, press the `Start` button in the Action tab (play icon) to begin the conversion. You can manually stop the conversion if needed.
6. **Completion**: Once the conversion is done, the `Done` button will become clickable, indicating that the process is complete.

### Image Conversion
- Set the desired resolution in the Image tab.

### Video Conversion
- Set resolution, FPS, bitrate, and codec.
- Optionally, trim the video using the `Cut Start` and `Cut End` time fields.

### Audio Conversion
- Adjust the codec, bitrate, sample rate, and audio channels.
- Optionally, trim the audio using the `Cut Start` and `Cut End` time fields.

## Action Tab
- The `Action` tab is only visible once you have selected a file to convert.
- Once all settings are configured, click `Start` to begin the conversion process.
- The `Done` button will become active once the conversion completes.

## Requirements
- FFmpeg and FFprobe must be in the same folder as the `ConvertTo2.exe`.