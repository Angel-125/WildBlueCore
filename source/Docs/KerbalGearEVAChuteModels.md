# Wearable EVA parachute display

`WBIModuleWearableItem.evaChuteModel` controls both packed chute visibility and model selection for carried back/backOrJetpack wearables. It replaces the removed `showChuteTransforms` field.

| evaChuteModel | Behavior |
| --- | --- |
| None (default) | No chute visibility exception; follow stock visibility and hideStockPacksWhenWorn. |
| Stock | Preserve an eligible carried chute using stock inventory-count/jetpack model selection. |
| Standalone | Preserve ChuteStTransform, against the Kerbal's body. |
| Jetpack | Preserve ChuteJetpackTransform. |
| Container | Preserve ChuteContainerTransform. |

Values are case-insensitive. Invalid values warn and fall back to None. Stock inventory and crew-eligibility checks still govern availability. None does not remove the parachute or force it invisible when stock packs are allowed.

## Configuration

Defaults:

```cfg
hideStockPacksWhenWorn = false
evaChuteModel = None
```

The live paramotor uses:

```cfg
hideStockPacksWhenWorn = true
evaChuteModel = Standalone
```

Diving packs explicitly use hideStockPacksWhenWorn=true and inherit None, hiding stock packs and the packed chute. Ordinary accessories inherit false/None and leave stock visibility unchanged. Back attachment alone never implies hiding. No SunkWorks compatibility patch is needed; its live parts are configured directly.

| hideStockPacksWhenWorn | evaChuteModel | Packed visibility |
| --- | --- | --- |
| false | None | Leave stock visibility unchanged. |
| false | Any other value | Leave other stock packs alone and preserve the requested chute. |
| true | None | Hide stock packs and the packed chute. |
| true | Any other value | Hide other stock packs and preserve the requested chute and required ancestors. |

Hiding requests are aggregated per carried part. If several back wearables request a non-None model, the first matching prop in the controller's cached part/prop order wins; None does not veto another wearable's request.

## Lifecycle

The controller caches the original three stock chute roots. Explicit Standalone/Jetpack/Container selection directs all three KerbalEVA model references to the requested original hierarchy, so stock inventory refreshes bind its matching canopy and animation. Stock and None retain the original references. A cached reflection call to stock UpdatePackModels refreshes selection only when the request changes, not every frame.

The selected permitted root and its ancestors remain active while other packs are suppressed. They are never disabled and immediately re-enabled each LateUpdate; removing that cycle fixed the paramotor's shifted packed-chute pose in game. No chute transforms are moved, reparented, or rescaled. The former EVAChuteVisuals alignment helper is removed.

Open canopies remain visible for every setting. Selection changes are deferred while semi-deployed or deployed to avoid rebinding/resetting the canopy. Removing the requesting wearable restores stock selection; removing the final hiding request also restores stock pack visibility and saved flag-decal states. Teardown restores original references without invoking model/UI refreshes.

## Migration and verification

Remove showChuteTransforms from existing configs. For its former true behavior, use evaChuteModel=Stock or an explicit model. For its former false behavior, omit evaChuteModel or use None. An existing explicit Stock now preserves the chute even when stock packs are hidden.

Only the live paramotor used showChuteTransforms in installed WildBlueIndustries configs; it already requested Standalone. SunkWorks diving packs omitted the redundant Stock setting before this change.

Restart KSP after updating. Check ordinary accessories and diving-pack visibility, then test the paramotor at initial EVA and after deployment. Its packed chute should stay against the body. Repeat with a jetpack present, remove the wearable while packed, and change inventory while deployed to verify deferred restoration. Build success verifies compilation; visual/lifecycle checks require KSP.
