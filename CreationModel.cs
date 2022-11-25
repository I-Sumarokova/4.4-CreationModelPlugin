using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace CreationModelPlugin
{

    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            CreateWalls(doc);

            return Result.Succeeded;
        }

        private void CreateWalls(Document doc)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level))
                            .OfType<Level>()
                            .ToList();

            Level level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();

            Level level2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();


            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            {
                transaction.Start();

                for (int i = 0; i < 4; i++)
                {
                   Line line = Line.CreateBound(points[i], points[i + 1]);
                   Wall wall = Wall.Create(doc, line, level1.Id, false);

                    walls.Add(wall);
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                }

                AddDoor(doc, level1, walls[0]);

                for (int i = 1; i <=3 ; i++)
                {
                    AddWindow(doc, level1, walls[i]);
                }

                AddRoof(doc, level2, walls);

                transaction.Commit();
            }
        }

         private void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            Application application = doc.Application;
            CurveArray curveArray = application.Create.NewCurveArray();

            LocationCurve curve = walls[0].Location as LocationCurve;
            XYZ p1 = curve.Curve.GetEndPoint(0);
            XYZ p2 = curve.Curve.GetEndPoint(1);
            Line line = Line.CreateBound(p1, p2);

            double dlinaLine = line.Length;
            double middleLine = dlinaLine/2;

            double extrusionStart = middleLine - dlinaLine;
            double extrusionEnd = middleLine;


            curveArray.Append(Line.CreateBound(new XYZ(p1.X, p1.Y, p1.Z), new XYZ(p1.X,p1.Y+middleLine/2,p1.Z+10)));
            curveArray.Append(Line.CreateBound(new XYZ(p1.X, p1.Y + middleLine/2, p1.Z + 10), new XYZ(p1.X, p1.Y+ middleLine, p1.Z)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 10), new XYZ(0, 1, 0), doc.ActiveView);
            ExtrusionRoof extrusionRoof =  doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, extrusionStart, extrusionEnd);

        }

        /*private void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));


            Application application = doc.Application;
            CurveArray footprint = application.Create.NewCurveArray();
            for (int i = 0; i < 4; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
                footprint.Append(line);

            }
            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);
                                //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                                //iterator.Reset();
                                //while (iterator.MoveNext())
                                //{
                                //    ModelCurve modelCurve = iterator.Current as ModelCurve;
                                //    footPrintRoof.set_DefinesSlope(modelCurve, true);
                                //    footPrintRoof.set_SlopeAngle(modelCurve, 0.5);
                                //}

            foreach(ModelCurve m in footPrintToModelCurveMapping)
            {
                footPrintRoof.set_DefinesSlope(m, true);
                footPrintRoof.set_SlopeAngle(m, 0.5);
            }
        }*/

        private void AddWindow(Document doc, Level level1, Wall wall)
         {
             FamilySymbol windowType = new FilteredElementCollector(doc)
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Windows)
                 .OfType<FamilySymbol>()
                 .Where(x => x.Name.Equals("0610 x 1220 мм"))
                 .Where(x => x.FamilyName.Equals("Фиксированные"))
                 .FirstOrDefault();

             LocationCurve hostCurve = wall.Location as LocationCurve;
             XYZ point1 = hostCurve.Curve.GetEndPoint(0);
             XYZ point2 = hostCurve.Curve.GetEndPoint(1);
             XYZ point = (point1 + point2) / 2;

             if (!windowType.IsActive)
                windowType.Activate();

             doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
         }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
    }
   
}
