namespace WildBlueCore.PartModules.Aero
{
    /// <summary>
    /// Identifies a ModuleLiftingSurface as a passive parafoil stabilizer.
    /// ModuleParafoil keeps the surface disabled until the parafoil is fully
    /// deployed, then ramps it to its configured deflectionLiftCoeff.
    /// </summary>
    public class ModuleParafoilStabilizer : ModuleLiftingSurface
    {
    }
}
