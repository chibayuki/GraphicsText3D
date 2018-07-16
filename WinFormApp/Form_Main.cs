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

            Label_Size.ForeColor = Label_Rotation.ForeColor = Me.RecommendColors.Text.ToColor();

            Label_Sx.ForeColor = Label_Sy.ForeColor = Label_Sz.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_Sx.BackColor = Label_Sy.BackColor = Label_Sz.BackColor = Me.RecommendColors.Button.ToColor();

            Label_Rx.ForeColor = Label_Ry.ForeColor = Label_Rz.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_Rx.BackColor = Label_Ry.BackColor = Label_Rz.BackColor = Me.RecommendColors.Button.ToColor();
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
            XY,
            YZ,
            ZX
        }

        private Bitmap GetProjectionOfCube(Com.PointD3D CubeSize, double[,] AffineMatrix, Views View, SizeF ImageSize)
        {
            //
            // 获取立方体的投影。
            //

            double CubeDiag = Math.Min(ImageSize.Width, ImageSize.Height);

            CubeSize = CubeSize.VectorNormalize * CubeDiag;

            Bitmap ProjBmp = new Bitmap(Math.Max(1, (Int32)ImageSize.Width), Math.Max(1, (Int32)ImageSize.Height));

            Graphics CreateProjBmp = Graphics.FromImage(ProjBmp);

            CreateProjBmp.SmoothingMode = SmoothingMode.AntiAlias;

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

            Com.PointD P2D_000 = new Com.PointD();
            Com.PointD P2D_100 = new Com.PointD();
            Com.PointD P2D_010 = new Com.PointD();
            Com.PointD P2D_110 = new Com.PointD();
            Com.PointD P2D_001 = new Com.PointD();
            Com.PointD P2D_101 = new Com.PointD();
            Com.PointD P2D_011 = new Com.PointD();
            Com.PointD P2D_111 = new Com.PointD();

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

            P2D_000 = GetProject3D(P3D_000, PrjCenter3D, TrueLenDist3D);
            P2D_100 = GetProject3D(P3D_100, PrjCenter3D, TrueLenDist3D);
            P2D_010 = GetProject3D(P3D_010, PrjCenter3D, TrueLenDist3D);
            P2D_110 = GetProject3D(P3D_110, PrjCenter3D, TrueLenDist3D);
            P2D_001 = GetProject3D(P3D_001, PrjCenter3D, TrueLenDist3D);
            P2D_101 = GetProject3D(P3D_101, PrjCenter3D, TrueLenDist3D);
            P2D_011 = GetProject3D(P3D_011, PrjCenter3D, TrueLenDist3D);
            P2D_111 = GetProject3D(P3D_111, PrjCenter3D, TrueLenDist3D);

            Com.PointD BitmapCenter = new Com.PointD(ProjBmp.Size) / 2;

            PointF P_000 = (P2D_000 + BitmapCenter).ToPointF();
            PointF P_100 = (P2D_100 + BitmapCenter).ToPointF();
            PointF P_010 = (P2D_010 + BitmapCenter).ToPointF();
            PointF P_110 = (P2D_110 + BitmapCenter).ToPointF();
            PointF P_001 = (P2D_001 + BitmapCenter).ToPointF();
            PointF P_101 = (P2D_101 + BitmapCenter).ToPointF();
            PointF P_011 = (P2D_011 + BitmapCenter).ToPointF();
            PointF P_111 = (P2D_111 + BitmapCenter).ToPointF();

            //

            List<Com.PointD3D[]> Side3D = new List<Com.PointD3D[]>()
            {
                // XY
                new Com.PointD3D[] { P3D_000, P3D_010, P3D_110, P3D_100 },
                new Com.PointD3D[] { P3D_001, P3D_011, P3D_111, P3D_101 },

                // YZ
                new Com.PointD3D[] { P3D_000, P3D_001, P3D_011, P3D_010 },
                new Com.PointD3D[] { P3D_100, P3D_101, P3D_111, P3D_110 },

                // ZX
                new Com.PointD3D[] { P3D_000, P3D_001, P3D_101, P3D_100 },
                new Com.PointD3D[] { P3D_010, P3D_011, P3D_111, P3D_110 }
            };

            List<PointF[]> Side2D = new List<PointF[]>()
            {
                // XY
                new PointF[] { P_000, P_010, P_110, P_100 },
                new PointF[] { P_001, P_011, P_111, P_101 },

                // YZ
                new PointF[] { P_000, P_001, P_011, P_010 },
                new PointF[] { P_100, P_101, P_111, P_110 },

                // ZX
                new PointF[] { P_000, P_001, P_101, P_100 },
                new PointF[] { P_010, P_011, P_111, P_110 }
            };

            List<Color> SideColor = new List<Color>()
            {
                // XY
                Colors.Side,
                Colors.Side,

                // YZ
                Colors.Side,
                Colors.Side,

                // ZX
                Colors.Side,
                Colors.Side
            };

            List<Com.PointD3D[]> Line3D = new List<Com.PointD3D[]>()
            {
                // X
                new Com.PointD3D[] { P3D_000, P3D_100 },
                new Com.PointD3D[] { P3D_010, P3D_110 },
                new Com.PointD3D[] { P3D_001, P3D_101 },
                new Com.PointD3D[] { P3D_011, P3D_111 },

                // Y
                new Com.PointD3D[] { P3D_000, P3D_010 },
                new Com.PointD3D[] { P3D_100, P3D_110 },
                new Com.PointD3D[] { P3D_001, P3D_011 },
                new Com.PointD3D[] { P3D_101, P3D_111 },

                // Z
                new Com.PointD3D[] { P3D_000, P3D_001 },
                new Com.PointD3D[] { P3D_100, P3D_101 },
                new Com.PointD3D[] { P3D_010, P3D_011 },
                new Com.PointD3D[] { P3D_110, P3D_111 }
            };

            List<PointF[]> Line2D = new List<PointF[]>()
            {
                // X
                new PointF[] { P_000, P_100 },
                new PointF[] { P_010, P_110 },
                new PointF[] { P_001, P_101 },
                new PointF[] { P_011, P_111 },

                // Y
                new PointF[] { P_000, P_010 },
                new PointF[] { P_100, P_110 },
                new PointF[] { P_001, P_011 },
                new PointF[] { P_101, P_111 },

                // Z
                new PointF[] { P_000, P_001 },
                new PointF[] { P_100, P_101 },
                new PointF[] { P_010, P_011 },
                new PointF[] { P_110, P_111 }
            };

            List<Color> LineColor = new List<Color>()
            {
                // X
                Colors.X,
                Colors.Line,
                Colors.Line,
                Colors.Line,

                // Y
                Colors.Y,
                Colors.Line,
                Colors.Line,
                Colors.Line,

                // Z
                Colors.Z,
                Colors.Line,
                Colors.Line,
                Colors.Line
            };

            Func<Com.PointD3D, Int32, Int32, Int32> GetAlphaOfPoint = (Pt, MinAlpha, MaxAlpha) =>
            {
                switch (View)
                {
                    case Views.XY: return (Int32)Math.Max(0, Math.Min(((Pt.Z - CubeCenter.Z) / CubeDiag + 0.5) * (MinAlpha - MaxAlpha) + MaxAlpha, 255));
                    case Views.YZ: return (Int32)Math.Max(0, Math.Min(((Pt.X - CubeCenter.X) / CubeDiag + 0.5) * (MinAlpha - MaxAlpha) + MaxAlpha, 255));
                    case Views.ZX: return (Int32)Math.Max(0, Math.Min(((Pt.Y - CubeCenter.Y) / CubeDiag + 0.5) * (MinAlpha - MaxAlpha) + MaxAlpha, 255));
                    default:
                        return 0;
                }
            };

            Func<Int32, Brush> GetBrushOfSide = (Index) =>
            {
                const Int32 _MinAlpha = 16, _MaxAlpha = 48;

                Com.PointD3D Pt_Avg = new Com.PointD3D(0, 0, 0);

                foreach (Com.PointD3D Pt in Side3D[Index])
                {
                    Pt_Avg += Pt;
                }

                Pt_Avg /= Side3D[Index].Length;

                return new SolidBrush(Color.FromArgb(GetAlphaOfPoint(Pt_Avg, _MinAlpha, _MaxAlpha), SideColor[Index]));
            };

            Func<Int32, Brush> GetBrushOfLine = (Index) =>
            {
                const Int32 _MinAlpha = 32, _MaxAlpha = 96;

                if (Com.PointD.DistanceBetween(new Com.PointD(Line2D[Index][0]), new Com.PointD(Line2D[Index][1])) > 1)
                {
                    Int32 Alpha0 = GetAlphaOfPoint(Line3D[Index][0], _MinAlpha, _MaxAlpha), Alpha1 = GetAlphaOfPoint(Line3D[Index][1], _MinAlpha, _MaxAlpha);

                    return new LinearGradientBrush(Line2D[Index][0], Line2D[Index][1], Color.FromArgb(Alpha0, LineColor[Index]), Color.FromArgb(Alpha1, LineColor[Index]));
                }
                else
                {
                    Int32 Alpha0 = GetAlphaOfPoint(Line3D[Index][0], _MinAlpha, _MaxAlpha);

                    return new SolidBrush(Color.FromArgb(Alpha0, LineColor[Index]));
                }
            };

            for (int i = 0; i < Side2D.Count; i++)
            {
                CreateProjBmp.FillPolygon(GetBrushOfSide(i), Side2D[i]);
            }

            for (int i = 0; i < Line2D.Count; i++)
            {
                CreateProjBmp.DrawLine(new Pen(GetBrushOfLine(i), 2F), Line2D[i][0], Line2D[i][1]);
            }

            CreateProjBmp.DrawString("X", new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Color.FromArgb(GetAlphaOfPoint(P3D_100, 64, 192), Colors.X)), P_100);
            CreateProjBmp.DrawString("Y", new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Color.FromArgb(GetAlphaOfPoint(P3D_010, 64, 192), Colors.Y)), P_010);
            CreateProjBmp.DrawString("Z", new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Color.FromArgb(GetAlphaOfPoint(P3D_001, 64, 192), Colors.Z)), P_001);

            //

            string ViewName = string.Empty;

            switch (View)
            {
                case Views.XY: ViewName = "XY 视图 (主视图)"; break;
                case Views.YZ: ViewName = "YZ 视图"; break;
                case Views.ZX: ViewName = "ZX 视图"; break;
            }

            CreateProjBmp.DrawString(ViewName, new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point, 134), new SolidBrush(Colors.Text), new PointF(Math.Max(0, (ProjBmp.Width - ProjBmp.Height) / 2), Math.Max(0, (ProjBmp.Height - ProjBmp.Width) / 2)));

            //

            CreateProjBmp.DrawRectangle(new Pen(Color.FromArgb(64, Colors.Border), 1F), new Rectangle(new Point(0, 0), ProjBmp.Size));

            //

            CreateProjBmp.Dispose();

            return ProjBmp;
        }

        private Com.PointD3D CubeSize = new Com.PointD3D(1, 1, 1); // 立方体各边长的比例。

        private double[,] AffineMatrix3D = new double[4, 4] // 3D 仿射矩阵。
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        };

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

            Bmp = new Bitmap(Math.Max(1, Panel_Main.Width), Math.Max(1, Panel_Main.Height));

            using (Graphics CreateBmp = Graphics.FromImage(Bmp))
            {
                CreateBmp.Clear(Colors.Background);

                //

                const Int32 NumX = 3, NumY = 1;

                SizeF BlockSize = new SizeF((float)Panel_Main.Width / NumX, (float)Panel_Main.Height / NumY);

                Bitmap[,] BmpArray = new Bitmap[NumX, NumY]
                {
                    {
                        GetProjectionOfCube(CubeSize, AffineMatrix3D, Views.XY, BlockSize)
                    },
                    {
                        GetProjectionOfCube(CubeSize, AffineMatrix3D, Views.YZ, BlockSize)
                    },
                    {
                        GetProjectionOfCube(CubeSize, AffineMatrix3D, Views.ZX, BlockSize)
                    }
                };

                for (int x = 0; x < NumX; x++)
                {
                    for (int y = 0; y < NumY; y++)
                    {
                        CreateBmp.DrawImage(BmpArray[x, y], new Point((Int32)(BlockSize.Width * x), (Int32)(BlockSize.Height * y)));
                    }
                }

                //

                foreach (Bitmap Bmp in BmpArray)
                {
                    if (Bmp != null)
                    {
                        Bmp.Dispose();
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
            Control Ctrl1 = Label_Size, Ctrl2 = Label_Rotation, Cntr = sender as Control;
            e.Graphics.DrawLine(P, new Point(Ctrl1.Right, Ctrl1.Top + Ctrl1.Height / 2), new Point(Cntr.Width - Ctrl1.Left, Ctrl1.Top + Ctrl1.Height / 2));
            e.Graphics.DrawLine(P, new Point(Ctrl2.Right, Ctrl2.Top + Ctrl2.Height / 2), new Point(Cntr.Width - Ctrl2.Left, Ctrl2.Top + Ctrl2.Height / 2));
            P.Dispose();
        }

        private Point ControlPanelLoc = new Point(); // 子窗口位置。
        private Point CursorLoc = new Point(); // 鼠标指针位置。
        private bool ControlPanelIsMoving = false; // 是否正在移动子窗口。

        private void Label_Control_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Control。
            //

            if (e.Button == MouseButtons.Left)
            {
                ControlPanelLoc = Panel_Control.Location;
                CursorLoc = e.Location;
                ControlPanelIsMoving = true;
            }
        }

        private void Label_Control_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Label_Control。
            //

            ControlPanelIsMoving = false;

            Panel_Control.Location = new Point(Math.Max(0, Math.Min(Panel_Control.Left, Panel_Client.Width - Label_Control_SubFormTitle.Right)), Math.Max(0, Math.Min(Panel_Control.Top, Panel_Client.Height - Label_Control_SubFormTitle.Bottom)));
        }

        private void Label_Control_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Control。
            //

            if (ControlPanelIsMoving)
            {
                Panel_Control.Location = new Point(Panel_Control.Left + (e.X - CursorLoc.X), Panel_Control.Top + (e.Y - CursorLoc.Y));
            }
        }

        #endregion

        #region 控制

        private Int32 CursorX = 0; // 鼠标指针 X 坐标。

        private const double RatioPerPixel = 0.01; // 每像素的缩放倍率。
        private Com.PointD3D CubeSizeCopy = new Com.PointD3D(); // 立方体各边长的比例。
        private bool ResizeNow = false; // 是否正在缩放立方体的某条边。

        private void Label_Sxyz_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Label_Sxyz。
            //

            ((Label)sender).BackColor = Me.RecommendColors.Button_DEC.ToColor();
        }

        private void Label_Sxyz_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Label_Sxyz。
            //

            ((Label)sender).BackColor = Me.RecommendColors.Button.ToColor();
        }

        private void Label_Sxyz_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Sxyz。
            //

            if (e.Button == MouseButtons.Left)
            {
                ((Label)sender).BackColor = Me.RecommendColors.Button_INC.ToColor();
                ((Label)sender).Cursor = Cursors.SizeWE;

                CursorX = e.X;
                CubeSizeCopy = CubeSize;
                ResizeNow = true;
            }
        }

        private void Label_Sxyz_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Label_Sxyz。
            //

            ResizeNow = false;

            ((Label)sender).BackColor = Me.RecommendColors.Button_DEC.ToColor();
            ((Label)sender).Cursor = Cursors.Default;

            Label_Sx.Text = "X";
            Label_Sy.Text = "Y";
            Label_Sz.Text = "Z";
        }

        private void Label_Sx_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Sx。
            //

            if (ResizeNow)
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

            if (ResizeNow)
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

            if (ResizeNow)
            {
                double ratio = Math.Max(0.01, 1 + (e.X - CursorX) * RatioPerPixel);

                ((Label)sender).Text = "× " + ratio.ToString("F2");

                CubeSize = Com.PointD3D.Max(new Com.PointD3D(0.001, 0.001, 0.001), new Com.PointD3D(CubeSizeCopy.X, CubeSizeCopy.Y, CubeSizeCopy.Z * ratio).VectorNormalize);

                RepaintBmp();
            }
        }

        private const double RadPerPixel = Math.PI / 180; // 每像素的旋转弧度。
        private double[,] AffineMatrix3DCopy = null; // 3D 仿射矩阵。
        private bool RotateNow = false; // 是否正在旋转立方体。

        private void Label_Rxyz_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Label_Rxyz。
            //

            ((Label)sender).BackColor = Me.RecommendColors.Button_DEC.ToColor();
        }

        private void Label_Rxyz_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Label_Rxyz。
            //

            ((Label)sender).BackColor = Me.RecommendColors.Button.ToColor();
        }

        private void Label_Rxyz_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Rxyz。
            //

            if (e.Button == MouseButtons.Left)
            {
                ((Label)sender).BackColor = Me.RecommendColors.Button_INC.ToColor();
                ((Label)sender).Cursor = Cursors.SizeWE;

                CursorX = e.X;
                Com.Matrix2D.Copy(AffineMatrix3D, out AffineMatrix3DCopy);
                RotateNow = true;
            }
        }

        private void Label_Rxyz_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Label_Rxyz。
            //

            RotateNow = false;

            ((Label)sender).BackColor = Me.RecommendColors.Button_DEC.ToColor();
            ((Label)sender).Cursor = Cursors.Default;

            Label_Rx.Text = "X";
            Label_Ry.Text = "Y";
            Label_Rz.Text = "Z";
        }

        private void Label_Rx_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Label_Rx。
            //

            if (RotateNow)
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

            if (RotateNow)
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

            if (RotateNow)
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

        #endregion

    }
}