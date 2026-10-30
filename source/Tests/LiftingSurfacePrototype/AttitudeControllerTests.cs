using System;
using UnityEngine;
using WildBlueCore.PartModules.Aero;

static class AttitudeControllerTests
{
    static int checks;
    static void Check(bool condition,string message) {if(!condition)throw new Exception(message); checks++;}
    static Vector3 Torque(Vector3 error,Vector3 omega,Vector3 inertia,float cap=100)
        =>ParafoilAttitudeMath.CalculateTorque(error,omega,Vector3.zero,Quaternion.identity,inertia,2,4,35*Mathf.Deg2Rad,Vector3.zero,cap);
    public static void Run()
    {
        Check(ParafoilAttitudeMath.UpdateLiftAuthority(0,150,1600,0.02f,4,0.05f)>0,"Introduce limited recovery support even during the logged handoff tumble");
        Check(ParafoilAttitudeMath.UpdateLiftAuthority(0,10,5,0.02f,4,0.05f)>0,"Introduce full wing authority once attitude and rate settle");
        Check(ParafoilAttitudeMath.UpdateLiftAuthority(0.5f,45,40,0.02f,4,0.05f)==0.5f,"Hold lift authority through moderate disturbance");
        Check(ParafoilAttitudeMath.UpdateLiftAuthority(0.5f,10,120,0.02f,4,0.05f)<0.5f,"Unload wing during excessive rotation");
        float authority=0;
        for(int i=0;i<210;i++)authority=ParafoilAttitudeMath.UpdateLiftAuthority(authority,10,5,0.02f,4,0.05f);
        Check(authority==1,"Settled wing reaches configured deployed lift after ramp");
        authority=0;
        for(int i=0;i<210;i++)authority=ParafoilAttitudeMath.UpdateLiftAuthority(authority,100,12,0.02f,4,0.05f);
        Check(MathF.Abs(authority-0.05f)<0.00001f,"Logged 100-degree persistent error cannot leave the wing disabled indefinitely");
        Quaternion target;
        Check(ParafoilAttitudeMath.TryTargetRotation(Vector3.forward,Vector3.up,Vector3.up,Vector3.forward,0,0,out target),"Valid Mk18 local axes");
        Check((target*Vector3.up-Vector3.forward).magnitude<0.001f,"Part +Y follows requested course");
        Check((target*Vector3.forward-Vector3.up).magnitude<0.001f,"Part +Z supplies upward wing normal");
        Check(ParafoilAttitudeMath.TryTargetRotation(Vector3.forward,Vector3.up,Vector3.up,Vector3.forward*(-1),0,0,out target),"Valid standard KSP control-reference axes");
        Check((target*Vector3.up-Vector3.forward).magnitude<0.001f,"KSP reference +Y follows requested course");
        Check((target*(Vector3.forward*(-1))-Vector3.up).magnitude<0.001f,"KSP reference -Z supplies dorsal/up direction");
        Check(ParafoilAttitudeMath.TryTargetRotation(Vector3.forward,Vector3.up,Vector3.up,Vector3.forward,10,30,out target),"Pitched bank target");
        Check(Vector3.Dot(target*Vector3.up,Vector3.up)>0,"Positive pitch raises nose");
        Check(Vector3.Dot(target*Vector3.forward,Vector3.right)>0,"Positive right turn tilts wing normal right");
        Check(!ParafoilAttitudeMath.TryTargetRotation(Vector3.up,Vector3.up,Vector3.up,Vector3.forward,0,0,out target),"Reject vertical heading");
        Check(!ParafoilAttitudeMath.TryTargetRotation(Vector3.forward,Vector3.up,Vector3.up,Vector3.up,0,0,out target),"Reject parallel configured axes");

        var rotation=Quaternion.AngleAxis(15,Vector3.up);
        var equivalent=new Quaternion(-rotation.x,-rotation.y,-rotation.z,-rotation.w);
        Check(ParafoilAttitudeMath.RotationError(rotation,equivalent).magnitude<0.001f,"Quaternion sign does not cause a full revolution");
        var shortError=ParafoilAttitudeMath.RotationError(Quaternion.AngleAxis(179,Vector3.up),Quaternion.AngleAxis(-179,Vector3.up));
        Check(MathF.Abs(shortError.magnitude* Mathf.Rad2Deg-2)<0.01f,"Heading wrap takes shortest path");
        var inertia=new Vector3(2,4,8);
        var omega=new Vector3(0.1f,-0.2f,0.3f);
        var damping=Torque(Vector3.zero,omega,inertia);
        Check(Vector3.Dot(damping,omega)<0,"Neutral attitude damps existing rotation");
        var capped=Torque(new Vector3(10,10,10),new Vector3(-100,50,90),inertia,5);
        Check(capped.magnitude<=5.001f,"Torque stays within configured limit");
        Check(Torque(Vector3.up,Vector3.zero,inertia,0)==Vector3.zero,"Zero limit disables torque");
        Check(Torque(new Vector3(float.NaN,0,0),Vector3.zero,inertia)==Vector3.zero,"Invalid error cannot reach physics");
        var reference=Quaternion.AngleAxis(90,Vector3.up);
        var acceleration=Vector3.right;
        var frameTorque=ParafoilAttitudeMath.CalculateTorque(Vector3.zero,acceleration*(-0.25f),Vector3.zero,reference,inertia,2,4,1,Vector3.zero,100);
        Check((frameTorque-new Vector3(8,0,0)).magnitude<0.001f,"Inertia is transformed from vessel reference frame to world frame");
        var ff=new Vector3(0,0.1f,0);
        Check(ParafoilAttitudeMath.CalculateTorque(Vector3.zero,ff,ff,Quaternion.identity,inertia,2,4,1,Vector3.zero,100)==Vector3.zero,"Established coordinated turn rate needs no damping torque");

        // Closed-loop rigidbody model: recover an attitude error with initial spin,
        // then repeat with ten times the inertia. This checks feedback sign and scaling.
        foreach(float scale in new[]{1f,10f})
        {
            var bodyInertia=inertia*scale;
            var current=Quaternion.AngleAxis(85,new Vector3(1,2,3).normalized);
            var angularVelocity=new Vector3(-0.2f,0.15f,0.1f);
            const float dt=0.02f;
            for(int i=0;i<1000;i++)
            {
                var error=ParafoilAttitudeMath.RotationError(current,Quaternion.identity);
                var torque=Torque(error,angularVelocity,bodyInertia);
                var angularAcceleration=new Vector3(torque.x/bodyInertia.x,torque.y/bodyInertia.y,torque.z/bodyInertia.z);
                angularVelocity=angularVelocity+angularAcceleration*dt;
                if(angularVelocity.magnitude>1e-7f)
                    current=Quaternion.AngleAxis(angularVelocity.magnitude*Mathf.Rad2Deg*dt,angularVelocity.normalized)*current;
            }
            Check(ParafoilAttitudeMath.RotationError(current,Quaternion.identity).magnitude<0.002f,"Closed-loop attitude converges at inertia scale "+scale);
            Check(angularVelocity.magnitude<0.002f,"Released controls settle rotation at inertia scale "+scale);
        }
        // Follow a moving heading target, then release the turn and recover level
        // flight. Unlike a static attitude test this exercises yaw feed-forward.
        var pose=Quaternion.identity;
        var spin=Vector3.zero;
        var heading=Vector3.forward;
        const float step=0.02f;
        for(int i=0;i<1000;i++)
        {
            float rate=i<250?5*Mathf.Deg2Rad:0;
            heading=Quaternion.AngleAxis(rate*Mathf.Rad2Deg*step,Vector3.up)*heading;
            ParafoilAttitudeMath.TryTargetRotation(heading,Vector3.up,Vector3.forward,Vector3.up,0,i<250?25:0,out target);
            var error=ParafoilAttitudeMath.RotationError(pose,target);
            var torque=ParafoilAttitudeMath.CalculateTorque(error,spin,Vector3.up*rate,Quaternion.identity,inertia,2,4,35*Mathf.Deg2Rad,Vector3.zero,100);
            spin=spin+new Vector3(torque.x/inertia.x,torque.y/inertia.y,torque.z/inertia.z)*step;
            if(spin.magnitude>1e-7f)pose=Quaternion.AngleAxis(spin.magnitude*Mathf.Rad2Deg*step,spin.normalized)*pose;
        }
        Check(ParafoilAttitudeMath.RotationError(pose,target).magnitude<0.002f,"Turn release holds the new heading and levels the frame");
        Check(spin.magnitude<0.002f,"Turn release arrests rotation");

        var unwound=ParafoilAttitudeMath.IntegrateTrim(new Vector3(20,0,0),new Vector3(-1,0,0),
            0.02f,2,20,new Vector3(90,0,0),Quaternion.identity,new Vector3(5,5,5),100);
        Check(unwound.x<20,"Saturated trim can unwind instead of remaining frozen");
        var blocked=ParafoilAttitudeMath.IntegrateTrim(new Vector3(10,0,0),new Vector3(1,0,0),
            0.02f,2,20,new Vector3(90,0,0),Quaternion.identity,new Vector3(5,5,5),100);
        Check(blocked.x==10,"Saturated trim does not integrate farther into actuator limit");

        // Regression for the flight failure: error was 80-175 degrees, inertia
        // about 3.3-5, and persistent moments balanced the old ~25 torque output.
        // Add opposing torque, grow it as lift comes in, and reverse it midflight.
        foreach(float timeStep in new[]{0.02f,0.08f})
        {
            var bodyInertia=new Vector3(4.9682f,3.2999f,4.8326f);
            var current=Quaternion.AngleAxis(170,Vector3.right);
            var angularVelocity=new Vector3(-1.7f,-0.85f,-2.68f);
            var trim=Vector3.zero;
            float wing=0,peakTorque=0,peakTrim=0,initialRecoveryTime=-1;
            int steps=(int)(60/timeStep);
            for(int i=0;i<steps;i++)
            {
                var error=ParafoilAttitudeMath.RotationError(current,Quaternion.identity);
                wing=ParafoilAttitudeMath.UpdateLiftAuthority(wing,error.magnitude*Mathf.Rad2Deg,
                    angularVelocity.magnitude*Mathf.Rad2Deg,timeStep,4,0.05f);
                var baseTorque=ParafoilAttitudeMath.CalculateTorque(error,angularVelocity,Vector3.zero,
                    Quaternion.identity,bodyInertia,2,4,35*Mathf.Deg2Rad,Vector3.zero,float.MaxValue);
                var rateError=ParafoilAttitudeMath.DesiredRate(error,Vector3.zero,2,35*Mathf.Deg2Rad)-angularVelocity;
                trim=ParafoilAttitudeMath.IntegrateTrim(trim,rateError,timeStep,2,20,baseTorque,
                    Quaternion.identity,bodyInertia,100);
                var torque=ParafoilAttitudeMath.CalculateTorque(error,angularVelocity,Vector3.zero,
                    Quaternion.identity,bodyInertia,2,4,35*Mathf.Deg2Rad,trim,100);
                peakTorque=MathF.Max(peakTorque,torque.magnitude);
                peakTrim=MathF.Max(peakTrim,trim.magnitude);
                float direction=i*timeStep<30?1:-1;
                var disturbance=new Vector3(30,5,-5)*(direction*(1+0.5f*wing));
                var net=torque+disturbance;
                angularVelocity=angularVelocity+new Vector3(net.x/bodyInertia.x,net.y/bodyInertia.y,net.z/bodyInertia.z)*timeStep;
                if(angularVelocity.magnitude>1e-7f)
                    current=Quaternion.AngleAxis(angularVelocity.magnitude*Mathf.Rad2Deg*timeStep,angularVelocity.normalized)*current;
                if(initialRecoveryTime<0 && i*timeStep<30 && wing>0.99f &&
                    ParafoilAttitudeMath.RotationError(current,Quaternion.identity).magnitude*Mathf.Rad2Deg<5 &&
                    angularVelocity.magnitude*Mathf.Rad2Deg<5)initialRecoveryTime=i*timeStep;
            }
            float finalError=ParafoilAttitudeMath.RotationError(current,Quaternion.identity).magnitude*Mathf.Rad2Deg;
            Console.WriteLine($"Disturbance test dt={timeStep}: recovery={initialRecoveryTime:F2}s, error={finalError:F3}deg, rate={angularVelocity.magnitude*Mathf.Rad2Deg:F3}deg/s, lift={wing:F3}, peak torque={peakTorque:F2}, peak trim={peakTrim:F2}");
            Check(initialRecoveryTime>=0 && initialRecoveryTime<25,"Initial recovery reaches full lift before the previous flight's ~27-second descent at dt="+timeStep);
            Check(finalError<2,"Recover attitude under persistent and reversed moments at dt="+timeStep);
            Check(angularVelocity.magnitude*Mathf.Rad2Deg<2,"Arrest rotation under persistent and reversed moments at dt="+timeStep);
            Check(wing>0.99f,"Recovery reaches full deployed lift at dt="+timeStep);
            Check(peakTorque<=100.001f&&peakTrim<=20.001f,"Output and trim retain hard limits at dt="+timeStep);
        }
        Console.WriteLine($"PASS: {checks} attitude-controller assertions, including closed-loop recovery with two inertias.");
    }
}
