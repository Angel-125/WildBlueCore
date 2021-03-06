            
This part module pumps one or more resources from the host part to other parts that have the same resource. The module can be directly added to a resource tank part or to a part that is radially attached to a resource tank part. When enabled, ModuleFuelPump will automatically pump resources until either the host part's resource is empty or when the destination parts are full. In either case, it will wait until the host part gains more resources to pump or the destination parts gain more room to store the resource.
            
ModuleFuelPump will transfer resources based on a part's Flow Priority. Higher priority parts will receive resources before lower priority parts.  
            
ModuleFuelPump is designed to pump resources throughout the same vessel, but it can also pump resources to a nearby vessel if it is also equipped with a part that has a ModuleFuelPump.  
            
To pump a resource throughout the same vessel, the following conditions must be met:  
            * The fuel pump providing resources must be set to Enabled.  
            * The resource must not be empty.  
            * The resource must be transferrable and unlocked.  
            * The destination parts must have space available to receive the pumped resource.  
            * The destination parts' resource storage must be unlocked.  
            
To pump a resource to another nearby vessel, in addition to the above conditions, the following conditions must also be met:  
            * All vessels must be either landed or splashed.  
            * The nearby vessels must be within the provider's pump range.  
            * The pump providing resources must have its pump mode set to Remote.  
            * The pumps that receive resources must be set to Enabled.  
            
            
> #### Example
```

            MODULE
            {
                name = ModuleFuelPump
                maxRemotePumpRange = 200
            }
            
```

            
        
## Fields

### onPumpStateChanged
Signals when the isActivated and/or remotePumpMode changes.
### maxRemotePumpRange
In meters, the maximum range that the fuel pump can reach when remote pumping resources. Default is 200 meters.
## Methods


### ActionFuelPumpToggle(KSPActionParam)
Toggles the fuel pump on/off.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpOn(KSPActionParam)
Turns the fuel pump on
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpOff(KSPActionParam)
Turns the fuel pump off
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpModeToggle(KSPActionParam)
Toggles the pump mode from local to remote and vice-versa.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpModeLocal(KSPActionParam)
Sets the pump mode to local.
> #### Parameters
> **param:** 


### ActionFuelPumpModeRemote(KSPActionParam)
Sets the pump mode to remote.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### DistributeResources
This method will attempt to distribute any resources that the host part has to other parts in the vessel or to nearby vessels. The resources must be capable of being transferred, and they must be unlocked. Additionally, to remotely distribute the resources, remotePumpMode must be set to true, the nearby vessel must have at least one ModuleFuelPump, and the nearby vessel's fuel pump' isActivated must be set to true.

### DistributeResourceLocally(PartResource,System.Double,System.Boolean)
Distributes the desired resource locally throughout the vessel.
> #### Parameters
> **resource:** The PartResource to distribute.

> **transferAmount:** A double containing how much of the resource to distrubute.

> **isFromRemotePump:** A bool indicating whether or not the source is from a remote pump. Default is false.

> #### Return value
> True if the distribution was successful, false if not.

