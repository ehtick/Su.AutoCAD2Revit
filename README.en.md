![Revit Support](https://img.shields.io/badge/Revit-2013~2024-blue)
![AutoCAD DWG](https://img.shields.io/badge/AutoCAD%20DWG-2013%20and%20below-green)
![License](https://img.shields.io/badge/license-MIT-lightgrey)

# Su.AutoCAD2Revit User Documentation

**GitHub:** https://github.com/ViewSuSu/Su.AutoCAD2Revit  
**Gitee:** https://gitee.com/SususuChang/su.-auto-cad2-revit

---

## 📘 Overview
Su.AutoCAD2Revit is a Revit plugin extension library based on **Teigha**,  
used for **reading DWG files without installing or opening AutoCAD** (including Revit-linked CAD drawings and local DWG files).

---

## ⭐ Core Features

### 🔄 Automatic Coordinate Conversion
- Automatically converts AutoCAD coordinate system to Revit coordinate system with precision  
- Handles Revit ImportInstance's own Transform  
- Automatically applies Revit elevation

### 🧩 Smart Block Processing
- Parses AutoCAD blocks and nested block structures  
- Automatically overlays block reference transformation matrices  
- Maintains relative positions and rotation information of all elements within blocks

### ✏ Text Extraction Capability
- Extracts text content  
- Supports text angle, layer, position, belonging block and other attributes  
- All coordinates are automatically converted to Revit world coordinates

---

## 📚 Core Class Description

### `ReadCADService`
Used for reading DWG files or Revit-linked CAD drawings.

#### Constructors
```csharp
// Create from Revit-linked drawing (ImportInstance)
var cadService = new ReadCADService(importInstance, levelHeight);

// Create from DWG file
var cadService = new ReadCADService(dwgFilePath, levelHeight);
```

---

### `CADTextModel`

DWG text data model, all converted fields are output in Revit coordinates.

| Property      | Description |
| ----------- | --------------- |
| `Location`  | Converted Revit world coordinates |
| `Text`      | Text content |
| `Layer`     | Layer name |
| `Angle`     | Text rotation angle |
| `BlockName` | Belonging block name (if exists) |

---

## 🚀 Basic Usage Examples

### 1️⃣ Reading Text from Revit-Linked Drawings

```csharp
// Automatically handles coordinate conversion & block coordinate transformation
using (var cadService = new ReadCADService(cadLink, level.Elevation))
{
    List<CADTextModel> texts = cadService.GetAllTexts();

    foreach (var text in texts)
    {
        Console.WriteLine($"Text: {text.Text}, Location: {text.Location}");
    }
}
```

---

### 2️⃣ Reading Local DWG Files

```csharp
using (var cadService = new ReadCADService(dwgPath, baseElevation))
{
    var texts = cadService.GetAllTexts();
    // All coordinates have been converted to Revit world coordinates
}
```

---

## 📐 Coordinate Conversion Description

This library automatically performs complete coordinate transformation chain internally, including:

* AutoCAD world coordinates (millimeters) → Revit world coordinates (feet)
* Overall Transform of Revit ImportInstance
* Overlay of DWG internal Block reference matrices
* Application of Revit elevation
* Processing recursive transformation chain of nested blocks

You don't need to manually calculate any Transform, this library outputs the final Revit world coordinates.

---

## ⚠ Important Notes

* Recommended to use `using` to release resources
* Currently only supports AutoCAD 2013 and below DWG format
* All output coordinates have been automatically converted and can be directly used in Revit API
* If used for commercial purposes, please ensure compliance with ODA (Open Design Alliance) related licensing agreements

---
