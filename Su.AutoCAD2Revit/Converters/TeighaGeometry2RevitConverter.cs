using Su.AutoCAD2Revit.Extension;

namespace Su.AutoCAD2Revit.Converters
{
    internal static class TeighaGeometry2RevitConverter
    {
        public static Autodesk.Revit.DB.Curve ToRevitCurve(this Curve curve)
        {
            switch (curve.GetType().Name)
            {
                case nameof(Line):
                    var line = curve as Line;
                    return Autodesk.Revit.DB.Line.CreateBound(line.StartPoint.ToRevitPoint(), line.EndPoint.ToRevitPoint());
                case nameof(Arc):
                    var arc = curve as Arc;
                    return Autodesk.Revit.DB.Arc.Create(arc.StartPoint.ToRevitPoint(), arc.EndPoint.ToRevitPoint(), arc.GetPointAtDist(arc.Length / 2).ToRevitPoint());
                case nameof(Circle):
                    {
                        var circle = curve as Circle;
                        using var plane = circle.GetPlane();
                        var normal = plane.Normal;
                        using var revitPlane = Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(normal.ToRevitVector(), circle.Center.ToRevitPoint());
                        return Autodesk.Revit.DB.Arc.Create(revitPlane, circle.Radius, 0, 2 * Math.PI);
                    }
                case nameof(Ellipse):
                    {
                        var ellipse = curve as Ellipse;
                        using var plane = ellipse.GetPlane();
                        var normal = plane.Normal;
                        using var revitPlane = Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(normal.ToRevitVector(), ellipse.Center.ToRevitPoint());
                        return Autodesk.Revit.DB.Ellipse.CreateCurve(ellipse.Center.ToRevitPoint(), ellipse.MinorRadius, ellipse.MajorRadius, ellipse.MinorAxis.ToRevitVector(), ellipse.MajorAxis.ToRevitVector(), ellipse.StartParam, ellipse.EndParam);
                    }
                default:
                    break;
            }
            return default;
        }
        //public static List<Autodesk.Revit.DB.Curve> ToRevitCurve(Polyline polyline)
        //{
        //    var numVertices = polyline.NumberOfVertices;
        //    if (numVertices < 2)
        //    {
        //        return null;
        //    }
        //}
    }
}
