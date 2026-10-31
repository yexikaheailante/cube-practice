using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CubePractice {
 public sealed class MainWindow : Form {
  readonly string settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"偏好设置.xml");
  readonly Preferences settings;
  readonly CubeCanvas canvas;
  readonly TableLayoutPanel layout;
  readonly Panel options;
  readonly Button pin,compact;
  readonly Random random=new Random();
  readonly ToolTip tips=new ToolTip();
  FormWindowState expandedState=FormWindowState.Normal;
  public MainWindow(){
   settings=PreferenceStore.Load(settingsPath);Text="方体练习";Name="CubePracticeWindow";Font=new Font("Microsoft YaHei UI",9f);AutoScaleMode=AutoScaleMode.Font;BackColor=Color.FromArgb(249,250,247);MinimumSize=new Size(340,470);StartPosition=FormStartPosition.Manual;KeyPreview=true;
   string icon=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"方体.ico");if(File.Exists(icon))try{Icon=new Icon(icon);}catch(ArgumentException){}
   layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=Padding.Empty};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,144));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,27));Controls.Add(layout);
   var toolbar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Padding=new Padding(6),Margin=Padding.Empty};for(int i=0;i<4;i++)toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
   pin=MakeButton("置顶：关",delegate{TogglePin();});toolbar.Controls.Add(pin,0,0);
   toolbar.Controls.Add(MakeButton("换角度",delegate{RandomPose();}),1,0);toolbar.Controls.Add(MakeButton("复位",delegate{settings.ResetPose();canvas.Invalidate();}),2,0);
   compact=MakeButton("精简窗口",delegate{ToggleCompact();});toolbar.Controls.Add(compact,3,0);layout.Controls.Add(toolbar,0,0);
   canvas=new CubeCanvas(settings){Dock=DockStyle.Fill,Margin=Padding.Empty};layout.Controls.Add(canvas,0,1);tips.SetToolTip(canvas,"左键拖动：转动\n右键或中键拖动：移动\n滚轮：缩放\nShift + 左键：绕视线转动\n双击：回到中央\n精简窗口：Esc 恢复，Ctrl + 拖动移动窗口\nR 换角度 · Home 复位 · T 置顶");
   options=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(10,5,10,2)};layout.Controls.Add(options,0,2);
   var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=4};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,78));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44));for(int i=0;i<4;i++)fields.RowStyles.Add(new RowStyle(SizeType.Absolute,i==3?27:33));options.Controls.Add(fields);
   Label strengthValue=MakeLabel(StrengthName(settings.Strength));TrackBar strength=MakeSlider(100,settings.Strength);strength.AccessibleName="近大远小强度";strength.ValueChanged+=delegate{settings.Strength=strength.Value;strengthValue.Text=StrengthName(strength.Value);canvas.Invalidate();};fields.Controls.Add(MakeLabel("近大远小"),0,0);fields.Controls.Add(strength,1,0);fields.Controls.Add(strengthValue,2,0);tips.SetToolTip(strength,"向右：观察距离更近，近边与远边的大小差更明显。\n向左：观察距离更远，近大远小变弱。\n整体显示大小做补偿，滚轮单独控制缩放。");
   Label shadeValue=MakeLabel(settings.Shade+"%");TrackBar shade=MakeSlider(90,settings.Shade);shade.AccessibleName="面的着色程度";shade.ValueChanged+=delegate{settings.Shade=shade.Value;shadeValue.Text=shade.Value+"%";canvas.Invalidate();};fields.Controls.Add(MakeLabel("面着色"),0,1);fields.Controls.Add(shade,1,1);fields.Controls.Add(shadeValue,2,1);tips.SetToolTip(shade,"向左更透明；调到 0% 只保留线框。");
   var checks=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,2,0,0)};var grid=new CheckBox{Text="四等分网格",AutoSize=true,Checked=settings.Grid,Margin=new Padding(0,3,12,0)};var rear=new CheckBox{Text="后侧线",AutoSize=true,Checked=settings.RearEdges,Margin=new Padding(0,3,0,0)};grid.CheckedChanged+=delegate{settings.Grid=grid.Checked;canvas.Invalidate();};rear.CheckedChanged+=delegate{settings.RearEdges=rear.Checked;canvas.Invalidate();};checks.Controls.Add(grid);checks.Controls.Add(rear);fields.Controls.Add(checks,0,2);fields.SetColumnSpan(checks,3);
   var guide=MakeLabel("近大远小：左边弱，右边强");guide.ForeColor=Color.FromArgb(100,110,102);guide.Font=new Font(Font.FontFamily,8.2f);fields.Controls.Add(guide,0,3);fields.SetColumnSpan(guide,3);
   var hint=MakeLabel("左键转动 · 右键移动 · 滚轮缩放");hint.TextAlign=ContentAlignment.MiddleCenter;hint.Font=new Font(Font.FontFamily,8.2f);hint.ForeColor=Color.FromArgb(91,104,95);layout.Controls.Add(hint,0,3);
   TopMost=settings.Pinned;UpdatePin();ApplyCompactAppearance();RestoreBoundsSafely();Shown+=delegate{canvas.Focus();};FormClosing+=Save;
  }
  static Label MakeLabel(string text){return new Label{Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Margin=Padding.Empty};}
  static TrackBar MakeSlider(int maximum,int value){return new TrackBar{Minimum=0,Maximum=maximum,Value=value,TickStyle=TickStyle.None,AutoSize=false,Dock=DockStyle.Fill,Margin=Padding.Empty,SmallChange=1,LargeChange=10};}
  Button MakeButton(string text,Action action){var b=new Button{Text=text,Dock=DockStyle.Fill,Margin=new Padding(3,0,3,0),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(249,250,247),UseVisualStyleBackColor=false};b.FlatAppearance.BorderColor=Color.FromArgb(202,211,202);b.Click+=delegate{action();};return b;}
  static string StrengthName(int value){return value<30?"较弱":value>75?"较强":"适中";}
  void UpdatePin(){pin.Text=TopMost?"置顶：开":"置顶：关";pin.BackColor=TopMost?Color.FromArgb(51,89,69):BackColor;pin.ForeColor=TopMost?Color.White:ForeColor;pin.AccessibleName=pin.Text;}
  void TogglePin(){TopMost=!TopMost;settings.Pinned=TopMost;UpdatePin();}
  void ApplyCompactAppearance(){SuspendLayout();layout.SuspendLayout();if(settings.Compact){layout.Controls.Remove(canvas);Controls.Add(canvas);layout.Visible=false;MinimumSize=new Size(96,96);FormBorderStyle=FormBorderStyle.None;canvas.BringToFront();}else{if(canvas.Parent!=layout){Controls.Remove(canvas);layout.Controls.Add(canvas,0,1);}FormBorderStyle=FormBorderStyle.Sizable;MinimumSize=new Size(340,470);layout.Visible=true;}tips.Active=!settings.Compact;if(settings.Compact)tips.Hide(canvas);layout.ResumeLayout(true);ResumeLayout(true);}
  void ToggleCompact(){if(!settings.Compact){Rectangle full=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;settings.FullWidth=full.Width;settings.FullHeight=full.Height;settings.FullLeft=full.Left;settings.FullTop=full.Top;expandedState=WindowState;Rectangle content=canvas.RectangleToScreen(canvas.ClientRectangle);WindowState=FormWindowState.Normal;settings.Compact=true;ApplyCompactAppearance();Bounds=content;}else{settings.Compact=false;ApplyCompactAppearance();RestoreRectangle(new Rectangle(settings.FullLeft,settings.FullTop,settings.FullWidth,settings.FullHeight));WindowState=expandedState;}canvas.Focus();}
  void RandomPose(){double yaw=random.NextDouble()*Math.PI*2,pitch=(random.NextDouble()-.5)*2.2,roll=(random.NextDouble()-.5)*.55;settings.Orientation=(Rotation.Axis(new V3(0,0,1),roll)*Rotation.Axis(new V3(1,0,0),pitch)*Rotation.Axis(new V3(0,1,0),yaw)).Normalized();settings.PanX=settings.PanY=0;settings.Zoom=1;canvas.Invalidate();canvas.Focus();}
  void RestoreBoundsSafely(){RestoreRectangle(new Rectangle(settings.Left,settings.Top,settings.Width,settings.Height));}
  void RestoreRectangle(Rectangle wanted){Rectangle[] areas=Array.ConvertAll(Screen.AllScreens,delegate(Screen s){return s.WorkingArea;});Rectangle area=WindowPlacement.ChooseArea(wanted,areas,Screen.PrimaryScreen.WorkingArea);int w=Math.Min(area.Width,Math.Max(MinimumSize.Width,wanted.Width)),h=Math.Min(area.Height,Math.Max(MinimumSize.Height,wanted.Height));int x=wanted.Left==-1?area.Right-w-24:wanted.Left,y=wanted.Top==-1?area.Top+45:wanted.Top;SetBounds(Math.Max(area.Left,Math.Min(area.Right-w,x)),Math.Max(area.Top,Math.Min(area.Bottom-h,y)),w,h);}
  void Save(object sender,FormClosingEventArgs e){Rectangle b=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;settings.Width=b.Width;settings.Height=b.Height;settings.Left=b.Left;settings.Top=b.Top;settings.Pinned=TopMost;try{PreferenceStore.Save(settingsPath,settings);}catch(IOException){}catch(UnauthorizedAccessException){}}
  protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Escape&&settings.Compact){ToggleCompact();return true;}if(key==Keys.T){TogglePin();return true;}if(key==Keys.R){RandomPose();return true;}if(key==Keys.Home){settings.ResetPose();canvas.Invalidate();return true;}return base.ProcessCmdKey(ref msg,key);}
  protected override void WndProc(ref Message m){base.WndProc(ref m);if(settings!=null&&settings.Compact&&m.Msg==0x84)m.Result=new IntPtr(WindowPlacement.ClientHit(ClientSize,PointToClient(WindowPlacement.MessagePoint(m.LParam)),(ModifierKeys&Keys.Control)==Keys.Control));}
 }
 internal static class WindowPlacement {
  internal static Point MessagePoint(IntPtr value){long xy=value.ToInt64();return new Point((short)(xy&0xffff),(short)((xy>>16)&0xffff));}
  internal static int ClientHit(Size size,Point point,bool move){bool left=point.X<6,right=point.X>=size.Width-6,top=point.Y<6,bottom=point.Y>=size.Height-6;if(top&&left)return 13;if(top&&right)return 14;if(bottom&&left)return 16;if(bottom&&right)return 17;if(left)return 10;if(right)return 11;if(top)return 12;if(bottom)return 15;return move?2:1;}
  internal static Rectangle ChooseArea(Rectangle wanted,Rectangle[] areas,Rectangle fallback){long best=0;Rectangle result=fallback;foreach(Rectangle area in areas){Rectangle overlap=Rectangle.Intersect(wanted,area);long size=(long)Math.Max(0,overlap.Width)*Math.Max(0,overlap.Height);if(size>best){best=size;result=area;}}return result;}
 }
 static class Program {
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string className,string windowName);
  [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hWnd,int cmd);
  [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hWnd);
  [STAThread]static void Main(){bool first;using(var mutex=new Mutex(true,"Local\\Niki.CubePractice.20261029",out first)){if(!first){IntPtr existing=FindWindow(null,"方体练习");if(existing!=IntPtr.Zero){if(IsIconic(existing))ShowWindow(existing,9);SetForegroundWindow(existing);}return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainWindow());}}
 }
}
