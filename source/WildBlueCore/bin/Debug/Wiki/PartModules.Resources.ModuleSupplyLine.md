            
Derived from ModuleFuelPup, this part module provides periodic refills of the resources contained in the storage tank. The storage tank can be either the part that hosts this part module, or, if the host part has no resources, then the tank is the part that the supply line part is attached to. It makes the assumption that the storage tank is completely full when it arrives at the desired destination; how it gets there is up to the player. The part module allows players to specify how long, in hours, it takes between supply runs. It also can optionally charge for the cost of the resources upon delivery. When a delivery is made, the part module can play an EFFECT and/or run an animation.
        
## Fields

### transfersEnabled
Flag to enable periodic transfers. Every transferPeriod, the fuel pump will immediately refill the tank and distribute the contents
### transferTime
In hours, how long to wait before magically refilling the tank and distributing the contents.
### lastUpdated
Last time the pump was updated.
### isRecordingTime
Flag indicating that the pump is recording mission time.
### missionStartTime
Last time the pump was updated.
### missionStopTime
Last time the pump was updated.
### missionElapsedTime
In seconds, elapsed mission time.
### chargeForResources
Flag to indicate whether or not the player should be charged for resource deliveries
### payFlatFee
Flag to indicate whether or not the player should be charged a flat fee to deliver resources

