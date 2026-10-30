// Minimal engine boundary for testing the production module's orchestration.
// These doubles do not validate Unity message dispatch, stock curves, or KSP flight dynamics.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public class Object { public static void Destroy(object value) { } }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 forward => new Vector3(0,0,1);
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 right => new Vector3(1,0,0);
        public float sqrMagnitude => x*x+y*y+z*z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 0 ? this*(1/magnitude) : zero;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x*b,a.y*b,a.z*b);
        public static bool operator ==(Vector3 a, Vector3 b) => (a-b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a==b);
        public override bool Equals(object other) => other is Vector3 v && this==v;
        public override int GetHashCode() => HashCode.Combine(x,y,z);
        public static float Dot(Vector3 a, Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 Scale(Vector3 a, Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static Vector3 ClampMagnitude(Vector3 v,float max)=>v.magnitude>max?v.normalized*max:v;
        public static Vector3 ProjectOnPlane(Vector3 v,Vector3 n)=>n.sqrMagnitude>0?v-n*(Dot(v,n)/n.sqrMagnitude):v;
        public string ToString(string format) => $"({x},{y},{z})";
    }
    public struct Quaternion
    {
        internal System.Numerics.Quaternion value;
        public float x=>value.X;
        public float y=>value.Y;
        public float z=>value.Z;
        public float w=>value.W;
        public Quaternion(float x,float y,float z,float w) {value=new System.Numerics.Quaternion(x,y,z,w);}
        public static Quaternion AngleAxis(float degrees,Vector3 axis)=>new Quaternion {value=System.Numerics.Quaternion.CreateFromAxisAngle(new System.Numerics.Vector3(axis.normalized.x,axis.normalized.y,axis.normalized.z),degrees*MathF.PI/180)};
        public void ToAngleAxis(out float angle,out Vector3 axis)
        {
            var q=System.Numerics.Quaternion.Normalize(value);
            angle=2*MathF.Acos(Math.Clamp(q.W,-1,1))*180/MathF.PI;
            float s=MathF.Sqrt(MathF.Max(0,1-q.W*q.W));
            axis=s<1e-6f?Vector3.right:new Vector3(q.X/s,q.Y/s,q.Z/s);
        }
        public static Quaternion LookRotation(Vector3 forward,Vector3 up)
        {
            forward=forward.normalized;
            var right=Vector3.Cross(up,forward).normalized;
            up=Vector3.Cross(forward,right);
            var matrix=new System.Numerics.Matrix4x4(right.x,right.y,right.z,0,up.x,up.y,up.z,0,forward.x,forward.y,forward.z,0,0,0,0,1);
            return new Quaternion {value=System.Numerics.Quaternion.CreateFromRotationMatrix(matrix)};
        }
        public static Quaternion identity => new Quaternion {value=System.Numerics.Quaternion.Identity};
        public static Quaternion Inverse(Quaternion q) => new Quaternion {value=System.Numerics.Quaternion.Inverse(q.value)};
        public static Quaternion Euler(Vector3 e) => new Quaternion {value=System.Numerics.Quaternion.CreateFromYawPitchRoll(e.y*MathF.PI/180,e.x*MathF.PI/180,e.z*MathF.PI/180)};
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion {value=a.value*b.value};
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var r=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(v.x,v.y,v.z),q.value);
            return new Vector3(r.X,r.Y,r.Z);
        }
    }
    public class Transform
    {
        public string name="test";
        public GameObject gameObject=new GameObject();
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation=Quaternion.identity;
        public Vector3 position {get=>parent==null?localPosition:parent.TransformPoint(localPosition);set=>localPosition=parent==null?value:parent.InverseTransformPoint(value);}
        public Quaternion rotation {get=>parent==null?localRotation:parent.rotation*localRotation;set=>localRotation=parent==null?value:Quaternion.Inverse(parent.rotation)*value;}
        public bool IsChildOf(Transform other) {for(var node=this;node!=null;node=node.parent)if(node==other)return true;return false;}
        public Vector3 TransformPoint(Vector3 point) => position+rotation*point;
        public Vector3 InverseTransformPoint(Vector3 point) => Quaternion.Inverse(rotation)*(point-position);
    }
    public class GameObject {public bool activeSelf=true;public void SetActive(bool active){activeSelf=active;}}
    public class Rigidbody
    {
        public Vector3 velocity, angularVelocity, worldCenterOfMass;
        public Vector3 GetPointVelocity(Vector3 point) => velocity+Vector3.Cross(angularVelocity,point-worldCenterOfMass);
    }
    public struct Color { public static Color blue => new Color(); public static Color red => new Color(); }
    public static class Mathf { public const float Deg2Rad=MathF.PI/180,Rad2Deg=180/MathF.PI; public static float Abs(float v)=>MathF.Abs(v); public static float Clamp01(float v)=>Math.Clamp(v,0,1); public static float Max(float a,float b)=>MathF.Max(a,b); public static float MoveTowards(float current,float target,float delta)=>MathF.Abs(target-current)<=delta?target:current+MathF.Sign(target-current)*delta; }
    public static class Time { public static float realtimeSinceStartup, fixedTime; }
    public static class Debug { public static void Log(object value) { } public static void LogWarning(object value) { } }
}
public struct Vector3d
{
    Vector3 value;
    public static explicit operator Vector3d(Vector3 v)=>new Vector3d{value=v};
    public static explicit operator Vector3(Vector3d v)=>v.value;
}
public class KSPField : Attribute { }
public class PartModule { public enum StartState { Flying } public virtual void OnStart(StartState state) { } }
public class BaseField { public bool guiActive; }
public static class HighLogic { public static bool LoadedSceneIsFlight=true; }
public static class PhysicsGlobals { public static bool AeroDataDisplay,AeroForceDisplay; public static float AeroForceDisplayScale=1; }
public static class Krakensbane { public static Vector3 GetFrameVelocityV3f()=>Vector3.zero; }
public class CelestialBody { public double GetSpeedOfSound(double pressure,double density)=>300; }
public static class FlightGlobals { public static CelestialBody GetHomeBody()=>new CelestialBody(); }
public class Vessel { public Vector3d CurrentCoM; public CelestialBody mainBody=new CelestialBody(); }
public class CenterOfLiftQuery { public Vector3 pos,dir,refVector; public float lift; public double refStaticPressure,refAirDensity=1; }
public interface ILiftProvider { bool DisableBodyLift {get;} bool IsLifting {get;} void OnCenterOfLiftQuery(CenterOfLiftQuery query); }
public class Part
{
    public string name="testPart";
    public Transform partTransform=new Transform();
    public Transform modelTransform;
    public Rigidbody Rigidbody=new Rigidbody();
    public bool packed,ShieldedFromAirstream;
    public double submergedPortion,dynamicPressurekPa=1,submergedDynamicPressurekPa,submergedLiftScalar=1,submergedDragScalar=1,machNumber;
    public Vector3 CoLOffset=new Vector3(1,2,3),CoPOffset=new Vector3(4,5,6);
    public List<(Vector3 force,Vector3 position)> forces=new List<(Vector3,Vector3)>();
    public Transform FindModelTransform(string name)=>modelTransform;
    public void AddForceAtPosition(Vector3d force,Vector3d position)=>forces.Add(((Vector3)force,(Vector3)position));
}
public class ArrowPointer
{
    public object gameObject=new object();
    public Vector3 Offset,Direction;
    public float Length;
    public static ArrowPointer Create(Transform parent,Vector3 offset,Vector3 force,float length,Color color,bool worldSpace)=>new ArrowPointer();
}
public class ModuleLiftingSurface : PartModule, ILiftProvider
{
    public enum TransformDir { X,Y,Z }
    public TransformDir transformDir=TransformDir.Z;
    public string transformName="";
    public float deflectionLiftCoeff=1,transformSign=1,liftScalar,dragScalar,liftDot;
    public bool displaceVelocity,useInternalDragModel=true,omnidirectional=true,perpendicularOnly;
    public Vector3 velocityOffset,liftForce,dragForce;
    public Part part=new Part();
    public Vessel vessel=new Vessel();
    protected Vector3 nVel,liftVector,pointVelocity;
    protected float absDot;
    protected double Qlift,Qdrag;
    protected Transform baseTransform;
    protected BaseField liftField=new BaseField(),dragField=new BaseField();
    public ArrowPointer liftArrow,dragArrow;
    public bool invalidAdjuster;
    public bool DisableBodyLift=>true;
    public bool IsLifting=>liftScalar!=0;
    public override void OnStart(StartState state) {baseTransform=string.IsNullOrEmpty(transformName)?part.partTransform:part.FindModelTransform(transformName)??part.partTransform;}
    public void FixedUpdate() {throw new Exception("Stock physics loop must not be called by prototype");}
    public void OnCenterOfLiftQuery(CenterOfLiftQuery query) {throw new Exception("Stock CoL query bypassed prototype");}
    public Vector3 GetLiftVector(Vector3 normal,float dot,float abs,double q,float mach)
    {
        liftScalar=(float)q*dot*deflectionLiftCoeff;
        var force=normal*(-liftScalar);
        return perpendicularOnly?force-nVel*Vector3.Dot(force,nVel):force;
    }
    public Vector3 GetDragVector(Vector3 velocity,float abs,double q) {dragScalar=(float)q*abs*deflectionLiftCoeff; return velocity*(-dragScalar);}
    protected Vector3 ApplyLiftForceAdjustments(Vector3 force)=>invalidAdjuster?new Vector3(float.NaN,0,0):force;
}
