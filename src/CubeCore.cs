using System;
using System.IO;
using System.Xml.Serialization;

namespace CubePractice {
 public struct V3 {
  public double X,Y,Z;
  public V3(double x,double y,double z){X=x;Y=y;Z=z;}
  public double Length {get{return Math.Sqrt(X*X+Y*Y+Z*Z);}}
  public static V3 operator +(V3 a,V3 b){return new V3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);}
  public static V3 operator -(V3 a,V3 b){return new V3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
  public static V3 operator *(V3 a,double b){return new V3(a.X*b,a.Y*b,a.Z*b);}
  public static double Dot(V3 a,V3 b){return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
  public static V3 Cross(V3 a,V3 b){return new V3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);}
 }
 public struct Rotation {
  public double X,Y,Z,W;
  public Rotation(double x,double y,double z,double w){X=x;Y=y;Z=z;W=w;}
  public static Rotation Identity {get{return new Rotation(0,0,0,1);}}
  public Rotation Normalized(){double n=Math.Sqrt(X*X+Y*Y+Z*Z+W*W);if(double.IsNaN(n)||double.IsInfinity(n)||n<1e-12)return Identity;return new Rotation(X/n,Y/n,Z/n,W/n);}
  public static Rotation Axis(V3 axis,double angle){double n=axis.Length;if(n<1e-12||double.IsNaN(angle)||double.IsInfinity(angle))return Identity;double s=Math.Sin(angle/2)/n;return new Rotation(axis.X*s,axis.Y*s,axis.Z*s,Math.Cos(angle/2));}
  public static Rotation operator *(Rotation a,Rotation b){return new Rotation(a.W*b.X+a.X*b.W+a.Y*b.Z-a.Z*b.Y,a.W*b.Y-a.X*b.Z+a.Y*b.W+a.Z*b.X,a.W*b.Z+a.X*b.Y-a.Y*b.X+a.Z*b.W,a.W*b.W-a.X*b.X-a.Y*b.Y-a.Z*b.Z);}
  public V3 Apply(V3 v){Rotation q=Normalized();V3 u=new V3(q.X,q.Y,q.Z);V3 t=V3.Cross(u,v)*2;return v+t*q.W+V3.Cross(u,t);}
 }
 public static class Geometry {
  public static double Clamp(double x,double min,double max,double fallback){if(double.IsNaN(x)||double.IsInfinity(x))return fallback;return Math.Max(min,Math.Min(max,x));}
  public static double Distance(int strength){double t=1-Math.Max(0,Math.Min(100,strength))/100.0;return 3+27*t*t;}
  public static V3 Project(V3 point,double distance){double d=Clamp(distance,3,1000,8),factor=d/(d-point.Z);return new V3(point.X*factor,point.Y*factor,point.Z);}
 }
 public class Preferences {
  public Rotation Orientation=Rotation.Identity;
  public double Zoom=1,PanX=0,PanY=0;
  public int Strength=60,Shade=24,Width=456,Height=599,Left=-1,Top=-1;
  public int FullWidth=456,FullHeight=599,FullLeft=-1,FullTop=-1;
  public bool Pinned=false,Grid=true,RearEdges=true,Compact=false;
  public void Sanitize(){Orientation=Orientation.Normalized();Zoom=Geometry.Clamp(Zoom,.05,2.4,1);PanX=Geometry.Clamp(PanX,-.65,.65,0);PanY=Geometry.Clamp(PanY,-.65,.65,0);Strength=Math.Max(0,Math.Min(100,Strength));Shade=Math.Max(0,Math.Min(90,Shade));Width=Math.Max(Compact?96:340,Math.Min(2000,Width));Height=Math.Max(Compact?96:470,Math.Min(1800,Height));FullWidth=Math.Max(340,Math.Min(2000,FullWidth));FullHeight=Math.Max(470,Math.Min(1800,FullHeight));}
  public void ResetPose(){Orientation=(Rotation.Axis(new V3(1,0,0),.43)*Rotation.Axis(new V3(0,1,0),-.63)).Normalized();Zoom=1;PanX=PanY=0;}
 }
 public static class PreferenceStore {
  public static Preferences Load(string file){try{using(FileStream stream=File.OpenRead(file)){Preferences p=(Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(stream);if(p!=null){p.Sanitize();return p;}}}catch(IOException){}catch(InvalidOperationException){}catch(UnauthorizedAccessException){}Preferences result=new Preferences();result.ResetPose();return result;}
  public static void Save(string file,Preferences value){value.Sanitize();string temp=file+".new";using(FileStream stream=new FileStream(temp,FileMode.Create,FileAccess.Write)){new XmlSerializer(typeof(Preferences)).Serialize(stream,value);}if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
 }
}
