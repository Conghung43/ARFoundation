# Depth Data NPY File Saving

## Overview
The `CpuImageSample` component now includes functionality to save depth data to NumPy `.npy` files. This is useful for offline processing, analysis, and computer vision applications.

## Features
- **NpyWriter Utility**: A helper class that handles writing arrays to NumPy-compatible `.npy` format files
- **Multi-format Support**: Supports saving depth data in multiple formats:
  - Float32 (32-bit floating point)
  - UInt16 (16-bit unsigned integer)
  - UInt8 (8-bit unsigned integer)
- **Automatic Directory Creation**: Creates output directories if they don't exist
- **Frame Numbering**: Automatically tracks and numbers saved frames

## Usage

### 1. Enable Depth Data Saving
In the Inspector, configure the `CpuImageSample` component:
- **Save Depth Data**: Toggle this checkbox to enable/disable saving
- **Depth Data Output Path**: Set the directory where depth files will be saved

Example path: `/Users/username/Documents/depth_data/`

### 2. Runtime Control
You can also enable/disable saving and set the path programmatically:

```csharp
CpuImageSample sample = GetComponent<CpuImageSample>();
sample.saveDepthData = true;
sample.depthDataOutputPath = Application.persistentDataPath + "/depth_data/";
```

### 3. Output Format
Depth data is saved as NumPy `.npy` files with the following naming convention:
```
{DepthType}_{FrameNumber}_{Timestamp}.npy
```

Examples:
- `HumanDepth_000000_2024-11-26_14-30-45-123.npy`
- `EnvironmentDepth_000001_2024-11-26_14-30-46-456.npy`
- `HumanStencil_000000_2024-11-26_14-30-45-789.npy`

### 4. Loading Data in Python
You can easily load and process the saved depth data using NumPy:

```python
import numpy as np
from PIL import Image

# Load depth data
depth_data = np.load('HumanDepth_000000_2024-11-26_14-30-45-123.npy')

# Check shape
print(f"Depth map shape: {depth_data.shape}")
print(f"Data type: {depth_data.dtype}")

# Display as image (normalize for visualization)
if depth_data.dtype == np.float32:
    normalized = ((depth_data - depth_data.min()) / (depth_data.max() - depth_data.min()) * 255).astype(np.uint8)
else:
    normalized = depth_data

img = Image.fromarray(normalized)
img.save('depth_visualization.png')
```

## Supported Depth Types
The system automatically saves the following depth data types:
- **HumanDepth**: Depth map for detected humans
- **HumanStencil**: Segmentation mask for detected humans
- **EnvironmentDepth**: Depth map of the environment
- **EnvironmentDepthConfidence**: Confidence values for environment depth

## Notes
- Depth data is saved on every frame where the depth image is successfully acquired
- The frame counter increments only when data is successfully saved
- Check Unity's console for log messages confirming successful saves or errors
- The saved `.npy` files are compatible with NumPy and can be opened in any Python environment with NumPy installed
- For large amounts of data, consider compressing or downsampling to manage file sizes

## Troubleshooting
- **Files not saving**: Ensure the output path has write permissions
- **Empty files**: Check that depth images are being acquired (visible in the UI)
- **Memory issues**: Consider sampling every N frames instead of every frame
