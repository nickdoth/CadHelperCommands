using System;
// using System.Collections.Generic;
// using System.Text;
using Autodesk.AutoCAD.Runtime;
// using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
// using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
// using System.Collections;
using System.Windows.Forms;
using System.IO;
using WinformSaveDialog = System.Windows.Forms.SaveFileDialog;
using WinformOpenDialog = System.Windows.Forms.OpenFileDialog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(NickDoth.CadHelperCommands))]

namespace NickDoth
{
    public class CadHelperCommands
    {
    	[CommandMethod("lmc")]
    	public void MakeCoordinate()
        {
    		Document doc = AcadApp.DocumentManager.MdiActiveDocument;
    		Database db = doc.Database;
            Editor ed = doc.Editor;
			PromptPointResult ppr = null;

            ppr = ed.GetPoint("\nSelect start point: ");
            if (ppr.Status != PromptStatus.OK) return;
            Point3d pt1 = ppr.Value;

            ppr = ed.GetCorner("\nSelect end point: ", pt1);
            if (ppr.Status != PromptStatus.OK) return;
            Point3d pt2 = ppr.Value;

            using (TranMan tm = new TranMan(db))
            {
            	Entity[] cross = null;
            	Point3d pcur;

            	double startX = (double) Math.Min(pt1.X, pt2.X);
            	double endX = (double) Math.Max(pt1.X, pt2.X);
            	double startY = (double) Math.Min(pt1.Y, pt2.Y);
            	double endY = (double) Math.Max(pt1.Y, pt2.Y);

                int count = 0;

            	for (double n = startX; n <= endX; n += 200)
            	{
            		for (double m = startY; m <= endY; m += 200)
            		{
            			pcur = new Point3d(n, m, 0);
            			cross = DrawCross(pcur, 20);
		            	tm.AddNewDBObject(cross[0]);
		            	tm.AddNewDBObject(cross[1]);
		            	tm.AddNewDBObject(cross[2]);
		            	tm.AddNewDBObject(cross[3]);

                        ++count;
            		}
            	}

            	tm.Commit();
                ed.WriteMessage("\n " + count.ToString() + " points have been drawn.");
            }
    	}


        [CommandMethod("lje")]
        [CommandMethod("jdexport")]
        public void JDExport() {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            string jdFileData = "";

            ed.WriteMessage("Select polyline: ");
            PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();
            if (acSSPrompt.Status == PromptStatus.OK)
            {
                SelectionSet acSSet = acSSPrompt.Value;

                using (TranMan tm = new TranMan(db))
                {
                    foreach (SelectedObject acSSObj in acSSet)
                    {
                        if (acSSObj == null) continue;

                        Entity ent = tm.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Entity;

                        if (ent is Polyline)
                        {
                            ed.WriteMessage("\n(Is a polyline)");
                            Polyline pl = ent as Polyline;
                            
                            // var minfo = pl.GetType().GetMembers();
                            // foreach (var mi in minfo)
                            // {
                            //     ed.WriteMessage("\n" + mi.MemberType + " " + mi.Name);

                            // }

                            ed.WriteMessage("\nTotal length of road : " + pl.Length);
                            ed.WriteMessage("\nCount of JDs: " + pl.NumberOfVertices);
                            for (int n = 0; n < pl.NumberOfVertices; ++n)
                            {
                                Point2d pt = pl.GetPoint2dAt(n);
                                string jdInfo = "JD" + n + ",," + pt.X.ToString("F3") + "," + pt.Y.ToString("F3");
                                ed.WriteMessage("\n" + jdInfo);
                                jdFileData += jdInfo + ",0\r\n";
                            }
                            
                        }
                        else
                        {
                            ed.WriteMessage("\n(Is NOT polyline)");
                        }

                        ed.WriteMessage("\nFullName: " + ent.GetType().FullName);

                        WinformSaveDialog sfd = new WinformSaveDialog();
                        sfd.Title = "Export JDs data";
                        sfd.Filter = "Data Format(*.dat)|*.dat";
                        sfd.FileName = "jdx1.dat";
                        
                        Stream jdFile = null;
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            if ((jdFile = sfd.OpenFile()) != null)
                            {
                                using (StreamWriter sw = new StreamWriter(jdFile))
                                {
                                    sw.Write(jdFileData);
                                }
                            }
                        }


                    }
                } 
            }
        }

        [CommandMethod("lji")]
        [CommandMethod("jdimport")]
        public void JDImport()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Open JD file
            string jdData;
            string[] jdDataLine;
            WinformOpenDialog od = new WinformOpenDialog();
            if (od.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(od.FileName);
                jdData = sr.ReadToEnd();
                sr.Close();
                jdDataLine = jdData.Split(new char[] {'\n'});
            }
            else
            {
                ed.WriteMessage("\nFailed to open JD data file.");
                return;
            }

            

            

