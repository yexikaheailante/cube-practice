using System;
using System.IO;
using CubePractice;

class CoreTests {
 static int passed=0,failed=0;
 static void Check(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Near(double actual,double expected){if(double.IsNaN(actual)||Math.Abs(actual-expected)>1e-8)throw new Exception("expected "+expected+", got "+actual);}
 static void True(bool condition,string message){if(!condition)throw new Exception(message);}
 static V3 Intersection(V3 a,V3 b,V3 c,V3 d){double x1=a.X,x2=b.X,x3=c.X,x4=d.X,y1=a.Y,y2=b.Y,y3=c.Y,y4=d.Y;double den=(x1-x2)*(y3-y4)-(y1-y2)*(x3-x4);return new V3(((x1*y2-y1*x2)*(x3-x4)-(x1-x2)*(x3*y4-y3*x4))/den,((x1*y2-y1*x2)*(y3-y4)-(y1-y2)*(x3*y4-y3*x4))/den,0);}
 static int Main(){
  Check("central projection enlarges near points",delegate{Near(Geometry.Project(new V3(1,2,1),6).X,1.2);Near(Geometry.Project(new V3(1,2,-1),6).X,6.0/7);Near(Geometry.Project(new V3(1,2,1),6).Y,2.4);});
  Check("strength changes near-far size ratio",delegate{double close=Geometry.Distance(100),far=Geometry.Distance(0);Near(close,3);Near(far,30);True((close+1)/(close-1)>(far+1)/(far-1),"perspective direction reversed");});
  Check("quarter turn is a rigid rotation",delegate{V3 v=Rotation.Axis(new V3(0,1,0),Math.PI/2).Apply(new V3(1,0,0));Near(v.X,0);Near(v.Y,0);Near(v.Z,-1);});
  Check("successive rotations preserve lengths",delegate{Rotation q=Rotation.Identity;for(int i=0;i<10000;i++)q=(Rotation.Axis(new V3(1,0,0),.01)*Rotation.Axis(new V3(0,1,0),-.013)*q).Normalized();Near(q.Apply(new V3(1,2,3)).Length,Math.Sqrt(14));Near(Math.Sqrt(q.X*q.X+q.Y*q.Y+q.Z*q.Z+q.W*q.W),1);});
  Check("parallel edges share the expected vanishing point",delegate{Rotation q=Rotation.Axis(new V3(0,1,0),Math.PI/4);V3 a=Geometry.Project(q.Apply(new V3(-1,1,1)),6),b=Geometry.Project(q.Apply(new V3(1,1,1)),6),c=Geometry.Project(q.Apply(new V3(-1,-1,-1)),6),d=Geometry.Project(q.Apply(new V3(1,-1,-1)),6);V3 vp=Intersection(a,b,c,d);Near(vp.X,6);Near(vp.Y,0);});
  Check("all cube corners stay finite at closest distance",delegate{for(int i=0;i<80;i++){Rotation q=(Rotation.Axis(new V3(0,1,0),i*.17)*Rotation.Axis(new V3(1,0,0),i*.23)).Normalized();for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){V3 v=q.Apply(new V3(x,y,z));Near(v.Length,Math.Sqrt(3));V3 p=Geometry.Project(v,Geometry.Distance(100));True(!double.IsNaN(p.X)&&!double.IsInfinity(p.X)&&Math.Abs(p.X)<5,"invalid projected corner");}}});
  Check("invalid saved values recover safely",delegate{Preferences p=new Preferences();p.Zoom=double.NaN;p.PanX=double.PositiveInfinity;p.Strength=999;p.Shade=-40;p.Width=0;p.Orientation=new Rotation(0,0,0,0);p.Sanitize();Near(p.Zoom,1);Near(p.PanX,0);True(p.Strength==100&&p.Shade==0&&p.Width>=340,"not sanitized");Near(p.Orientation.W,1);});
  Check("preferences persist pose and pin choice",delegate{string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"roundtrip.xml");Preferences p=new Preferences();p.Orientation=Rotation.Axis(new V3(1,0,0),.8);p.Pinned=true;p.PanX=.12;p.Strength=74;PreferenceStore.Save(file,p);Preferences loaded=PreferenceStore.Load(file);True(loaded.Pinned&&loaded.Strength==74,"choices not restored");Near(loaded.PanX,.12);Near(loaded.Orientation.X,Math.Sin(.4));File.Delete(file);});
  Check("corrupt settings fall back to usable defaults",delegate{string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"corrupt.xml");File.WriteAllText(file,"<bad");Preferences p=PreferenceStore.Load(file);True(p.Width>=340&&p.Zoom>0,"no usable fallback");File.Delete(file);});
  Check("null settings XML falls back to defaults",delegate{string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"null.xml");File.WriteAllText(file,"<Preferences xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />");Preferences p=PreferenceStore.Load(file);True(p!=null&&p.Zoom>0,"null preferences");File.Delete(file);});
  Check("small display zoom is preserved without changing perspective",delegate{Preferences p=new Preferences();p.Zoom=.06;p.Strength=0;p.Sanitize();Near(p.Zoom,.06);True(p.Strength==0,"perspective changed with zoom");});
  Check("compact window can persist at 96 pixels",delegate{Preferences p=new Preferences();p.Compact=true;p.Width=96;p.Height=96;p.Sanitize();True(p.Width==96&&p.Height==96,"compact size was enlarged");});
  Console.WriteLine("RESULT "+passed+" passed, "+failed+" failed");return failed==0?0:1;
 }
}
