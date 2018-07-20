/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
Copyright © 2018 chibayuki@foxmail.com

3D绘图测试
Version 18.7.10.0000

This file is part of "3D绘图测试" (GraphicsText3D)

"3D绘图测试" (GraphicsText3D) is released under the GPLv3 license
* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Drawing2D;

namespace WinFormApp
{
    public partial class Form_Main : Form
    {
        #region 版本信息

        private static readonly string ApplicationName = Application.ProductName; // 程序名。
        private static readonly string ApplicationEdition = "18"; // 程序版本。

        private static readonly Int32 MajorVersion = new Version(Application.ProductVersion).Major; // 主版本。
        private static readonly Int32 MinorVersion = new Version(Application.ProductVersion).Minor; // 副版本。
        private static readonly Int32 BuildNumber = new Version(Application.ProductVersion).Build; // 版本号。
        private static readonly Int32 BuildRevision = new Version(Application.ProductVersion).Revision; // 修订版本。
        private static readonly string LabString = "3D"; // 分支名。
        private static readonly string BuildTime = "180710-0000"; // 编译时间。

        #endregion

        #region 窗体构造

        private Com.WinForm.FormManager Me;

        public Com.WinForm.FormManager FormManager
        {
            get
            {
                return Me;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x00020000;

                CreateParams CP = base.CreateParams;

                if (Me != null && Me.FormStyle != Com.WinForm.FormStyle.Dialog)
                {
                    CP.Style = CP.Style | WS_MINIMIZEBOX;
                }

                return CP;
            }
        }

        private void _Ctor(Com.WinForm.FormManager owner)
        {
            InitializeComponent();

            //

            if (owner != null)
            {
                Me = new Com.WinForm.FormManager(this, owner);
            }
            else
            {
                Me = new Com.WinForm.FormManager(this);
            }

            //

            FormDefine();
        }

        public Form_Main()
        {
            _Ctor(null);
        }

        public Form_Main(Com.WinForm.FormManager owner)
        {
            _Ctor(owner);
        }

        private void FormDefine()
        {
            Me.Caption = ApplicationName;
            Me.FormStyle = Com.WinForm.FormStyle.Sizable;
            Me.EnableFullScreen = true;
            Me.ClientSize = new Size(800, 450);
            Me.Theme = Com.WinForm.Theme.Colorful;
            Me.ThemeColor = Com.ColorManipulation.GetRandomColorX();
            Me.FormState = Com.WinForm.FormState.Maximized;

            Me.Loaded += LoadedEvents;
            Me.Resize += ResizeEvents;
            Me.SizeChanged += SizeChangedEvents;
            Me.ThemeChanged += ThemeColorChangedEvents;
            Me.ThemeColorChanged += ThemeColorChangedEvents;
        }

        #endregion

        #region 窗体事件

        private void LoadedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体加载后发生。
            //

            Me.OnSizeChanged();
            Me.OnThemeChanged();

            Panel_GraphArea.BackColor = Colors.Background;

            Panel_GraphArea.Visible = true;
        }

        private void ResizeEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的大小调整时发生。
            //

            Panel_GraphArea.Size = Panel_Client.Size = Panel_Main.Size;