            using (TranMan tm = new TranMan(db))
            {
                using (Polyline jdPoly = new Polyline())
                {
                    // jdPoly.AddVertexAt(0, new Point2d(2, 4), 0, 0, 0);
                    // jdPoly.AddVertexAt(1, new Point2d(4, 2), 0, 0, 0);
                    // jdPoly.AddVertexAt(2, new Point2d(6, 4), 0, 0, 0);

                    Point2d pcur;
                    for (int i = 0; i < jdDataLine.Length; ++i)
                    {
                        ed.WriteMessage("\n" + jdDataLine[i]);
                        string[] jdPointInfo = jdDataLine[i].Split(new char[]{','});
                        if (jdPointInfo.Length < 4) continue;
                        pcur = new Point2d(double.Parse(jdPointInfo[3]), double.Parse(jdPointInfo[2]));
                        jdPoly.AddVertexAt(i, pcur, 0, 0, 0);
                    }

                    tm.AddNewDBObject(jdPoly);
                }

                tm.Commit();
            }

        }
        
        [CommandMethod("lzz")]
        public void zzzb()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Open JD file
            string jdData;
            string[] jdDataLine;
            WinformOpenDialog od = new WinformOpenDialog();
            if (od.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(od.FileName);
                jdData = sr.ReadToEnd();
                sr.Close();
                jdDataLine = jdData.Split(new char[] {'\n'});
            }
            else
            {
                ed.WriteMessage("\nFailed to open JD data file.");
                return;
            }

            

            

            using (TranMan tm = new TranMan(db))
            {
                Point3d pprv = new Point3d(-1, -1, -1);
                Point3d pcur;
                // Point3d textPos;
                for (int i = 0; i < jdDataLine.Length; ++i)
                {
                    
                    
                    ed.WriteMessage("\n" + jdDataLine[i]);
                    string[] jdPointInfo = jdDataLine[i].Split(new char[]{','});
                    if (jdPointInfo.Length < 4) continue;
                    
                    pcur = new Point3d(double.Parse(jdPointInfo[3]), double.Parse(jdPointInfo[2]), 0);
                    
                    if (i > 0)
                    {
                        var prvX = pprv.X;
                        var normalSlope = -1 / ((pcur.Y - pprv.Y) / (pcur.X - pprv.X));
                        var normalB = pcur.Y - normalSlope * pcur.X;
                        var normaLength = 20;
                        
                        var bX = pcur.X + normaLength / Math.Sqrt(Math.Pow(normalSlope, 2) + 1);
                        var bY = normalSlope * bX + normalB;
                        
                        var isRightSide = !IsLeft(pcur, pprv, bX, bY);
                        if (isRightSide)
                        {
                            bX = 2 * pcur.X - bX;
                            bY = 2 * pcur.Y - bY;
                        }
                        
                        var bPoint = new Point3d(bX, bY, 0);
                        
                        // var normalLine = new Line(pcur, new Point3d(0,0,0));
                        ed.WriteMessage("\n" + pcur.ToString());
                        tm.AddNewDBObject(new Line(pcur, bPoint));
                        
                        var zzText = new DBText();
                        zzText.Position = pcur;
                        zzText.Height = 5;
                        zzText.Rotation = Math.Atan(normalSlope) + (isRightSide ? Math.PI : 0);
                        zzText.TextString = jdPointInfo[0];
                        tm.AddNewDBObject(zzText);
                    } 
                    
                    pprv = pcur;
                    
                    // tm.AddNewDBObject(new Line(pcur, new Point3d(0,0,0)));
                }
                
                // tm.AddNewDBObject(null); // TODO: xxx

                tm.Commit();
            }

        }

    	public static Entity[] DrawCross(Point3d center, int length)
    	{
    		Point3d startPtV = new Point3d(center.X + length / 2, center.Y, 0);
    		Point3d endPtV = new Point3d(center.X - length / 2, center.Y, 0);
    		Point3d startPtH = new Point3d(center.X, center.Y + length / 2, 0);
    		Point3d endPtH = new Point3d(center.X, center.Y - length / 2, 0);
    		Line lv = new Line(startPtV, endPtV);
    		Line lh = new Line(startPtH, endPtH);

    		Point3d textPtX = new Point3d(center.X + length / 4, center.Y + length / 4, 0);
    		Point3d textPtY = new Point3d(center.X + length / 4, center.Y - length / 4, 0);
    		
    		DBText textX = new DBText();
    		textX.Position = textPtY;
    		textX.Height = 4;
    		textX.TextString = "Y " + center.X.ToString("F3");

    		DBText textY = new DBText();
    		textY.Position = textPtX;
    		textY.Height = 4;
    		textY.TextString = "X " + center.Y.ToString("F3");

    		return new Entity[]{lv, lh, textX, textY};
    	}
        
        
        public static bool IsLeft(Point3d pcur, Point3d pprv, double bX, double bY)
        {
            var k = (pcur.Y - pprv.Y) / (pcur.X - pprv.Y);
            var b = pcur.Y - k * pcur.X;
            
            var back = pcur.X < pprv.X;
            
            if (bY > k * bX + b){
                return !back;
            }
            else
            {
                return back;
            }
        }
    }

    public class TranMan : IDisposable
    {
    	private Database db;
    	private Transaction tran;
    	private BlockTableRecord space;

    	public TranMan(Database db) {
    		this.db = db;
    		this.tran = db.TransactionManager.StartTransaction();
    		this.space = (BlockTableRecord) this.tran.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
    	}

    	public void Commit() {
    		this.tran.Commit();
    	}

    	public ObjectId AddNewDBObject(Entity ent) {
    		ObjectId id = this.space.AppendEntity(ent);
    		this.tran.AddNewlyCreatedDBObject(ent, true);
    		return id;
    	}

    	public void Dispose() {
    		this.tran.Dispose();
    	}

        public DBObject GetObject(ObjectId id, OpenMode mode)
        {
            return this.tran.GetObject(id, mode);
        }
    }
}