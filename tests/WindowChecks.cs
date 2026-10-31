using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CubePractice;

class WindowChecks {
 static object Field(object value,string name){return value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
 static void Invoke(object value,string name,params object[] args){value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,args);}
 static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
 static void Layout(Control c){c.CreateControl();c.PerformLayout();foreach(Control child in c.Controls)Layout(child);}
 [STAThread]static int Main(){
  try{
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   Rectangle negativeScreen=new Rectangle(-1920,-1080,1920,1080),primary=new Rectangle(0,0,1920,1040);
   Check(WindowPlacement.ChooseArea(new Rectangle(-1500,-800,456,599),new[]{primary,negativeScreen},primary)==negativeScreen,"negative monitor coordinates restore to the correct screen");
   Check(WindowPlacement.ChooseArea(new Rectangle(9000,9000,456,599),new[]{primary,negativeScreen},primary)==primary,"disconnected monitor falls back to primary screen");
   Check(WindowPlacement.ClientHit(new Size(96,96),new Point(48,48),false)==1&&WindowPlacement.ClientHit(new Size(96,96),new Point(48,48),true)==2,"center hit remains interactive and Ctrl enables window dragging");
   Check(WindowPlacement.ClientHit(new Size(96,96),new Point(1,40),false)==10&&WindowPlacement.ClientHit(new Size(96,96),new Point(94,94),false)==17,"invisible edges support window resizing");
   string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"previews");Directory.CreateDirectory(output);
   using(MainWindow window=new MainWindow()){
    window.Bounds=new Rectangle(0,0,456,599);
    // Set managed visibility for off-screen drawing, without displaying a desktop window.
    typeof(Control).GetMethod("SetState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{2,true});
    Layout(window);
    Preferences p=(Preferences)Field(window,"settings");CubeCanvas canvas=(CubeCanvas)Field(window,"canvas");
    using(Bitmap bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(Path.Combine(output,"window-normal.png"));}
    Invoke(window,"TogglePin");Check(window.TopMost&&p.Pinned,"pin action enables native TopMost and saved choice");Invoke(window,"TogglePin");Check(!window.TopMost&&!p.Pinned,"pin action disables native TopMost");
    double before=p.Orientation.X;Invoke(window,"RandomPose");Check(Math.Abs(before-p.Orientation.X)>1e-8,"random angle changes the cube pose");
    p.ResetPose();Layout(window);Invoke(canvas,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,80,80,0));Invoke(canvas,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,140,110,0));Invoke(canvas,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,140,110,0));Check(Math.Abs(p.Orientation.X-Rotation.Axis(new V3(1,0,0),.43).X)>1e-5,"left-drag handlers rotate the model");
    p.PanX=p.PanY=0;Invoke(canvas,"OnMouseDown",new MouseEventArgs(MouseButtons.Right,1,100,100,0));Invoke(canvas,"OnMouseMove",new MouseEventArgs(MouseButtons.Right,0,125,135,0));Invoke(canvas,"OnMouseUp",new MouseEventArgs(MouseButtons.Right,1,125,135,0));Check(p.PanX>0&&p.PanY>0,"right-drag handlers move the model");
    p.Zoom=1;Invoke(canvas,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,100,100,120));Check(p.Zoom>1,"wheel handler zooms in");Invoke(canvas,"OnMouseDoubleClick",new MouseEventArgs(MouseButtons.Left,2,100,100,0));Check(p.Zoom==1&&p.PanX==0&&p.PanY==0,"double-click recenters and restores scale");
    Rectangle expandedBounds=window.Bounds;Rotation preserved=p.Orientation;int preservedStrength=p.Strength;bool preservedGrid=p.Grid;double preservedZoom=p.Zoom;
    Invoke(window,"ToggleCompact");Layout(window);
    Check(p.Compact&&window.FormBorderStyle==FormBorderStyle.None,"compact mode removes title bar and window frame");
    int visibleCount=0;foreach(Control child in window.Controls)if(child.Visible)visibleCount++;
    Check(visibleCount==1&&canvas.Visible&&canvas.Parent==window&&canvas.Bounds==window.ClientRectangle,"only the interactive cube canvas remains visible");
    Check(!((ToolTip)Field(window,"tips")).Active,"compact mode suppresses tooltip overlays");
    Check(p.Orientation.X==preserved.X&&p.Orientation.Y==preserved.Y&&p.Orientation.Z==preserved.Z&&p.Orientation.W==preserved.W&&p.Strength==preservedStrength&&p.Grid==preservedGrid&&p.Zoom==preservedZoom,"entering compact mode preserves pose perspective mesh and zoom");
    window.Size=new Size(96,96);Layout(window);Check(window.Width==96&&window.Height==96&&canvas.Size==window.ClientSize,"compact window shrinks to 96 by 96");
    p.Zoom=.46;for(int i=0;i<24;i++)Invoke(canvas,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,40,40,-120));Check(p.Zoom<.08&&p.Zoom>=.05&&p.Strength==preservedStrength,"wheel shrinks cube well below the previous limit without changing perspective");
    p.Zoom=preservedZoom;
    window.Size=new Size(260,260);Layout(window);
    using(Bitmap bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(Path.Combine(output,"window-compact.png"));}
    Message escapeMessage=Message.Create(IntPtr.Zero,0,IntPtr.Zero,IntPtr.Zero);object[] escapeArgs={escapeMessage,Keys.Escape};bool handled=(bool)window.GetType().GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,escapeArgs);
    Check(handled&&!p.Compact&&window.FormBorderStyle==FormBorderStyle.Sizable&&((Panel)Field(window,"options")).Visible&&window.Bounds==expandedBounds,"Escape restores the complete original window");
    Check(p.Orientation.X==preserved.X&&p.Strength==preservedStrength&&p.Grid==preservedGrid,"restoring full window keeps cube settings unchanged");
    p.Strength=100;p.Shade=24;p.ResetPose();using(CubeCanvas sample=new CubeCanvas(p)){sample.Size=new Size(420,340);Layout(sample);using(Bitmap bitmap=new Bitmap(420,340)){sample.DrawToBitmap(bitmap,new Rectangle(0,0,420,340));bitmap.Save(Path.Combine(output,"cube-strong.png"));}
    p.Strength=0;using(Bitmap bitmap=new Bitmap(420,340)){sample.DrawToBitmap(bitmap,new Rectangle(0,0,420,340));bitmap.Save(Path.Combine(output,"cube-weak.png"));}}
   }
   string settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"偏好设置.xml");
   try{Preferences saved=new Preferences();saved.ResetPose();saved.Compact=true;saved.Width=saved.Height=96;saved.FullWidth=520;saved.FullHeight=700;saved.FullLeft=40;saved.FullTop=50;saved.Strength=0;saved.Grid=true;saved.Shade=90;saved.Zoom=.06;PreferenceStore.Save(settingsPath,saved);
    using(MainWindow reopened=new MainWindow()){
     Preferences restored=(Preferences)Field(reopened,"settings");CubeCanvas restoredCanvas=(CubeCanvas)Field(reopened,"canvas");
     Check(reopened.FormBorderStyle==FormBorderStyle.None&&reopened.Size==new Size(96,96)&&restoredCanvas.Parent==reopened&&restored.Strength==0&&restored.Zoom==.06&&restored.Grid&&restored.Shade==90,"reopening compact window preserves small size and all cube preferences");
     Invoke(reopened,"ToggleCompact");Check(reopened.Size==new Size(520,700)&&!restored.Compact,"reopened compact window can restore its full layout");
    }
   }finally{if(File.Exists(settingsPath))File.Delete(settingsPath);}
   Console.WriteLine("RESULT window handler and layout checks passed");return 0;
  }catch(Exception e){Console.WriteLine(e.ToString());return 1;}
 }
}
