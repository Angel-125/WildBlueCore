            
This module converts Power Units from Breaking Ground Science to Electric Charge and vice-versa.
            
            
> #### Example
```

            MODULE
            {
                name = ModulePowerUnitConverter
                isActive = true
                isConsuming = false
                ecPerPowerUnit = 0.25
                maxPowerUnitsProduced = 10
            }
            
```

            
        
## Fields

### isActive
Indicates whether or not the converter is running.
### isConsuming
Indicates whether or not the converter is consuming (true) or sharing (false).
### maxPowerAvailable
The maximum number of Power Units that the part may produce. This value ranges between 1 and maxPowerUnitsProduced.
### ecPerPowerUnit
In Breaking Ground Science, Power Unit is an integer, but resources like ElectricCharge use decimals. The default is 0.25, so 1.0 EC = 4 PU. This number was derived by comparing the size of the Breaking Ground Mini-NUK-PB RTG to the stock PB-NUK RTG, and looking how how much ElectricCharge the stock RTG produces. That actually gives us 0.375 (the Mini-NUK is about half as tall as the stock RTG), but we dropped that to 0.25 to make the math easier.
### maxPowerUnitsProduced
The maximum number of Power Units that the converter can provide. Note that this is an integer value. The default is 10. Multiply by ecPerPowerUnit to calculate how much ElectricCharge/sec that the power converter will consume. If you leave focus on the vessel and come back, then the E.C. will be drained accordingly.
### lastUpdated
Timestamp of the last time the module was updated.
## Properties

### CanDistributeEC
Indicates whether or not the power converter can distribute Electric Charge.
### CanConsumeEC
Indicates whether or not the power converter can consume Electric Charge.
## Methods


### GetPowerAvailable(System.Int32,System.Double)
Returns the number of Power Units available.
> #### Parameters
> **totalConverterCount:** An int containing the total number of converters to distribute power to.

> **deltaTime:** A double containing the current time duration.

> #### Return value
> An int containing the total Power Units available.

### DistributePower(System.Single)
Asks the converter to convert the supplied available power into Electric Charge and distribute it throughout the vessel.
> #### Parameters
> **availablePower:** An int containing the total Power Units to distribute.


