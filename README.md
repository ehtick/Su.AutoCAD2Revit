 [English](README.en.md) | [简体中文](README.md)

![Revit Support](https://img.shields.io/badge/Revit-2013~2024-blue)
![AutoCAD DWG](https://img.shields.io/badge/AutoCAD%20DWG-2013%20及以下-green)
![License](https://img.shields.io/badge/license-MIT-lightgrey)

# Su.AutoCAD2Revit 使用文档

**GitHub:** https://github.com/ViewSuSu/Su.AutoCAD2Revit  
**Gitee:** https://gitee.com/SususuChang/su.-auto-cad2-revit  
**NuGet:** https://www.nuget.org/packages/Su.AutoCAD2Revit/

---

## 📘 概述
Su.AutoCAD2Revit 是一个基于 **Teigha** 的 Revit 插件扩展库，  
用于在 **无需安装或无需打开 AutoCAD 的情况下读取 DWG 文件**（包含 Revit 链接的 CAD 图纸与本地 DWG）。
---

## ⭐ 核心特性

### 🔄 自动坐标转换
- 自动将 AutoCAD 坐标系精确转换到 Revit 坐标系  
- 处理 Revit ImportInstance 自身的 Transform  
- 自动应用 Revit 标高（Elevation）

### 🧩 智能块处理
- 解析 AutoCAD 块（Block）与嵌套块结构  
- 自动叠加块参照的变换矩阵  
- 保持块内所有元素相对位置、旋转信息

### ✏ 文本提取能力
- 提取文字内容  
- 支持文本角度、图层、位置、所属块等属性  
- 坐标均已自动转换为 Revit 世界坐标

---

## 📦 NuGet 安装

### 包管理器控制台
```powershell
Install-Package Su.AutoCAD2Revit
```

### .NET CLI
```bash
dotnet add package Su.AutoCAD2Revit
```

### 包引用
```xml
<PackageReference Include="Su.AutoCAD2Revit" Version="2013.1.0.0" />
```

---

## 📚 核心类说明

### `AutoCADReader`
用于读取 DWG 文件或 Revit 链接 CAD 图纸。

#### 构造函数
```csharp
// 从 Revit 链接图纸创建（ImportInstance）
var cadService = new AutoCADReader(importInstance, levelHeightZ);

// 从 DWG 文件创建
var cadService = new AutoCADReader(dwgFilePath, levelHeightZ);
```

---

### `CADTextModel`

DWG 文本数据模型，转换后的字段全部以 Revit 坐标输出。

| 属性          | 说明                  |
| ----------- | ------------------- |
| `Location`  | 文本插入点转换后的 Revit 世界坐标 |
| `Center`    | 文本包围盒中心转换后的 Revit 世界坐标 |
| `Text`      | 文本内容                |
| `Layer`     | 图层名称                |
| `Angle`     | 文本旋转角度              |
| `BlockName` | 所属块名称（如存在）          |

---

## 🚀 基础用法示例

### 1️⃣ 读取 Revit 链接图纸中的文本

```csharp
// 自动处理坐标转换 & 块坐标变换
using (var cadReader = new AutoCADReader(cadLink, level.Elevation))
{
    List<CADTextModel> texts = cadReader.GetAllTexts();

    foreach (var text in texts)
    {
        Console.WriteLine($"文字: {text.Text}, 位置: {text.Location}");
    }
}
```

---

### 2️⃣ 读取本地 DWG 文件

```csharp
using (var cadReader = new AutoCADReader(dwgPath, baseElevation))
{
    var texts = cadReader.GetAllTexts();
    // 所有坐标已转换为 Revit 世界坐标
}
```

---

## 📐 坐标转换说明

本库内部会自动执行完整的坐标变换链路，包括：

* AutoCAD 世界坐标（毫米） → Revit 世界坐标（英尺）
* Revit ImportInstance 的整体 Transform
* DWG 内部的 Block 参照矩阵叠加
* Revit 标高 Elevation 应用
* 处理嵌套块的递归变换链路

你无需手动计算任何 Transform，本库会输出最终的 Revit 世界坐标。

---

## ⚠ 注意事项

* **必须使用 `using` 语句或调用 `Dispose()` 释放资源**
* 当前仅支持 AutoCAD 2013 及以下 DWG
* 输出的所有坐标均已自动转换为可直接用于 Revit API 的坐标
* 若用于商业目的，请确保遵循 ODA（Open Design Alliance）的相关授权许可

```