            Panel_Control.Location = new Point(Math.Max(0, Math.Min(Panel_Control.Left, Panel_Client.Width - Label_Control_SubFormTitle.Right)), Math.Max(0, Math.Min(Panel_Control.Top, Panel_Client.Height - Label_Control_SubFormTitle.Bottom)));
        }

        private void SizeChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的大小更改时发生。
            //

            RepaintBmp();
        }

        private void ThemeColorChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的主题色更改时发生。
            //

            Panel_Control.BackColor = Me.RecommendColors.Background_DEC.ToColor();

            Label_Control_SubFormTitle.ForeColor = Me.RecommendColors.Caption.ToColor();
            Label_Control_SubFormTitle.BackColor = Me.RecommendColors.CaptionBar.ToColor();

            //

            Label_Size.ForeColor = Label_Rotation.ForeColor = Label_Illumination.ForeColor = Me.RecommendColors.Text.ToColor();

            Label_Sx.ForeColor = Label_Sy.ForeColor = Label_Sz.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_Sx.BackColor = Label_Sy.BackColor = Label_Sz.BackColor = Me.RecommendColors.Button.ToColor();

            Label_Rx.ForeColor = Label_Ry.ForeColor = Label_Rz.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_Rx.BackColor = Label_Ry.BackColor = Label_Rz.BackColor = Me.RecommendColors.Button.ToColor();

            Label_IlluminationZ.ForeColor = Label_IlluminationXY.ForeColor = Label_Exposure.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_IlluminationZ.BackColor = Label_IlluminationXY.BackColor = Label_Exposure.BackColor = Me.RecommendColors.Button.ToColor();
        }

        #endregion

        #region 3D绘图

        private void AffineTransform(ref Com.PointD3D Pt, Com.PointD3D Origin, double[,] AffineMatrix)
        {
            //
            // 将一个 3D 坐标以指定点为新的原点进行仿射变换。
            //

            Pt -= Origin;
            Pt.AffineTransform(AffineMatrix);
            Pt += Origin;
        }

        private enum Views // 视角枚举。
        {
            NULL = -1,

            XY,
            YZ,
            ZX,

            COUNT
        }

        private Bitmap GetProjectionOfCube(Com.PointD3D CubeSize, double[,] AffineMatrix, Com.PointD3D IlluminationDirection, double Exposure, Views View, SizeF ImageSize)
        {
            //
            // 获取立方体的投影。
            //

            double CubeDiag = Math.Min(ImageSize.Width, ImageSize.Height);

            CubeSize = CubeSize.VectorNormalize * CubeDiag;

            Bitmap PrjBmp = new Bitmap(Math.Max(1, (Int32)ImageSize.Width), Math.Max(1, (Int32)ImageSize.Height));

            Color CubeColor = Me.RecommendColors.Main_DEC.AtAlpha(192).ToColor();

            //

            Com.PointD3D CubeCenter = new Com.PointD3D(0, 0, 0);

            Com.PointD3D P3D_000 = new Com.PointD3D(0, 0, 0);
            Com.PointD3D P3D_100 = new Com.PointD3D(1, 0, 0);
            Com.PointD3D P3D_010 = new Com.PointD3D(0, 1, 0);
            Com.PointD3D P3D_110 = new Com.PointD3D(1, 1, 0);
            Com.PointD3D P3D_001 = new Com.PointD3D(0, 0, 1);
            Com.PointD3D P3D_101 = new Com.PointD3D(1, 0, 1);
            Com.PointD3D P3D_011 = new Com.PointD3D(0, 1, 1);
            Com.PointD3D P3D_111 = new Com.PointD3D(1, 1, 1);

            P3D_000 = (P3D_000 - 0.5) * CubeSize + CubeCenter;
            P3D_100 = (P3D_100 - 0.5) * CubeSize + CubeCenter;
            P3D_010 = (P3D_010 - 0.5) * CubeSize + CubeCenter;
            P3D_110 = (P3D_110 - 0.5) * CubeSize + CubeCenter;
            P3D_001 = (P3D_001 - 0.5) * CubeSize + CubeCenter;
            P3D_101 = (P3D_101 - 0.5) * CubeSize + CubeCenter;
            P3D_011 = (P3D_011 - 0.5) * CubeSize + CubeCenter;
            P3D_111 = (P3D_111 - 0.5) * CubeSize + CubeCenter;

            Com.PointD3D RotateCenter3D = CubeCenter;

            AffineTransform(ref P3D_000, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_100, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_010, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_110, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_001, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_101, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_011, RotateCenter3D, AffineMatrix);
            AffineTransform(ref P3D_111, RotateCenter3D, AffineMatrix);

            //

            double TrueLenDist3D = new Com.PointD(Screen.PrimaryScreen.Bounds.Size).VectorModule;

            Com.PointD3D PrjCenter3D = CubeCenter;

            switch (View)
            {
                case Views.XY: PrjCenter3D.Z -= (TrueLenDist3D + CubeDiag / 2); break;
                case Views.YZ: PrjCenter3D.X -= (TrueLenDist3D + CubeDiag / 2); break;
                case Views.ZX: PrjCenter3D.Y -= (TrueLenDist3D + CubeDiag / 2); break;
            }

            Func<Com.PointD3D, Com.PointD3D, double, Com.PointD> GetProject3D = (Pt, PrjCenter, TrueLenDist) =>
            {
                switch (View)
                {
                    case Views.XY: return Pt.ProjectToXY(PrjCenter, TrueLenDist);
                    case Views.YZ: return Pt.ProjectToYZ(PrjCenter, TrueLenDist);
                    case Views.ZX: return Pt.ProjectToZX(PrjCenter, TrueLenDist);
                    default: return Com.PointD.NaN;
                }
            };

            Com.PointD P2D_000 = GetProject3D(P3D_000, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_100 = GetProject3D(P3D_100, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_010 = GetProject3D(P3D_010, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_110 = GetProject3D(P3D_110, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_001 = GetProject3D(P3D_001, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_101 = GetProject3D(P3D_101, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_011 = GetProject3D(P3D_011, PrjCenter3D, TrueLenDist3D);
            Com.PointD P2D_111 = GetProject3D(P3D_111, PrjCenter3D, TrueLenDist3D);

            Com.PointD BitmapCenter = new Com.PointD(PrjBmp.Size) / 2;

            PointF P_000 = (P2D_000 + BitmapCenter).ToPointF();
            PointF P_100 = (P2D_100 + BitmapCenter).ToPointF();
            PointF P_010 = (P2D_010 + BitmapCenter).ToPointF();
            PointF P_110 = (P2D_110 + BitmapCenter).ToPointF();
            PointF P_001 = (P2D_001 + BitmapCenter).ToPointF();
            PointF P_101 = (P2D_101 + BitmapCenter).ToPointF();
            PointF P_011 = (P2D_011 + BitmapCenter).ToPointF();
            PointF P_111 = (P2D_111 + BitmapCenter).ToPointF();

            //

            List<Com.PointD3D[]> Element3D = new List<Com.PointD3D[]>(18)
            {
                // XY 面
                new Com.PointD3D[] { P3D_000, P3D_010, P3D_110, P3D_100 },
                new Com.PointD3D[] { P3D_001, P3D_011, P3D_111, P3D_101 },

                // YZ 面
                new Com.PointD3D[] { P3D_000, P3D_001, P3D_011, P3D_010 },
                new Com.PointD3D[] { P3D_100, P3D_101, P3D_111, P3D_110 },

                // ZX 面
                new Com.PointD3D[] { P3D_000, P3D_001, P3D_101, P3D_100 },
                new Com.PointD3D[] { P3D_010, P3D_011, P3D_111, P3D_110 },
                        
                // X 棱
                new Com.PointD3D[] { P3D_000, P3D_100 },
                new Com.PointD3D[] { P3D_010, P3D_110 },
                new Com.PointD3D[] { P3D_001, P3D_101 },
                new Com.PointD3D[] { P3D_011, P3D_111 },

                // Y 棱
                new Com.PointD3D[] { P3D_000, P3D_010 },
                new Com.PointD3D[] { P3D_100, P3D_110 },
                new Com.PointD3D[] { P3D_001, P3D_011 },
                new Com.PointD3D[] { P3D_101, P3D_111 },

                // Z 棱
                new Com.PointD3D[] { P3D_000, P3D_001 },
                new Com.PointD3D[] { P3D_100, P3D_101 },
                new Com.PointD3D[] { P3D_010, P3D_011 },
                new Com.PointD3D[] { P3D_110, P3D_111 }
            };

            List<PointF[]> Element2D = new List<PointF[]>(18)
            {
                // XY 面
                new PointF[] { P_000, P_010, P_110, P_100 },
                new PointF[] { P_001, P_011, P_111, P_101 },

                // YZ 面
                new PointF[] { P_000, P_001, P_011, P_010 },
                new PointF[] { P_100, P_101, P_111, P_110 },

                // ZX 面
                new PointF[] { P_000, P_001, P_101, P_100 },
                new PointF[] { P_010, P_011, P_111, P_110 },
                        
                // X 棱
                new PointF[] { P_000, P_100 },
                new PointF[] { P_010, P_110 },
                new PointF[] { P_001, P_101 },
                new PointF[] { P_011, P_111 },

                // Y 棱
                new PointF[] { P_000, P_010 },
                new PointF[] { P_100, P_110 },
                new PointF[] { P_001, P_011 },
                new PointF[] { P_101, P_111 },

                // Z 棱
                new PointF[] { P_000, P_001 },
                new PointF[] { P_100, P_101 },
                new PointF[] { P_010, P_011 },
                new PointF[] { P_110, P_111 }
            };

            //

            List<double> IlluminationIntensity = new List<double>(Element3D.Count);

            double Exp = Math.Max(-2, Math.Min(Exposure / 50, 2));

            if (IlluminationDirection.IsEmpty)
            {
                for (int i = 0; i < 6; i++)
                {
                    IlluminationIntensity.Add(Exp);
                }

                for (int i = 6; i < Element3D.Count; i++)
                {
                    IlluminationIntensity.Add((Math.Sqrt(2) / 2) * (Exp + 1) + (Exp - 1));
                }
            }
            else
            {
                List<Com.PointD3D> NormalVector = new List<Com.PointD3D>(6)
                    {
                        // XY 面
                        new Com.PointD3D(0, 0, -1),
                        new Com.PointD3D(0, 0, 1),

                        // YZ 面
                        new Com.PointD3D(-1, 0, 0),
                        new Com.PointD3D(1, 0, 0),

                        // ZX 面
                        new Com.PointD3D(0, -1, 0),
                        new Com.PointD3D(0, 1, 0),
                    };

                Com.PointD3D NewOrigin = new Com.PointD3D(0, 0, 0).AffineTransformCopy(AffineMatrix);

                for (int i = 0; i < NormalVector.Count; i++)
                {
                    NormalVector[i] = NormalVector[i].AffineTransformCopy(AffineMatrix) - NewOrigin;
                }

                List<double> Angle = new List<double>(NormalVector.Count);

                for (int i = 0; i < NormalVector.Count; i++)
                {
                    Angle.Add(IlluminationDirection.AngleFrom(NormalVector[i]));
                }

                for (int i = 0; i < Angle.Count; i++)
                {
                    double A = Angle[i];
                    double CosA = Math.Cos(A);
                    double CosSqrA = CosA * CosA;

                    double _IlluminationIntensity = (A < Math.PI / 2 ? -CosSqrA : (A > Math.PI / 2 ? CosSqrA : 0));

                    if (CubeColor.A < 255 && A != Math.PI / 2)
                    {
                        double Transmittance = 1 - (double)CubeColor.A / 255;

                        if (A < Math.PI / 2)
                        {
                            _IlluminationIntensity += (Transmittance * Math.Abs(CosA) * CosSqrA);
                        }
                        else
                        {
                            _IlluminationIntensity -= ((1 - Transmittance) * (1 - Math.Abs(CosA)) * CosSqrA);
                        }
                    }

                    _IlluminationIntensity += Exp;

                    IlluminationIntensity.Add(_IlluminationIntensity);
                }

                for (int i = 6; i < Element3D.Count; i++)
                {
                    double _IlluminationIntensity = 0;

                    int Num = 0;

                    for (int j = 0; j < 6; j++)
                    {
                        bool Flag = true;

                        foreach (Com.PointD3D P in Element3D[i])
                        {
                            if (!Element3D[j].Contains(P))
                            {
                                Flag = false;

                                break;
                            }
                        }

                        if (Flag)
                        {
                            _IlluminationIntensity += IlluminationIntensity[j];

                            Num++;
                        }
                    }

                    _IlluminationIntensity = (Math.Sqrt(2) / 2) * (_IlluminationIntensity / Num + 1) + (Exp - 1);

                    IlluminationIntensity.Add(_IlluminationIntensity);
                }
            }

            for (int i = 0; i < IlluminationIntensity.Count; i++)
            {
                IlluminationIntensity[i] = Math.Max(-1, Math.Min(IlluminationIntensity[i], 1));
            }

            //

            List<Color> ElementColor = new List<Color>(IlluminationIntensity.Count);

            for (int i = 0; i < IlluminationIntensity.Count; i++)
            {
                double _IlluminationIntensity = IlluminationIntensity[i];

                Color ECr;

                switch (i)
                {
                    case 6: ECr = Colors.X; break;
                    case 10: ECr = Colors.Y; break;
                    case 14: ECr = Colors.Z; break;
                    default: ECr = CubeColor; break;
                }

                if (_IlluminationIntensity == 0)
                {
                    ElementColor.Add(ECr);
                }
                else
                {
                    Com.ColorX EColor = new Com.ColorX(ECr);

                    if (_IlluminationIntensity < 0)
                    {
                        EColor.Lightness_HSL += EColor.Lightness_HSL * _IlluminationIntensity;
                    }
                    else
                    {
                        EColor.Lightness_HSL += (100 - EColor.Lightness_HSL) * _IlluminationIntensity;
                    }

                    ElementColor.Add(EColor.ToColor());
                }
            }

            //

            List<double> ElementZAvg = new List<double>(Element3D.Count);

            for (int i = 0; i < Element3D.Count; i++)
            {
                Com.PointD3D[] Element = Element3D[i];

                double ZAvg = 0;

                foreach (Com.PointD3D P in Element)
                {
                    switch (View)
                    {
                        case Views.XY: ZAvg += P.Z; break;
                        case Views.YZ: ZAvg += P.X; break;
                        case Views.ZX: ZAvg += P.Y; break;
                    }
                }

                ZAvg /= Element.Length;

                ElementZAvg.Add(ZAvg);
            }

            List<int> ElementIndex = new List<int>(ElementZAvg.Count);

            for (int i = 0; i < ElementZAvg.Count; i++)
            {
                ElementIndex.Add(i);
            }

            for (int i = 0; i < ElementZAvg.Count; i++)
            {
                for (int j = i + 1; j < ElementZAvg.Count; j++)
                {
                    if (ElementZAvg[ElementIndex[i]] < ElementZAvg[ElementIndex[j]] || (ElementZAvg[ElementIndex[i]] <= ElementZAvg[ElementIndex[j]] + 2F && Element2D[ElementIndex[i]].Length < Element2D[ElementIndex[j]].Length))
                    {
                        int Temp = ElementIndex[i];
                        ElementIndex[i] = ElementIndex[j];
                        ElementIndex[j] = Temp;
                    }
                }
            }

            //

            using (Graphics Grph = Graphics.FromImage(PrjBmp))
            {
                Grph.SmoothingMode = SmoothingMode.AntiAlias;

                //

                for (int i = 0; i < ElementIndex.Count; i++)
                {
                    int EIndex = ElementIndex[i];

                    Color EColor = ElementColor[EIndex];

                    if (!EColor.IsEmpty && EColor.A > 0)
                    {
                        PointF[] Element = Element2D[EIndex];

                        if (Element.Length >= 3)
                        {
                            try
                            {
                                using (SolidBrush Br = new SolidBrush(EColor))
                                {
                                    Grph.FillPolygon(Br, Element);
                                }
                            }
                            catch { }
                        }
                        else if (Element.Length == 2)
                        {
                            double PrjZ = 0;

                            switch (View)
                            {
                                case Views.XY: PrjZ = PrjCenter3D.Z; break;
                                case Views.YZ: PrjZ = PrjCenter3D.X; break;
                                case Views.ZX: PrjZ = PrjCenter3D.Y; break;
                            }

                            float EdgeWidth = (TrueLenDist3D == 0 ? 2F : (float)(TrueLenDist3D / (ElementZAvg[EIndex] - PrjZ) * 2F));

                            try
                            {
                                Brush Br;

                                Func<Color, double, int> GetAlpha = (Cr, Z) =>
                                {
                                    int Alpha;

                                    if (TrueLenDist3D == 0)
                                    {
                                        Alpha = Cr.A;
                                    }
                                    else
                                    {
                                        if (Z - PrjZ <= TrueLenDist3D)
                                        {
                                            Alpha = Cr.A;
                                        }
                                        else
                                        {
                                            Alpha = (int)Math.Max(0, Math.Min(TrueLenDist3D / (Z - PrjZ) * Cr.A, 255));
                                        }
                                    }

                                    if (EdgeWidth < 1)
                                    {
                                        Alpha = (int)(Alpha * EdgeWidth);
                                    }

                                    return Alpha;
                                };

                                if (Com.PointD.DistanceBetween(new Com.PointD(Element[0]), new Com.PointD(Element[1])) > 1)
                                {
                                    int Alpha0 = 0, Alpha1 = 0;

                                    switch (View)
                                    {
                                        case Views.XY: Alpha0 = GetAlpha(EColor, Element3D[EIndex][0].Z); Alpha1 = GetAlpha(EColor, Element3D[EIndex][1].Z); break;
                                        case Views.YZ: Alpha0 = GetAlpha(EColor, Element3D[EIndex][0].X); Alpha1 = GetAlpha(EColor, Element3D[EIndex][1].X); break;
                                        case Views.ZX: Alpha0 = GetAlpha(EColor, Element3D[EIndex][0].Y); Alpha1 = GetAlpha(EColor, Element3D[EIndex][1].Y); break;
                                    }

                                    Br = new LinearGradientBrush(Element[0], Element[1], Color.FromArgb(Alpha0, EColor), Color.FromArgb(Alpha1, EColor));
                                }
                                else
                                {
                                    int Alpha = GetAlpha(EColor, ElementZAvg[EIndex]);

                                    Br = new SolidBrush(Color.FromArgb(Alpha, EColor));
                                }

                                using (Pen Pn = new Pen(Br, EdgeWidth))
                                {
                                    Grph.DrawLines(Pn, Element);
                                }

                                if (Br != null)
                                {
                                    Br.Dispose();
                                }
                            }
                            catch { }
                        }
                    }
                }

                //

                Func<Com.PointD3D, Int32, Int32, Int32> GetAlphaOfPoint = (Pt, MinAlpha, MaxAlpha) =>
                {
                    switch (View)
                    {
                        case Views.XY: return (Int32)Math.Max(0, Math.Min(((Pt.Z - CubeCenter.Z) / CubeDiag + 0.5) * (MinAlpha - MaxAlpha) + MaxAlpha, 255));
                        case Views.YZ: return (Int32)Math.Max(0, Math.Min(((Pt.X - CubeCenter.X) / CubeDiag + 0.5) * (MinAlpha - MaxAlpha) + MaxAlpha, 255));
                        case Views.ZX: return (Int32)Math.Max(0, Math.Min(((Pt.Y - CubeCenter.Y) / CubeDiag + 0.5) * (MinAlpha - MaxAlpha) + MaxAlpha, 255));
                        default: return 0;
                    }
                };

                Grph.DrawString("X", new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Color.FromArgb(GetAlphaOfPoint(P3D_100, 64, 192), Colors.X)), P_100);
                Grph.DrawString("Y", new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Color.FromArgb(GetAlphaOfPoint(P3D_010, 64, 192), Colors.Y)), P_010);
                Grph.DrawString("Z", new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Color.FromArgb(GetAlphaOfPoint(P3D_001, 64, 192), Colors.Z)), P_001);

                //

                string ViewName = string.Empty;

                switch (View)
                {
                    case Views.XY: ViewName = "XY 视图 (主视图)"; break;
                    case Views.YZ: ViewName = "YZ 视图"; break;
                    case Views.ZX: ViewName = "ZX 视图"; break;
                }

                Grph.DrawString(ViewName, new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Colors.Text), new PointF(Math.Max(0, (PrjBmp.Width - PrjBmp.Height) / 2), Math.Max(0, (PrjBmp.Height - PrjBmp.Width) / 2)));

                //

                Grph.DrawRectangle(new Pen(Color.FromArgb(64, Colors.Border), 1F), new Rectangle(new Point(-1, -1), PrjBmp.Size));
            }

            return PrjBmp;
        }

        private Com.PointD3D CubeSize = new Com.PointD3D(1, 1, 1); // 立方体各边长的比例。

        private double[,] AffineMatrix3D = new double[4, 4] // 3D 仿射矩阵。
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        };

        private Com.PointD3D IlluminationDirection = new Com.PointD3D(1, 0, 0); // 光照方向（球坐标系）。

        private double Exposure = 0; // 曝光。

        private static class Colors // 颜色。
        {
            public static readonly Color Background = Color.Black;
            public static readonly Color Text = Color.White;
            public static readonly Color Border = Color.White;

            public static readonly Color X = Color.DeepPink;
            public static readonly Color Y = Color.Lime;
            public static readonly Color Z = Color.DeepSkyBlue;

            public static readonly Color Side = Color.White;
            public static readonly Color Line = Color.White;
        }

        private Bitmap Bmp; // 位图。

        private void UpdateBmp()
        {
            //
            // 更新位图。
            //

            if (Bmp != null)
            {
                Bmp.Dispose();
            }

            Bmp = new Bitmap(Math.Max(1, Panel_GraphArea.Width), Math.Max(1, Panel_GraphArea.Height));

            using (Graphics Grph = Graphics.FromImage(Bmp))
            {
                Grph.Clear(Colors.Background);

                //

                int N = (int)Views.COUNT;
                double R = Math.Sqrt(N);
                int W = Math.Max(1, (int)Math.Floor(R * Math.Sqrt((double)Panel_GraphArea.Width / Panel_GraphArea.Height)));
                int H = Math.Max(1, (int)Math.Floor(R * Math.Sqrt((double)Panel_GraphArea.Height / Panel_GraphArea.Width)));

                while (W * H < N)
                {
                    if ((W + 1) * H >= N || W * (H + 1) >= N)
                    {
                        if (Math.Abs((double)Panel_GraphArea.Width / (W + 1) - (double)Panel_GraphArea.Height / H) <= Math.Abs((double)Panel_GraphArea.Width / W - (double)Panel_GraphArea.Height / (H + 1)))
                        {
                            W++;
                        }
                        else
                        {
                            H++;
                        }
                    }
                    else
                    {
                        W++;
                        H++;
                    }
                }

                SizeF BlockSize = new SizeF((float)Panel_GraphArea.Width / W, (float)Panel_GraphArea.Height / H);

                Bitmap[] PrjBmpArray = new Bitmap[(int)Views.COUNT]
                {
                    GetProjectionOfCube(CubeSize, AffineMatrix3D, IlluminationDirection.ToCartesian(), Exposure, Views.XY, BlockSize),
                    GetProjectionOfCube(CubeSize, AffineMatrix3D, IlluminationDirection.ToCartesian(), Exposure, Views.YZ, BlockSize),
                    GetProjectionOfCube(CubeSize, AffineMatrix3D, IlluminationDirection.ToCartesian(), Exposure, Views.ZX, BlockSize)
                };

                for (int i = 0; i < PrjBmpArray.Length; i++)
                {
                    Bitmap PrjBmp = PrjBmpArray[i];

                    if (PrjBmp != null)
                    {
                        Grph.DrawImage(PrjBmp, new Point((Int32)(BlockSize.Width * (i % W)), (Int32)(BlockSize.Height * (i / W))));

                        PrjBmp.Dispose();
                    }
                }
            }
        }

        private void RepaintBmp()
        {
            //
            // 更新并重绘位图。
            //

            if (Panel_GraphArea.Visible && (Panel_GraphArea.Width > 0 && Panel_GraphArea.Height > 0))
            {
                UpdateBmp();

                if (Bmp != null)
                {
                    Panel_GraphArea.CreateGraphics().DrawImage(Bmp, new Point(0, 0));
                }
            }
        }

        private void Panel_GraphArea_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_GraphArea 绘图。
            //

            if (Bmp == null)
            {
                UpdateBmp();
            }

            if (Bmp != null)
            {
                e.Graphics.DrawImage(Bmp, new Point(0, 0));
            }
        }

        #endregion

        #region "控制"子窗口

        private void Panel_Control_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Control 绘图。
            //

            Pen P = new Pen(Me.RecommendColors.Main.ToColor(), 1);
            Control Cntr = sender as Control;
            e.Graphics.DrawRectangle(P, new Rectangle(new Point(0, 0), new Size(Cntr.Width - 1, Cntr.Height - 1)));
            P.Dispose();
        }

        private void Panel_Control_SubFormClient_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Control_SubFormClient 绘图。
            //

            Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
            Control Ctrl1 = Label_Size, Ctrl2 = Label_Rotation, Ctrl3 = Label_Illumination, Cntr = sender as Control;
            e.Graphics.DrawLine(P, new Point(Ctrl1.Right, Ctrl1.Top + Ctrl1.Height / 2), new Point(Cntr.Width - Ctrl1.Left, Ctrl1.Top + Ctrl1.Height / 2));
            e.Graphics.DrawLine(P, new Point(Ctrl2.Right, Ctrl2.Top + Ctrl2.Height / 2), new Point(Cntr.Width - Ctrl2.Left, Ctrl2.Top + Ctrl2.Height / 2));
            e.Graphics.DrawLine(P, new Point(Ctrl3.Right, Ctrl3.Top + Ctrl3.Height / 2), new Point(Cntr.Width - Ctrl3.Left, Ctrl3.Top + Ctrl3.Height / 2));
            P.Dispose();
        }

        private Point SubFormLoc = new Point(); // 子窗口位置。
        private Point CursorLoc = new Point(); // 鼠标指针位置。
        private bool SubFormIsMoving = false; // 是否正在移动子窗口。

        private void Label_Control_SubFormTitle_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Control_SubFormTitle。
            //

            if (e.Button == MouseButtons.Left)
            {
                SubFormLoc = Panel_Control.Location;
                CursorLoc = e.Location;
                SubFormIsMoving = true;
            }
        }

        private void Label_Control_SubFormTitle_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Label_Control_SubFormTitle。
            //

            if (e.Button == MouseButtons.Left)
            {
                SubFormIsMoving = false;

                Panel_Control.Location = new Point(Math.Max(0, Math.Min(Panel_Control.Left, Panel_Client.Width - Label_Control_SubFormTitle.Right)), Math.Max(0, Math.Min(Panel_Control.Top, Panel_Client.Height - Label_Control_SubFormTitle.Bottom)));
            }
        }

        private void Label_Control_SubFormTitle_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Control_SubFormTitle。
            //

            if (SubFormIsMoving)
            {
                Panel_Control.Location = new Point(Panel_Control.Left + (e.X - CursorLoc.X), Panel_Control.Top + (e.Y - CursorLoc.Y));
            }
        }

        #endregion

        #region 控制

        private const double RatioPerPixel = 0.01; // 每像素的缩放倍率。
        private const double RadPerPixel = Math.PI / 180; // 每像素的旋转弧度。
        private const double ShiftPerPixel = 1; // 每像素的偏移量。

        private Int32 CursorX = 0; // 鼠标指针 X 坐标。
        private bool AdjustNow = false; // 是否正在调整。

        private Com.PointD3D CubeSizeCopy = new Com.PointD3D(); // 立方体各边长的比例。
        private double[,] AffineMatrix3DCopy = null; // 3D 仿射矩阵。
        private Com.PointD3D IlluminationDirectionCopy = new Com.PointD3D(); // 光照方向（球坐标系）。
        private double ExposureCopy = 0; // 曝光。

        private void Label_Control_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Label_Control。
            //

            ((Label)sender).BackColor = Me.RecommendColors.Button_DEC.ToColor();
        }

        private void Label_Control_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Label_Control。
            //

            ((Label)sender).BackColor = Me.RecommendColors.Button.ToColor();
        }

        private void Label_Control_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Control。
            //

            if (e.Button == MouseButtons.Left)
            {
                ((Label)sender).BackColor = Me.RecommendColors.Button_INC.ToColor();
                ((Label)sender).Cursor = Cursors.SizeWE;

                CubeSizeCopy = CubeSize;
                Com.Matrix2D.Copy(AffineMatrix3D, out AffineMatrix3DCopy);
                IlluminationDirectionCopy = IlluminationDirection;
                ExposureCopy = Exposure;

                CursorX = e.X;
                AdjustNow = true;
            }
        }

        private void Label_Control_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Label_Control。
            //

            if (e.Button == MouseButtons.Left)
            {
                AdjustNow = false;

                ((Label)sender).BackColor = (Com.Geometry.CursorIsInControl((Label)sender) ? Me.RecommendColors.Button_DEC.ToColor() : Me.RecommendColors.Button.ToColor());
                ((Label)sender).Cursor = Cursors.Default;

                Label_Sx.Text = "X";
                Label_Sy.Text = "Y";
                Label_Sz.Text = "Z";
                Label_Rx.Text = "X";
                Label_Ry.Text = "Y";
                Label_Rz.Text = "Z";
                Label_IlluminationZ.Text = "Z";
                Label_IlluminationXY.Text = "XY";
                Label_Exposure.Text = "Exp";
            }
        }

        private void Label_Sx_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Sx。
            //

            if (AdjustNow)
            {
                double ratio = Math.Max(0.01, 1 + (e.X - CursorX) * RatioPerPixel);

                ((Label)sender).Text = "× " + ratio.ToString("F2");

                CubeSize = Com.PointD3D.Max(new Com.PointD3D(0.001, 0.001, 0.001), new Com.PointD3D(CubeSizeCopy.X * ratio, CubeSizeCopy.Y, CubeSizeCopy.Z).VectorNormalize);

                RepaintBmp();
            }
        }

        private void Label_Sy_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Sy。
            //

            if (AdjustNow)
            {
                double ratio = Math.Max(0.01, 1 + (e.X - CursorX) * RatioPerPixel);

                ((Label)sender).Text = "× " + ratio.ToString("F2");

                CubeSize = Com.PointD3D.Max(new Com.PointD3D(0.001, 0.001, 0.001), new Com.PointD3D(CubeSizeCopy.X, CubeSizeCopy.Y * ratio, CubeSizeCopy.Z).VectorNormalize);

                RepaintBmp();
            }
        }

        private void Label_Sz_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Sz。
            //

            if (AdjustNow)
            {
                double ratio = Math.Max(0.01, 1 + (e.X - CursorX) * RatioPerPixel);

                ((Label)sender).Text = "× " + ratio.ToString("F2");

                CubeSize = Com.PointD3D.Max(new Com.PointD3D(0.001, 0.001, 0.001), new Com.PointD3D(CubeSizeCopy.X, CubeSizeCopy.Y, CubeSizeCopy.Z * ratio).VectorNormalize);

                RepaintBmp();
            }
        }

        private void Label_Rx_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Rx。
            //

            if (AdjustNow)
            {
                double angle = (e.X - CursorX) * RadPerPixel;

                ((Label)sender).Text = (angle >= 0 ? "+ " : "- ") + (Math.Abs(angle) / Math.PI * 180).ToString("F0") + "°";

                double[,] matrixLeft = Com.PointD3D.RotateXMatrix(angle);

                if (Com.Matrix2D.Multiply(matrixLeft, AffineMatrix3DCopy, out AffineMatrix3D))
                {
                    RepaintBmp();
                }
            }
        }

        private void Label_Ry_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Ry。
            //

            if (AdjustNow)
            {
                double angle = (e.X - CursorX) * RadPerPixel;

                ((Label)sender).Text = (angle >= 0 ? "+ " : "- ") + (Math.Abs(angle) / Math.PI * 180).ToString("F0") + "°";

                double[,] matrixLeft = Com.PointD3D.RotateYMatrix(angle);

                if (Com.Matrix2D.Multiply(matrixLeft, AffineMatrix3DCopy, out AffineMatrix3D))
                {
                    RepaintBmp();
                }
            }
        }

        private void Label_Rz_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Rz。
            //

            if (AdjustNow)
            {
                double angle = (e.X - CursorX) * RadPerPixel;

                ((Label)sender).Text = (angle >= 0 ? "+ " : "- ") + (Math.Abs(angle) / Math.PI * 180).ToString("F0") + "°";

                double[,] matrixLeft = Com.PointD3D.RotateZMatrix(angle);

                if (Com.Matrix2D.Multiply(matrixLeft, AffineMatrix3DCopy, out AffineMatrix3D))
                {
                    RepaintBmp();
                }
            }
        }

        private void Label_IlluminationZ_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_IlluminationZ。
            //

            if (AdjustNow)
            {
                double angle = (e.X - CursorX) * RadPerPixel;

                double Z = Com.Geometry.AngleMapping(IlluminationDirectionCopy.Y + angle);

                if (Z <= Math.PI)
                {
                    IlluminationDirection.Y = Z;
                    IlluminationDirection.Z = IlluminationDirectionCopy.Z;
                }
                else
                {
                    IlluminationDirection.Y = 2 * Math.PI - Z;
                    IlluminationDirection.Z = Com.Geometry.AngleMapping(IlluminationDirectionCopy.Z + Math.PI);
                }

                ((Label)sender).Text = (IlluminationDirection.Y / Math.PI * 180).ToString("F0") + "°";

                RepaintBmp();
            }
        }

        private void Label_IlluminationXY_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_IlluminationXY。
            //

            if (AdjustNow)
            {
                double angle = (e.X - CursorX) * RadPerPixel;

                IlluminationDirection.Z = Com.Geometry.AngleMapping(IlluminationDirectionCopy.Z + angle);

                ((Label)sender).Text = (IlluminationDirection.Z / Math.PI * 180).ToString("F0") + "°";

                RepaintBmp();
            }
        }

        private void Label_Exposure_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Exposure。
            //

            if (AdjustNow)
            {
                double shift = (e.X - CursorX) * ShiftPerPixel;

                Exposure = Math.Max(-100, Math.Min(ExposureCopy + shift, 100));

                ((Label)sender).Text = (Exposure >= 0 ? "+ " : "- ") + Math.Abs(Exposure);

                RepaintBmp();
            }
        }

        #endregion

    }
}