            
Stock lifting-surface curves with independent orientation, velocity sampling, and force application. ModuleParafoil can manage this module as a primary wing.
        
## Fields

### followTransformRotation
Follow the named transform's rotation; otherwise capture its part-relative rotation at OnStart.
### orientationOffset
Additional Euler rotation in degrees relative to the selected aerodynamic reference.
### velocitySampleOffset
Airflow sampling position in part-local coordinates, independent of transformName.
### usePartLiftOffset
Use the part's CoLOffset instead of liftForceOffset.
### liftForceOffset
Lift application position in part-local coordinates when usePartLiftOffset is false.
### usePartDragOffset
Use the part's CoPOffset instead of dragForceOffset.
### dragForceOffset
Drag application position in part-local coordinates when usePartDragOffset is false.
### debugMode
Log observations of this module's force calculation.
### debugLogInterval
Seconds between diagnostic lines; zero logs every physics update.
## Methods


### OnStart(PartModule.StartState)
Load stock curves and capture the selected orientation relative to the part.

### FixedUpdate
Own the Unity physics message: never call base.FixedUpdate, which would apply a second set of forces at the stock positions.

### OnCenterOfLiftQuery(CenterOfLiftQuery)
Use the same orientation and lift application position for editor/ILiftProvider queries.

