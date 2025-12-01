using Autodesk.Revit.UI;
using ricaun.Revit.UI;
using System.IO;

namespace Su.AutoCAD2Revit.Test
{
    internal class App : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            var panel = application.CreateRibbonPanel(Tab.AddIns, "Su.AutoCAD2Revit");
            panel.CreatePushButton<TestCommand>("Test Command");
            return Result.Succeeded;
        }
    }
}
