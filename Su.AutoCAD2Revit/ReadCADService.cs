using Autodesk.Revit.DB;
using Su.AutoCAD2Revit.Extension;
using System.IO;
using Teigha.Runtime;
using Entity = Teigha.DatabaseServices.Entity;
using Exception = System.Exception;
using Path = System.IO.Path;

namespace Su.AutoCAD2Revit
{
    /// <summary>
    /// AutoCAD2Revit图纸识别服务对象
    /// </summary>
    public class ReadCADService : IDisposable
    {
        //图纸element
        private string cacheDwgFile;//缓存图纸的路径
        private Transform importInstanceTransform = Transform.Identity;//图纸的transform
        private double levelHeight = 0;//图纸的绝对标高z
        private Services service;
        private Database database;
        private BlockTable table;
        private BlockTableRecord record;
        private readonly FileOpenMode fileOpenMode;
        private readonly string blockTableRecord;
        private readonly bool allowCPConversion;
        private readonly string password;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="importInstance">链接的AutoCAD图纸</param>
        /// <param name="levelHeightZ">图纸所在的绝对标高z</param>
        /// <param name="fileOpenMode"></param>
        /// <param name="blockTableRecord"></param>
        /// <param name="allowCPConversion"></param>
        /// <param name="password"></param>
        public ReadCADService(ImportInstance importInstance, double levelHeightZ, FileOpenMode fileOpenMode = FileOpenMode.OpenForReadAndWriteNoShare, string blockTableRecord = "*MODEL_SPACE", bool allowCPConversion = true, string password = "") : this(fileOpenMode, blockTableRecord, allowCPConversion, password)
        {
            cacheDwgFile = Directory.GetParent(GetType().Assembly.Location).FullName + $"\\{Path.GetFileName(importInstance.GetCADPath())}";
            SmartCopyFile(importInstance.GetCADPath(), cacheDwgFile);
            importInstanceTransform = importInstance.GetTransform();
            levelHeight = levelHeightZ;
            Init();
        }

        private void SmartCopyFile(string sourceFile, string cacheDwgFile)
        {
            // 使用 FileStream + FileShare.ReadWrite 方式读取被占用文件
            const int bufferSize = 1024 * 1024; // 1MB 缓冲
            byte[] buffer = new byte[bufferSize];

            try
            {
                using var source = new FileStream(
                    sourceFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete); // ⭐ 允许其他程序继续占用
                using var dest = new FileStream(
                    cacheDwgFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    dest.Write(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"文件复制失败！{ex.Message}");
            }
        }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dwgfile">DWG文件绝对路径（包含文件拓展名）</param>
        /// <param name="levelHeightZ">图纸所在的绝对标高z</param>
        /// <param name="placement">图纸对齐方式</param>
        /// <param name="fileOpenMode"></param>
        /// <param name="blockTableRecord"></param>
        /// <param name="allowCPConversion"></param>
        /// <param name="password"></param>
        public ReadCADService(string dwgfile, double levelHeightZ, ImportPlacement placement = ImportPlacement.Origin, FileOpenMode fileOpenMode = FileOpenMode.OpenForReadAndWriteNoShare, string blockTableRecord = "*MODEL_SPACE", bool allowCPConversion = true, string password = "") : this(fileOpenMode, blockTableRecord, allowCPConversion, password)
        {
            cacheDwgFile = Directory.GetParent(GetType().Assembly.Location).FullName + $"\\{Path.GetFileName(dwgfile)}";
            SmartCopyFile(dwgfile, cacheDwgFile);
            levelHeight = levelHeightZ;
            Init();
        }

        private ReadCADService(FileOpenMode fileOpenMode, string blockTableRecord, bool allowCPConversion, string password)
        {
            this.fileOpenMode = fileOpenMode;
            this.blockTableRecord = blockTableRecord;
            this.allowCPConversion = allowCPConversion;
            this.password = password;
        }

        private ReadCADService()
        {

        }

        private void Init()
        {
            this.service = new Services();
            this.database = new Database(false, false);
            try
            {
                database.ReadDwgFile(cacheDwgFile, fileOpenMode, allowCPConversion, password);
            }
            catch
            {
                throw new Exception($"图纸读取失败！可能是您的图纸版本高于AutoCAD2013，或您的图纸进行了特殊加密，或您链接的图纸失去链接！请检查后重试");
            }
            try
            {
                database.ExportBlocks(blockTableRecord);
            }
            catch (Exception ex)
            {
            }
            this.table = (BlockTable)database.BlockTableId.GetObject(OpenMode.ForWrite);
            this.record = (BlockTableRecord)table[blockTableRecord].GetObject(OpenMode.ForWrite);
        }

        private void DeleteCacheFile()
        {
            try
            {
                File.Delete(cacheDwgFile);
            }
            catch
            {

            }
        }


        /// <summary>
        /// 取得该图纸中的所有文字信息
        /// </summary>
        /// <returns>所有文字信息</returns>
        public List<CADTextModel> GetAllTexts()
        {
            List<CADTextModel> listCADModels = [];
            foreach (ObjectId id in record)
            {
                using Entity entity = (Entity)id.GetObject(OpenMode.ForRead, false, false);
                switch (entity.GetType().Name)
                {
                    case nameof(DBText):
                        DBText text = (DBText)entity;
                        var dbLocation = text.Position.ToRevitPoint().Transform(importInstanceTransform).SetZ(levelHeight);
                        var dbCenter = text.GeometricExtents.Center().ToRevitPoint().Transform(importInstanceTransform).SetZ(levelHeight);
                        CADTextModel model = new(dbLocation, dbCenter, text.TextString, text.Layer, text.Rotation, text.BlockName);
                        listCADModels.Add(model);
                        break;

                    case nameof(MText):
                        MText mText = (MText)entity;
                        var mtLocation = mText.Location.ToRevitPoint().Transform(importInstanceTransform).SetZ(levelHeight);
                        var mtCenter = mText.GeometricExtents.Center().ToRevitPoint().Transform(importInstanceTransform).SetZ(levelHeight);
                        CADTextModel acDbMTextModel = new(mtLocation, mtCenter, mText.Text, mText.Layer, mText.Rotation, mText.BlockName);
                        listCADModels.Add(acDbMTextModel);
                        break;
                }
            }
            return listCADModels;
        }

        public void Dispose()
        {
            try
            {
                this.record.Dispose();
                this.table.Dispose();
                this.database.Dispose();
                this.service.Dispose();
                this.importInstanceTransform.Dispose();
            }
            catch (Exception ex)
            {

            }
            DeleteCacheFile();
        }
    }
}