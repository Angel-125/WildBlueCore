using System;
using UnityEngine;
using WildBlueCore.PartModules.Aero;

static class Program
{
    static int assertions;
    static void Check(bool value,string message)
    {
        if(!value) throw new Exception(message);
        assertions++;
    }
    static void Near(Vector3 actual,Vector3 expected,string message)=>Check((actual-expected).magnitude<0.001f,message);
    static WBIModuleLiftingSurface Create(bool named=false)
    {
        var m=new WBIModuleLiftingSurface();
        m.part.Rigidbody.velocity=new Vector3(20,0,10);
        if(named) {m.transformName="reference"; m.part.modelTransform=new Transform {position=new Vector3(0,10,0)};}
        m.OnStart(PartModule.StartState.Flying);
        return m;
    }
    static void Main()
    {
        var root=Create(); root.FixedUpdate();
        Check(root.part.forces.Count==2,"One lift and one drag force per direct physics invocation");
        Near(root.part.forces[0].position,root.part.CoLOffset,"Default lift point");
        Near(root.part.forces[1].position,root.part.CoPOffset,"Default drag point");
        var baselineLift=root.liftForce;

        var named=Create(true);
        named.part.Rigidbody.angularVelocity=new Vector3(1,0,0);
        named.FixedUpdate();
        Near(named.liftForce,baselineLift,"Named transform position must not displace root airflow sampling");
        named.part.modelTransform.position=new Vector3(100,-200,300);
        named.part.modelTransform.rotation=Quaternion.Euler(new Vector3(90,0,0));
        named.part.forces.Clear(); named.FixedUpdate();
        Near(named.liftForce,baselineLift,"Captured orientation ignores subsequent animation");
        named.followTransformRotation=true; named.FixedUpdate();
        Check((named.liftForce-baselineLift).magnitude>1,"Live rotation is an explicit opt-in");

        var oriented=new WBIModuleLiftingSurface {transformName="reference"};
        oriented.part.modelTransform=new Transform {rotation=Quaternion.Euler(new Vector3(0,90,0))};
        oriented.part.Rigidbody.velocity=new Vector3(20,0,10);
        oriented.OnStart(PartModule.StartState.Flying); oriented.FixedUpdate();
        Check((oriented.liftForce-baselineLift).magnitude>1,"Named orientation is captured at startup");
        var offset=Create(); offset.orientationOffset=new Vector3(0,90,0); offset.FixedUpdate();
        Near(oriented.liftForce,offset.liftForce,"Explicit orientation offset matches equivalent authored reference");
        var missing=new WBIModuleLiftingSurface {transformName="missing"};
        missing.part.Rigidbody.velocity=new Vector3(20,0,10);
        missing.OnStart(PartModule.StartState.Flying); missing.FixedUpdate();
        Near(missing.liftForce,baselineLift,"Missing named reference falls back to root");

        var sampled=Create(true);
        sampled.part.Rigidbody.angularVelocity=new Vector3(1,0,0);
        sampled.velocitySampleOffset=new Vector3(0,10,0);
        sampled.FixedUpdate();
        Check((sampled.liftForce-baselineLift).magnitude>1,"Explicit sample offset includes angular point velocity");
        Near(sampled.part.forces[0].position,sampled.part.CoLOffset,"Sampling offset leaves force point unchanged");

        var located=Create();
        located.part.partTransform.position=new Vector3(7,8,9);
        located.part.partTransform.rotation=Quaternion.Euler(new Vector3(0,90,0));
        located.usePartLiftOffset=located.usePartDragOffset=false;
        located.liftForceOffset=new Vector3(2,0,0); located.dragForceOffset=new Vector3(0,0,3);
        located.FixedUpdate();
        Near(located.part.forces[0].position,new Vector3(7,8,7),"Lift offset follows part rotation/translation");
        Near(located.part.forces[1].position,new Vector3(10,8,9),"Independent drag offset follows part frame");
        var query=new CenterOfLiftQuery {refVector=new Vector3(20,0,10)};
        var savedLift=located.liftScalar;
        ((ILiftProvider)located).OnCenterOfLiftQuery(query);
        Near(query.pos,located.part.forces[0].position,"ILiftProvider uses actual force point");
        Check(located.liftScalar==savedLift,"CoL query preserves flight scalar");
        Near(query.dir,new Vector3(-1,0,0),"Captured root normal rotates with the part");

        foreach(var reason in new[]{"coefficient","shielded","packed","no-rigidbody","zero-velocity","invalid-adjuster","not-flight"})
        {
            var m=Create(); m.FixedUpdate(); m.part.forces.Clear();
            switch(reason)
            {
                case "coefficient":m.deflectionLiftCoeff=0;break;
                case "shielded":m.part.ShieldedFromAirstream=true;break;
                case "packed":m.part.packed=true;break;
                case "no-rigidbody":m.part.Rigidbody=null;break;
                case "zero-velocity":m.part.Rigidbody.velocity=Vector3.zero;break;
                case "invalid-adjuster":m.invalidAdjuster=true;break;
                case "not-flight":HighLogic.LoadedSceneIsFlight=false;break;
            }
            m.FixedUpdate();
            Check(m.part.forces.Count==0,"No forces when "+reason);
            if(reason!="not-flight") Check(m.liftForce==Vector3.zero&&m.dragForce==Vector3.zero&&m.liftScalar==0&&m.dragScalar==0,"No stale force diagnostics when "+reason);
            HighLogic.LoadedSceneIsFlight=true;
        }
        var noDrag=Create(); noDrag.FixedUpdate(); noDrag.part.forces.Clear(); noDrag.useInternalDragModel=false; noDrag.FixedUpdate();
        Check(noDrag.part.forces.Count==1&&noDrag.dragForce==Vector3.zero&&noDrag.dragScalar==0,"Disabling internal drag clears previous drag");
        AttitudeControllerTests.Run();
        Console.WriteLine($"PASS: {assertions} assertions. Engine doubles test orchestration; Unity dispatch and stock aerodynamics require KSP flight testing.");
    }
}
