            
This part module pumps one or more resources from the host part to other parts that have the same resource. The module can be directly added to a resource tank part or to a part that is radially attached to a resource tank part. When enabled, WBIModuleFuelPump will automatically pump resources until either the host part's resource is empty or when the destination parts are full. In either case, it will wait until the host part gains more resources to pump or the destination parts gain more room to store the resource.
            
WBIModuleFuelPump will transfer resources based on a part's Flow Priority. Higher priority parts will receive resources before lower priority parts.  
            
WBIModuleFuelPump is designed to pump resources throughout the same vessel, but it can also pump resources to a nearby vessel if it is also equipped with a part that has a WBIModuleFuelPump.  
            
To pump a resource throughout the same vessel, the following conditions must be met:  
            * The fuel pump providing resources must be set to Distribute Localy.  
            * The resource must not be empty.  
            * The resource must be transferrable and unlocked.  
            * The destination parts must have space available to receive the pumped resource.  
            * The destination parts' resource storage must be unlocked.  
            
To pump a resource to another nearby vessel, the following conditions must be met:  
            * The resource must not be empty.  
            * The resource must be transferrable and unlocked.  
            * The destination parts must have space available to receive the pumped resource.  
            * The destination parts' resource storage must be unlocked.  
            * All vessels must be either landed or splashed.  
            * The nearby vessels must be within the provider's pump range.  
            * The pump providing resources must have its pump mode set to Send to remote.  
            * The pumps that receive resources must be set to Receive from remote.  
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleFuelPump
                maxRemotePumpRange = 200
            }
            
```

            
        
## Fields

### onPumpStateChanged
Signals when the isActivated and/or remotePumpMode changes.
### maxRemotePumpRange
In meters, the maximum range that the fuel pump can reach when remote pumping resources. Default is 2000 meters.
### selfIsHostPart
Flag to indicate that the part that has the WBIModuleFuelPump is the host part.
## Methods


### ActionFuelPumpOn(KSPActionParam)
Turns off the fuel pump.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpOff(KSPActionParam)
Turns off the fuel pump.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpLocal(KSPActionParam)
Sets pump mode to local distribution.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpRemoteSend(KSPActionParam)
Sets the pump mode to send to remote pumps.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpModeReceive(KSPActionParam)
Sets the pump mode to receive from remote pumps.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### DistributeResources(System.Single)
This method will attempt to distribute any resources that the host part has to other parts in the vessel or to nearby vessels. The resources must be capable of being transferred, and they must be unlocked. Additionally, to remotely distribute the resources, remotePumpMode must be set to true, the nearby vessel must have at least one WBIModuleFuelPump, and the nearby vessel's fuel pump' isActivated must be set to true.

### DistributeResourceLocally(PartResource,System.Double,System.Boolean)
Distributes the desired resource locally throughout the vessel.
> #### Parameters
> **resource:** The PartResource to distribute.

> **transferAmount:** A double containing how much of the resource to distrubute.

> **isFromRemotePump:** A bool indicating whether or not the source is from a remote pump. Default is false.

> #### Return value
> True if the distribution was successful, false if not.

