using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CubePractice {
 public sealed class CubeCanvas : Control {
  readonly Preferences settings;
  Point previous;
  MouseButtons dragging=MouseButtons.None;
  public event Action PoseChanged;
  static readonly V3[] normals={new V3(1,0,0),new V3(-1,0,0),new V3(0,1,0),new V3(0,-1,0),new V3(0,0,1),new V3(0,0,-1)};
  sealed class Face {public V3 N,U,V,Rotated;public bool Front;public double Depth;}
  public CubeCanvas(Preferences model){settings=model;DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(240,242,239);TabStop=true;AccessibleName="透明立方体，左键转动，右键移动，滚轮缩放";SetStyle(ControlStyles.Selectable,true);}
  PointF ScreenPoint(V3 v){V3 p=Geometry.Project(settings.Orientation.Apply(v),Geometry.Distance(settings.Strength));double s=Math.Min(ClientSize.Width,ClientSize.Height)*.25*settings.Zoom;return new PointF((float)(ClientSize.Width*(.5+settings.PanX)+p.X*s),(float)(ClientSize.Height*(.5+settings.PanY)-p.Y*s));}
  static V3 Basis(V3 n){return Math.Abs(n.Y)>.5?new V3(1,0,0):new V3(0,1,0);}
  void DrawSegment(Graphics g,Pen pen,V3 a,V3 b){g.DrawLine(pen,ScreenPoint(a),ScreenPoint(b));}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);if(Width<2||Height<2)return;
   Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   double distance=Geometry.Distance(settings.Strength);
   var faces=new List<Face>();
   foreach(V3 normal in normals){V3 u=Basis(normal),v=V3.Cross(normal,u),rn=settings.Orientation.Apply(normal);faces.Add(new Face{N=normal,U=u,V=v,Rotated=rn,Depth=rn.Z,Front=rn.Z*distance>1});}
   faces.Sort(delegate(Face a,Face b){return a.Depth.CompareTo(b.Depth);});
   V3 light=new V3(-.35,.75,.56);light=light*(1/light.Length);
   foreach(Face face in faces){
    PointF[] corners={ScreenPoint(face.N-face.U-face.V),ScreenPoint(face.N+face.U-face.V),ScreenPoint(face.N+face.U+face.V),ScreenPoint(face.N-face.U+face.V)};
    double lit=Math.Max(0,V3.Dot(face.Rotated,light));int shade=(int)(105+85*lit),alpha=(int)(settings.Shade*2.55*(face.Front?1:.18));
    using(Brush fill=new SolidBrush(Color.FromArgb(alpha,shade,Math.Min(255,shade+12),Math.Min(255,shade+5))))g.FillPolygon(fill,corners);
    if(settings.Grid&&(face.Front||settings.RearEdges)){
     using(Pen pen=new Pen(Color.FromArgb(face.Front?115:47,55,72,63),1)){
      if(!face.Front)pen.DashStyle=DashStyle.Dash;
      for(int i=1;i<4;i++){double t=-1+i*.5;DrawSegment(g,pen,face.N+face.U*t-face.V,face.N+face.U*t+face.V);DrawSegment(g,pen,face.N+face.V*t-face.U,face.N+face.V*t+face.U);}
     }
    }
   }
   // Each edge belongs to two faces. A visible adjacent face makes a solid contour.
   for(int axis=0;axis<3;axis++)for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2){
    V3 p,q,n1,n2;
    if(axis==0){p=new V3(-1,a,b);q=new V3(1,a,b);n1=new V3(0,a,0);n2=new V3(0,0,b);}
    else if(axis==1){p=new V3(a,-1,b);q=new V3(a,1,b);n1=new V3(a,0,0);n2=new V3(0,0,b);}
    else{p=new V3(a,b,-1);q=new V3(a,b,1);n1=new V3(a,0,0);n2=new V3(0,b,0);}
    bool front=settings.Orientation.Apply(n1).Z*distance>1||settings.Orientation.Apply(n2).Z*distance>1;
    if(!front&&!settings.RearEdges)continue;
    using(Pen pen=new Pen(front?Color.FromArgb(43,62,52):Color.FromArgb(80,65,82,72),front?1.65f:1.0f)){if(!front)pen.DashStyle=DashStyle.Dash;DrawSegment(g,pen,p,q);}
   }
   if(settings.Compact){
    GraphicsState saved=g.Save();g.SmoothingMode=SmoothingMode.None;g.PixelOffsetMode=PixelOffsetMode.None;
    using(Brush border=new SolidBrush(Color.FromArgb(109,139,117))){g.FillRectangle(border,0,0,Width,1);g.FillRectangle(border,0,Height-1,Width,1);g.FillRectangle(border,0,0,1,Height);g.FillRectangle(border,Width-1,0,1,Height);}
    g.Restore(saved);
   }
  }
  void Changed(){Invalidate();if(PoseChanged!=null)PoseChanged();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left||e.Button==MouseButtons.Right||e.Button==MouseButtons.Middle){dragging=e.Button;previous=e.Location;Capture=true;Cursor=Cursors.SizeAll;}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging==MouseButtons.None)return;int dx=e.X-previous.X,dy=e.Y-previous.Y;previous=e.Location;
   if(dragging==MouseButtons.Left){Rotation turn=(ModifierKeys&Keys.Shift)==Keys.Shift?Rotation.Axis(new V3(0,0,1),-dx*.011):Rotation.Axis(new V3(0,1,0),dx*.011)*Rotation.Axis(new V3(1,0,0),dy*.011);settings.Orientation=(turn*settings.Orientation).Normalized();}
   else{settings.PanX=Geometry.Clamp(settings.PanX+dx/(double)Math.Max(1,Width),-.65,.65,0);settings.PanY=Geometry.Clamp(settings.PanY+dy/(double)Math.Max(1,Height),-.65,.65,0);}Changed();
  }
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=MouseButtons.None;Capture=false;Cursor=Cursors.Default;}
  protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){dragging=MouseButtons.None;Cursor=Cursors.Default;}}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);settings.Zoom=Geometry.Clamp(settings.Zoom*Math.Pow(1.12,e.Delta/120.0),.05,2.4,1);Changed();}
  protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(e.Button==MouseButtons.Left){settings.PanX=settings.PanY=0;settings.Zoom=1;Changed();}}
  protected override void WndProc(ref Message m){if(settings!=null&&settings.Compact&&m.Msg==0x84){Point p=PointToClient(WindowPlacement.MessagePoint(m.LParam));if(WindowPlacement.ClientHit(ClientSize,p,(ModifierKeys&Keys.Control)==Keys.Control)!=1){m.Result=new IntPtr(-1);return;}}base.WndProc(ref m);}
 }
}
