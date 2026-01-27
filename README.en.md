 [English](README.en.md) | [简体中文](README.md)

![Revit Support](https://img.shields.io/badge/Revit-2013~2024-blue)
![AutoCAD DWG](https://img.shields.io/badge/AutoCAD%20DWG-2013%20and%20below-green)
![License](https://img.shields.io/badge/license-MIT-lightgrey)

# Su.AutoCAD2Revit Documentation

**GitHub:** https://github.com/ViewSuSu/Su.AutoCAD2Revit  
**Gitee:** https://gitee.com/SususuChang/su.-auto-cad2-revit

---

## 📘 Overview
Su.AutoCAD2Revit is a Revit add-in extension library based on **Teigha**,  
used for **reading DWG files without installing or opening AutoCAD** (including Revit-linked CAD drawings and local DWG files).

---

## ⭐ Core Features

### 🔄 Automatic Coordinate Conversion
- Automatically and precisely converts AutoCAD coordinates to Revit coordinates
- Handles the Transform of Revit ImportInstance
- Automatically applies Revit level elevation

### 🧩 Smart Block Processing
- Parses AutoCAD blocks and nested block structures
- Automatically overlays block reference transformation matrices
- Maintains relative positions and rotation information of all elements within blocks

### ✏ Text Extraction Capabilities
- Extracts text content
- Supports text angle, layer, position, belonging block and other attributes
- All coordinates are automatically converted to Revit world coordinates

---

## 📚 Core Class Description

### `AutoCADReader`
Used to read DWG files or Revit-linked CAD drawings.

#### Constructors
```csharp
// Create from Revit-linked drawing (ImportInstance)
var cadService = new AutoCADReader(importInstance, levelHeightZ);

// Create from DWG file
var cadService = new AutoCADReader(dwgFilePath, levelHeightZ);
```

---

### `CADTextModel`

DWG text data model. All converted fields are output in Revit coordinates.

| Property       | Description                                    |
| -------------- | ---------------------------------------------- |
| `Location`     | Converted Revit world coordinates of text insertion point |
| `Center`       | Converted Revit world coordinates of text bounding box center |
| `Text`         | Text content                                   |
| `Layer`        | Layer name                                     |
| `Angle`        | Text rotation angle                            |
| `BlockName`    | Belonging block name (if exists)               |

---

## 🚀 Basic Usage Examples

### 1️⃣ Reading Text from Revit-Linked Drawings

```csharp
// Automatically handles coordinate conversion & block coordinate transformation
using (var cadReader = new AutoCADReader(cadLink, level.Elevation))
{
    List<CADTextModel> texts = cadReader.GetAllTexts();

    foreach (var text in texts)
    {
        Console.WriteLine($"Text: {text.Text}, Location: {text.Location}");
    }
}
```

---

### 2️⃣ Reading Local DWG Files

```csharp
using (var cadReader = new AutoCADReader(dwgPath, baseElevation))
{
    var texts = cadReader.GetAllTexts();
    // All coordinates have been converted to Revit world coordinates
}
```

---

## 📐 Coordinate Conversion Explanation

This library internally performs the complete coordinate transformation chain, including:

* AutoCAD world coordinates (mm) → Revit world coordinates (feet)
* Overall Transform of Revit ImportInstance
* Overlay of Block reference matrices within DWG
* Application of Revit level elevation
* Processing of recursive transformation chains for nested blocks

You don't need to manually calculate any Transform. This library outputs the final Revit world coordinates.

---

## ⚠ Important Notes

* **Must use `using` statement or call `Dispose()` to release resources**
* Currently only supports AutoCAD 2013 and below DWG files
* All output coordinates have been automatically converted for direct use in Revit API
* For commercial use, ensure compliance with ODA (Open Design Alliance) licensing requirements

---
