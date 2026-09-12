using Experience;
using Experience.Effects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// Grants temporary stock experience effects while their contributing wearable cargo is
    /// carried by an EVA kerbal. Add EXPERIENCE_EFFECT child nodes to the wearable part's
    /// part-level EVA_OVERRIDES node; KerbalGear creates this module automatically when needed.
    /// </summary>
    /// <example>
    /// <code>
    /// EVA_OVERRIDES
    /// {
    ///     EXPERIENCE_EFFECT
    ///     {
    ///         name = RepairSkill
    ///         tier = 2
    ///     }
    /// }
    /// </code>
    /// </example>
    public class WBIModuleEVAExperienceEffects : WBIBasePartModule,
        IKerbalGearProviderListener
    {
        #region Constants
        internal const string ModuleName = "WBIModuleEVAExperienceEffects";
        internal const string OverridesNodeName = "EVA_OVERRIDES";
        internal const string EffectNodeName = "EXPERIENCE_EFFECT";
        const string kNameField = "name";
        const string kTierField = "tier";
        #endregion

        #region Supporting types
        enum TierRegistration
        {
            None,
            Repair,
            FailureRepair,
            Science,
            Autopilot,
            EVAChute
        }

        /// <summary>
        /// Describes one EXPERIENCE_EFFECT request read from one carried inventory provider.
        /// Claims are short-lived value data collected before duplicate effect types and tiers
        /// are consolidated, so this is a struct rather than persistent runtime state.
        /// </summary>
        struct EffectClaim
        {
            internal Type effectType;
            internal string effectName;
            internal ConfigNode config;
            internal string nonTierSignature;
            internal int tier;

            internal EffectClaim(Type effectType, string effectName, ConfigNode config,
                string nonTierSignature, int tier)
            {
                this.effectType = effectType;
                this.effectName = effectName;
                this.config = config;
                this.nonTierSignature = nonTierSignature;
                this.tier = tier;
            }
        }

        /// <summary>
        /// Describes the single effect that should exist after every matching EffectClaim has
        /// been consolidated. It holds the winning configuration, effective tier, and comparison
        /// signature used to decide whether the currently active effect must be replaced.
        /// </summary>
        struct DesiredEffect
        {
            internal Type effectType;
            internal string effectName;
            internal ConfigNode config;
            internal string signature;
            internal int tier;

            internal DesiredEffect(Type effectType, string effectName, ConfigNode config,
                string signature, int tier)
            {
                this.effectType = effectType;
                this.effectName = effectName;
                this.config = config;
                this.signature = signature;
                this.tier = tier;
            }
        }

        /// <summary>
        /// Tracks an instantiated ExperienceEffect and its live PartValues registration. This is
        /// mutable runtime state and therefore remains a class: tierProvider captures this exact
        /// instance, and its stable identity is needed to unregister the same delegate later.
        /// </summary>
        sealed class ActiveEffect
        {
            internal ExperienceEffect effect;
            internal string signature;
            internal int tier;
            internal TierRegistration tierRegistration;
            internal EventValueComparison<int>.OnEvent tierProvider;
        }
        #endregion

        #region Housekeeping
        ProtoCrewMember crewMember;
        readonly Dictionary<Type, ActiveEffect> activeEffects =
            new Dictionary<Type, ActiveEffect>();
        readonly HashSet<string> warningKeys = new HashSet<string>();
        bool subscribedToBoarding;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            captureCrewMember();
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            subscribedToBoarding = true;
        }

        public override void OnInactive()
        {
            removeAllEffects(part);
            unsubscribeFromBoarding();
            base.OnInactive();
        }

        public void OnDestroy()
        {
            removeAllEffects(part);
            unsubscribeFromBoarding();
        }

        /// <summary>
        /// Rebuilds the temporary effects from all inventory providers assigned to this aggregate
        /// module. Duplicate effect types result in one active effect with reference-counted claims.
        /// </summary>
        public void OnKerbalGearProvidersChanged(ModuleInventoryPart inventory,
            KerbalGearModuleProvider[] providers)
        {
            if (!HighLogic.LoadedSceneIsFlight || inventory == null ||
                inventory.part != part || !captureCrewMember())
            {
                return;
            }

            reconcileEffects(buildDesiredEffects(providers));
        }
        #endregion

        #region Discovery
        /// <summary>
        /// Determines whether a cargo part declares at least one experience effect.
        /// </summary>
        internal static bool PartHasExperienceEffects(AvailablePart availablePart)
        {
            if (availablePart == null || availablePart.partConfig == null)
                return false;

            ConfigNode[] overrideNodes =
                availablePart.partConfig.GetNodes(OverridesNodeName);
            for (int index = 0; index < overrideNodes.Length; index++)
            {
                if (overrideNodes[index].HasNode(EffectNodeName))
                    return true;
            }
            return false;
        }

        Dictionary<Type, DesiredEffect> buildDesiredEffects(
            KerbalGearModuleProvider[] providers)
        {
            Dictionary<Type, List<EffectClaim>> claims =
                new Dictionary<Type, List<EffectClaim>>();
            if (providers == null)
                return new Dictionary<Type, DesiredEffect>();

            KerbalGearModuleProvider[] sortedProviders =
                (KerbalGearModuleProvider[])providers.Clone();
            Array.Sort(sortedProviders, delegate(KerbalGearModuleProvider left,
                KerbalGearModuleProvider right)
            {
                return left.SlotIndex.CompareTo(right.SlotIndex);
            });

            for (int providerIndex = 0; providerIndex < sortedProviders.Length;
                providerIndex++)
            {
                KerbalGearModuleProvider provider = sortedProviders[providerIndex];
                if (provider == null || provider.StoredPart == null)
                    continue;

                AvailablePart availablePart =
                    PartLoader.getPartInfoByName(provider.StoredPart.partName);
                collectPartClaims(availablePart, claims);
            }

            Dictionary<Type, DesiredEffect> desired =
                new Dictionary<Type, DesiredEffect>();
            foreach (KeyValuePair<Type, List<EffectClaim>> claimEntry in claims)
            {
                List<EffectClaim> effectClaims = claimEntry.Value;
                EffectClaim winner = effectClaims[0];
                bool hasExplicitTier = false;
                int highestTier = -1;
                HashSet<string> configurations = new HashSet<string>();

                for (int claimIndex = 0; claimIndex < effectClaims.Count; claimIndex++)
                {
                    EffectClaim claim = effectClaims[claimIndex];
                    configurations.Add(claim.nonTierSignature);
                    if (claim.tier >= 0 && (!hasExplicitTier || claim.tier > highestTier))
                    {
                        winner = claim;
                        highestTier = claim.tier;
                        hasExplicitTier = true;
                    }
                }

                if (configurations.Count > 1)
                {
                    warnOnce("conflict:" + claimEntry.Key.FullName,
                        "Carried items specify different non-tier settings for " +
                        winner.effectName + "; the deterministic winning provider is used.");
                }

                int effectiveTier = hasExplicitTier ? highestTier : -1;
                string signature = winner.config.ToString() + "|effectiveTier=" +
                    (hasExplicitTier ? highestTier.ToString() : "stock");
                desired.Add(claimEntry.Key, new DesiredEffect(winner.effectType,
                    winner.effectName, winner.config, signature, effectiveTier));
            }
            return desired;
        }

        void collectPartClaims(AvailablePart availablePart,
            Dictionary<Type, List<EffectClaim>> claims)
        {
            if (availablePart == null || availablePart.partConfig == null)
                return;

            ConfigNode[] overrideNodes =
                availablePart.partConfig.GetNodes(OverridesNodeName);
            for (int overrideIndex = 0; overrideIndex < overrideNodes.Length;
                overrideIndex++)
            {
                ConfigNode[] effectNodes =
                    overrideNodes[overrideIndex].GetNodes(EffectNodeName);
                for (int effectIndex = 0; effectIndex < effectNodes.Length; effectIndex++)
                {
                    ConfigNode effectNode = effectNodes[effectIndex];
                    string effectName = effectNode.GetValue(kNameField);
                    if (string.IsNullOrEmpty(effectName))
                    {
                        warnOnce("missingName:" + availablePart.name,
                            availablePart.name + " has an EXPERIENCE_EFFECT without a name.");
                        continue;
                    }
                    effectName = effectName.Trim();

                    Type effectType = KerbalRoster.GetExperienceEffectType(effectName);
                    if (effectType == null ||
                        !typeof(ExperienceEffect).IsAssignableFrom(effectType))
                    {
                        warnOnce("unknown:" + effectName,
                            "Cannot grant unknown experience effect " + effectName + ".");
                        continue;
                    }

                    int tier = -1;
                    string tierValue = effectNode.GetValue(kTierField);
                    if (!string.IsNullOrEmpty(tierValue))
                    {
                        if (!int.TryParse(tierValue, out tier))
                        {
                            tier = -1;
                            warnOnce("tier:" + availablePart.name + ":" + effectName,
                                availablePart.name + " has an invalid tier for " +
                                effectName + "; stock effect behavior will be used.");
                        }
                        else
                        {
                            tier = Mathf.Clamp(tier, 0,
                                KerbalRoster.GetExperienceMaxLevel());
                        }
                    }

                    ConfigNode config = effectNode.CreateCopy();
                    ConfigNode nonTierConfig = effectNode.CreateCopy();
                    nonTierConfig.RemoveValue(kTierField);
                    EffectClaim claim = new EffectClaim(effectType, effectName, config,
                        nonTierConfig.ToString(), tier);

                    List<EffectClaim> effectClaims;
                    if (!claims.TryGetValue(effectType, out effectClaims))
                    {
                        effectClaims = new List<EffectClaim>();
                        claims.Add(effectType, effectClaims);
                    }
                    effectClaims.Add(claim);
                }
            }
        }
        #endregion

        #region Effect lifecycle
        void reconcileEffects(Dictionary<Type, DesiredEffect> desiredEffects)
        {
            List<Type> removals = new List<Type>();
            foreach (KeyValuePair<Type, ActiveEffect> activeEntry in activeEffects)
            {
                DesiredEffect desired;
                if (!desiredEffects.TryGetValue(activeEntry.Key, out desired) ||
                    activeEntry.Value.signature != desired.signature)
                {
                    removals.Add(activeEntry.Key);
                }
            }

            for (int index = 0; index < removals.Count; index++)
                removeEffect(removals[index], part);

            foreach (KeyValuePair<Type, DesiredEffect> desiredEntry in desiredEffects)
            {
                if (!activeEffects.ContainsKey(desiredEntry.Key))
                    addEffect(desiredEntry.Value);
            }

            if (part != null && part.PartValues != null)
                part.PartValues.Update();
        }

        void addEffect(DesiredEffect desired)
        {
            ExperienceEffect effect = null;
            try
            {
                effect = Activator.CreateInstance(desired.effectType,
                    new object[] { crewMember.experienceTrait }) as ExperienceEffect;
                if (effect == null)
                    throw new InvalidOperationException("Activator returned null.");

                effect.LoadFromConfig(desired.config);
                crewMember.experienceTrait.Effects.Add(effect);

                ActiveEffect active = new ActiveEffect
                {
                    effect = effect,
                    signature = desired.signature,
                    tier = desired.tier,
                    tierRegistration = getTierRegistration(desired.effectType)
                };

                if (desired.tier >= 0 && active.tierRegistration != TierRegistration.None)
                    registerTierProvider(active, part);
                else
                {
                    if (desired.tier >= 0)
                    {
                        warnOnce("unsupportedTier:" + desired.effectType.FullName,
                            desired.effectName + " does not have a supported tier adapter; " +
                            "its stock behavior will be used.");
                    }
                    active.tierRegistration = TierRegistration.None;
                    effect.Register(part);
                }

                activeEffects.Add(desired.effectType, active);
            }
            catch (Exception ex)
            {
                if (effect != null && crewMember != null &&
                    crewMember.experienceTrait != null)
                {
                    crewMember.experienceTrait.Effects.Remove(effect);
                }
                Debug.LogError("[WildBlueCore] Could not grant experience effect " +
                    desired.effectName + ": " + ex);
            }
        }

        void removeEffect(Type effectType, Part registrationPart)
        {
            ActiveEffect active;
            if (!activeEffects.TryGetValue(effectType, out active))
                return;

            unregisterActive(active, registrationPart);
            if (crewMember != null && crewMember.experienceTrait != null)
                crewMember.experienceTrait.Effects.Remove(active.effect);
            activeEffects.Remove(effectType);
        }

        void removeAllEffects(Part registrationPart)
        {
            Type[] effectTypes = new Type[activeEffects.Count];
            activeEffects.Keys.CopyTo(effectTypes, 0);
            for (int index = 0; index < effectTypes.Length; index++)
                removeEffect(effectTypes[index], registrationPart);

            if (registrationPart != null && registrationPart.PartValues != null)
                registrationPart.PartValues.Update();
        }

        void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
        {
            if (action.from != part || activeEffects.Count == 0)
                return;

            // RemoveCrewmember has already invoked each effect's stock Unregister on the EVA
            // part, and AddCrewmember has registered every effect on the destination. Tiered
            // effects use our callback on EVA, so remove that callback from the source and undo
            // the automatic stock registration on the destination before removing the effect.
            foreach (KeyValuePair<Type, ActiveEffect> activeEntry in activeEffects)
            {
                ActiveEffect active = activeEntry.Value;
                if (active.tierRegistration != TierRegistration.None)
                {
                    unregisterTierProvider(active, part);
                    if (action.to != null)
                        active.effect.Unregister(action.to);
                }
                else if (action.to != null)
                {
                    active.effect.Unregister(action.to);
                }

                if (crewMember != null && crewMember.experienceTrait != null)
                    crewMember.experienceTrait.Effects.Remove(active.effect);
            }
            activeEffects.Clear();

            if (part != null && part.PartValues != null)
                part.PartValues.Update();
            if (action.to != null && action.to.PartValues != null)
                action.to.PartValues.Update();
        }

        void unregisterActive(ActiveEffect active, Part registrationPart)
        {
            if (active == null || registrationPart == null)
                return;
            if (active.tierRegistration == TierRegistration.None)
                active.effect.Unregister(registrationPart);
            else
                unregisterTierProvider(active, registrationPart);
        }
        #endregion

        #region Tier adapters
        static TierRegistration getTierRegistration(Type effectType)
        {
            if (effectType == typeof(RepairSkill))
                return TierRegistration.Repair;
            if (effectType == typeof(FailureRepairSkill))
                return TierRegistration.FailureRepair;
            if (effectType == typeof(ScienceSkill))
                return TierRegistration.Science;
            if (effectType == typeof(AutopilotSkill))
                return TierRegistration.Autopilot;
            if (effectType == typeof(EVAChuteSkill))
                return TierRegistration.EVAChute;
            return TierRegistration.None;
        }

        static void registerTierProvider(ActiveEffect active, Part targetPart)
        {
            if (targetPart == null || targetPart.PartValues == null)
                return;

            active.tierProvider = delegate { return active.tier; };
            switch (active.tierRegistration)
            {
                case TierRegistration.Repair:
                    targetPart.PartValues.RepairSkill.Add(active.tierProvider);
                    break;
                case TierRegistration.FailureRepair:
                    targetPart.PartValues.FailureRepairSkill.Add(active.tierProvider);
                    break;
                case TierRegistration.Science:
                    targetPart.PartValues.ScienceSkill.Add(active.tierProvider);
                    break;
                case TierRegistration.Autopilot:
                    targetPart.PartValues.AutopilotSkill.Add(active.tierProvider);
                    targetPart.PartValues.AutopilotKerbalSkill.Add(active.tierProvider);
                    break;
                case TierRegistration.EVAChute:
                    targetPart.PartValues.EVAChuteSkill.Add(active.tierProvider);
                    break;
            }
        }

        static void unregisterTierProvider(ActiveEffect active, Part targetPart)
        {
            if (targetPart == null || targetPart.PartValues == null ||
                active.tierProvider == null)
            {
                return;
            }

            switch (active.tierRegistration)
            {
                case TierRegistration.Repair:
                    targetPart.PartValues.RepairSkill.Remove(active.tierProvider);
                    break;
                case TierRegistration.FailureRepair:
                    targetPart.PartValues.FailureRepairSkill.Remove(active.tierProvider);
                    break;
                case TierRegistration.Science:
                    targetPart.PartValues.ScienceSkill.Remove(active.tierProvider);
                    break;
                case TierRegistration.Autopilot:
                    targetPart.PartValues.AutopilotSkill.Remove(active.tierProvider);
                    targetPart.PartValues.AutopilotKerbalSkill.Remove(active.tierProvider);
                    break;
                case TierRegistration.EVAChute:
                    targetPart.PartValues.EVAChuteSkill.Remove(active.tierProvider);
                    break;
            }
            active.tierProvider = null;
        }
        #endregion

        #region Utilities
        bool captureCrewMember()
        {
            if (crewMember != null && crewMember.experienceTrait != null)
                return true;
            if (part == null || part.protoModuleCrew == null ||
                part.protoModuleCrew.Count == 0)
            {
                return false;
            }
            crewMember = part.protoModuleCrew[0];
            return crewMember != null && crewMember.experienceTrait != null;
        }

        void unsubscribeFromBoarding()
        {
            if (!subscribedToBoarding)
                return;
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            subscribedToBoarding = false;
        }

        void warnOnce(string key, string message)
        {
            if (warningKeys.Add(key))
                Debug.LogWarning("[WildBlueCore] " + message);
        }
        #endregion
    }
}
