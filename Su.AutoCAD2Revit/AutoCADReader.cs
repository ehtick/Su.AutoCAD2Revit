using System.IO;
using Autodesk.Revit.DB;
using Su.AutoCAD2Revit.Extension;
using Teigha.Runtime;
using Entity = Teigha.DatabaseServices.Entity;
using Exception = System.Exception;
using Path = System.IO.Path;

namespace Su.AutoCAD2Revit
{
    /// <summary>
    ///  This class reads a CAD drawing file (DWG) and extracts the text data from it.
    /// </summary>
    public class AutoCADReader : IDisposable
    {
        private string cacheDwgFile;
        private Transform importInstanceTransform = Transform.Identity;
        private double levelHeight = 0;
        private Services service;
        private Database database;
        private BlockTable table;
        private BlockTableRecord record;

        private readonly FileOpenMode fileOpenMode;
        private readonly string blockTableRecord;
        private readonly bool allowCPConversion;
        private readonly string password;

        /// <summary>
        ///  Initializes a new instance of the <see cref="AutoCADReader"/> class.
        /// </summary>
        /// <param name="importInstance"> The Linked ImportInstance object.</param>
        /// <param name="levelHeightZ"> The height of the level to which the imported CAD objects will be placed.</param>
        /// <param name="fileOpenMode"> The file open mode for the DWG file.</param>
        /// <param name="blockTableRecord"> The name of the block table record to import into.</param>
        /// <param name="allowCPConversion"> Whether to allow the conversion of CAD color palette (CP) to Revit color palette (ACI).</param>
        /// <param name="password"></param>
        /// <exception cref="Exception"> Thrown when the ImportInstance is not a CAD link imported into Revit.</exception>
        public AutoCADReader(
            ImportInstance importInstance,
            double levelHeightZ,
            FileOpenMode fileOpenMode = FileOpenMode.OpenForReadAndWriteNoShare,
            string blockTableRecord = "*MODEL_SPACE",
            bool allowCPConversion = true,
            string password = ""
        )
            : this(fileOpenMode, blockTableRecord, allowCPConversion, password)
        {
            string sourcePath = null;
            try
            {
                sourcePath = importInstance.GetCADPath();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Failed to obtain the local file path from the ImportInstance! Please make sure the ImportInstance is a CAD link imported into Revit"
                );
            }
            cacheDwgFile = GetTempCachePath(sourcePath);

            SmartCopyFile(sourcePath, cacheDwgFile);

            importInstanceTransform = importInstance.GetTransform();
            levelHeight = levelHeightZ;
            Init();
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="AutoCADReader"/> class.
        /// </summary>
        /// <param name="dwgfile"> The path of the DWG file to import.</param>
        /// <param name="levelHeightZ"> The height of the level to which the imported CAD objects will be placed.</param>
        /// <param name="placement"> The placement of the imported CAD objects in the Revit model.</param>
        /// <param name="fileOpenMode"> The file open mode for the DWG file.</param>
        /// <param name="blockTableRecord"> The name of the block table record to import into.</param>
        /// <param name="allowCPConversion"> Whether to allow the conversion of CAD color palette (CP) to Revit color palette (ACI).</param>
        /// <param name="password"> The password for the DWG file.</param>
        public AutoCADReader(
            string dwgfile,
            double levelHeightZ,
            ImportPlacement placement = ImportPlacement.Origin,
            FileOpenMode fileOpenMode = FileOpenMode.OpenForReadAndWriteNoShare,
            string blockTableRecord = "*MODEL_SPACE",
            bool allowCPConversion = true,
            string password = ""
        )
            : this(fileOpenMode, blockTableRecord, allowCPConversion, password)
        {
            cacheDwgFile = GetTempCachePath(dwgfile);
            SmartCopyFile(dwgfile, cacheDwgFile);

            levelHeight = levelHeightZ;
            Init();
        }

        private AutoCADReader(
            FileOpenMode fileOpenMode,
            string blockTableRecord,
            bool allowCPConversion,
            string password
        )
        {
            this.fileOpenMode = fileOpenMode;
            this.blockTableRecord = blockTableRecord;
            this.allowCPConversion = allowCPConversion;
            this.password = password;
        }

        /// <summary>
        /// 生成系统 TEMP 下的缓存路径
        /// </summary>
        private string GetTempCachePath(string sourceFile)
        {
            string tempDir = Path.GetTempPath();
            return Path.Combine(tempDir, Path.GetFileName(sourceFile));
        }

        /// <summary>
        /// 能读取被占用文件的复制方法
        /// </summary>
        private void SmartCopyFile(string sourceFile, string cacheFile)
        {
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];

            try
            {
                using var source = new FileStream(
                    sourceFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete
                );

                using var dest = new FileStream(
                    cacheFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );

                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    dest.Write(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                throw new Exception($"文件复制失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 初始化 Teigha 数据库
        /// </summary>
        private void Init()
        {
            service = new Services();
            database = new Database(false, false);

            try
            {
                database.ReadDwgFile(cacheDwgFile, fileOpenMode, allowCPConversion, password);
            }
            catch
            {
                throw new Exception(
                    "图纸读取失败！可能 DWG 版本过高、被加密或者路径失效，请检查后重试。"
                );
            }

            try
            {
                database.ExportBlocks(blockTableRecord);
            }
            catch
            {
                // 允许忽略
            }

            table = (BlockTable)database.BlockTableId.GetObject(OpenMode.ForWrite);
            record = (BlockTableRecord)table[blockTableRecord].GetObject(OpenMode.ForWrite);
        }

        private void DeleteCacheFile()
        {
            try
            {
                if (File.Exists(cacheDwgFile))
                    File.Delete(cacheDwgFile);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        /// <summary>
        /// Gets all the text data from the DWG file.
        /// </summary>
        public List<CADTextModel> GetAllTexts()
        {
            List<CADTextModel> result = new();

            foreach (ObjectId id in record)
            {
                using Entity entity = id.GetObject(OpenMode.ForRead) as Entity;
                if (entity == null)
                    continue;

                switch (entity)
                {
                    case DBText text:
                        AddDbText(result, text);
                        break;

                    case MText mText:
                        AddMText(result, mText);
                        break;
                }
            }

            return result;
        }

        private void AddDbText(List<CADTextModel> list, DBText text)
        {
            XYZ dbLocation = text
                .Position.ToRevitPoint()
                .Transform(importInstanceTransform)
                .SetZ(levelHeight);

            XYZ dbCenter = text
                .GeometricExtents.Center()
                .ToRevitPoint()
                .Transform(importInstanceTransform)
                .SetZ(levelHeight);

            list.Add(
                new CADTextModel(
                    dbLocation,
                    dbCenter,
                    text.TextString,
                    text.Layer,
                    text.Rotation,
                    text.BlockName
                )
            );
        }

        private void AddMText(List<CADTextModel> list, MText mText)
        {
            XYZ location = mText
                .Location.ToRevitPoint()
                .Transform(importInstanceTransform)
                .SetZ(levelHeight);

            XYZ center = mText
                .GeometricExtents.Center()
                .ToRevitPoint()
                .Transform(importInstanceTransform)
                .SetZ(levelHeight);

            list.Add(
                new CADTextModel(
                    location,
                    center,
                    mText.Text,
                    mText.Layer,
                    mText.Rotation,
                    mText.BlockName
                )
            );
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="AutoCADReader"/> and optionally releases the managed resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                record?.Dispose();
                table?.Dispose();
                database?.Dispose();
                service?.Dispose();
                importInstanceTransform?.Dispose();
            }
            catch { }

            DeleteCacheFile();
        }
    }
}
