            
This module lets users rotate a seat in a part's IVA if the seat is occupied.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleSeatRotator
                
                // Name of the seat transform to rotate. This needs to be the same name as in the IVA's 3D model and in the IVA's config file.
                seatName = Seat001
                
                // The name of the prop that the kerbal sits on. This is optional.
                propName = NF_SEAT_Chair_Basic
                
                // If your list of props has more than one prop for the seats, then specify the index of the seat prop.
                propIndex = 2
                
                // The x, y, and z axis to rotate the prop by. The default is 0,0,1
                propRotationAxis = 0,1,0
            }
            
```

            
        
## Fields

### seatName
Name of the seat transform to rotate. This needs to be the same name as in the IVA's 3D model and in the IVA's config file. If you use a prop in addition to the seat transform, be sure to specify the propName and propIndex as well.
### propName
The name of the prop that the kerbals sit on. If the seat transform in your IVA's 3D model is NOT the same thing as the seat prop, then specify the propName as wel as the propIndex in order to rotate the prop along with the seat transform.
### propRotationAxis
The x, y, and z axis to rotate the prop by. The default is 0,0,1
### propIndex
If your list of props has more than one prop for the seats, then specify the index of the seat prop (as it appears in order in the config file) to rotate.
### rotationRate
Rate at which to rotate the seat, in degrees per second.
### rotationAmount
How far to rotate the seat when commanded to rotate the seat
## Methods


### RotateLeft
Rotates the seat to the left.

### RotateRight
Rotates the seat to the right.

