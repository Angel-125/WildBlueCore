using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace WildBlueCore.PartModules.KerbalGear
{
    internal class WBIResourceTransferGUI : Dialog<WBIResourceTransferGUI>
    {
        public List<StoredPart> storedParts;

        Vector2 scrollPos;
        static GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(64), GUILayout.Height(32) };
        string buttonIn;
        string buttonOut;

        public WBIResourceTransferGUI() :
        base("Transfer Resources", 635, 400)
        {
            WindowTitle = Localizer.Format("#LOC_WILDBLUECORE_transferResources");
            Resizable = false;
            buttonIn = Localizer.Format("#LOC_WILDBLUECORE_transferResourcesIn");
            buttonOut = Localizer.Format("#LOC_WILDBLUECORE_transferResourcesOut");
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            StoredPart storedPart;
            ProtoPartResourceSnapshot resourceSnapshot;
            int count = storedParts.Count;
            for (int index = 0; index < count; index++)
            {
                storedPart = storedParts[index];

                // Title
                GUILayout.Label("<color=orange>" + storedPart.snapshot.partInfo.title + "</color>");

                // Resources
                GUILayout.BeginHorizontal();
                int resourceCount = storedPart.snapshot.resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    resourceSnapshot = storedPart.snapshot.resources[resourceIndex];
                    GUILayout.Label("<color=white>" + resourceSnapshot.definition.displayName + string.Format(" {0:n2}/{1:n2}", resourceSnapshot.amount, resourceSnapshot.maxAmount) + "</color>");
                    if (GUILayout.Button(buttonIn, buttonOptions))
                    {
                        transferResourceIn(resourceSnapshot, storedPart);
                    }
                    if (GUILayout.Button(buttonOut, buttonOptions))
                    {
                        transferResourceOut(resourceSnapshot, storedPart);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        void transferResource(ProtoPartResourceSnapshot resourceSnapshot, StoredPart sourcePart, bool transferIn)
        {
            StoredPart storedPart;
            int count = storedParts.Count;
            for (int index = 0; index < count; index++)
            {
                storedPart = storedParts[index];
                if (storedPart == sourcePart)
                    continue;
            }
        }

        void transferResourceIn(ProtoPartResourceSnapshot resourceSnapshot, StoredPart sourcePart)
        {
            if (resourceSnapshot.maxAmount >= resourceSnapshot.amount)
                return;

            StoredPart storedPart;
            List<ProtoPartResourceSnapshot> resources = new List<ProtoPartResourceSnapshot>();
            ProtoPartResourceSnapshot resource;

            // Get the parts that have the resource that we're importing
            int count = storedParts.Count;
            int resourceCount;
            for (int index = 0; index < count; index++)
            {
                storedPart = storedParts[index];
                if (storedPart == sourcePart)
                    continue;

                resourceCount = storedPart.snapshot.resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    resource = storedPart.snapshot.resources[resourceIndex];

                    if (resource.resourceName == resourceSnapshot.resourceName)
                    {
                        // Skip parts that are empty
                        if (resource.amount <= 0)
                            continue;

                        resources.Add(resource);
                    }
                }
            }

            // Go through the list of parts and pull their resource until the part that is importing the resource is full.
            count = resources.Count;
            double amountToAdd;
            for (int index = 0; index < count; index++)
            {
                resource = resources[index];

                // If we're full, then stop.
                if (resourceSnapshot.amount >= resourceSnapshot.maxAmount)
                    break;

                if (resourceSnapshot.amount + resource.amount <= resourceSnapshot.maxAmount)
                {
                    resourceSnapshot.amount += resource.amount;
                    resource.amount = 0f;
                }
                else
                {
                    amountToAdd = resourceSnapshot.maxAmount - resourceSnapshot.amount;
                    resource.amount -= amountToAdd;
                    resourceSnapshot.amount = resourceSnapshot.maxAmount;
                    break;
                }
            }
        }

        void transferResourceOut(ProtoPartResourceSnapshot resourceSnapshot, StoredPart sourcePart)
        {
            if (resourceSnapshot.amount <= 0)
                return;

            StoredPart storedPart;
            List<ProtoPartResourceSnapshot> resources = new List<ProtoPartResourceSnapshot>();
            ProtoPartResourceSnapshot resource;

            // Get the parts that have the resource that we're exporting
            int count = storedParts.Count;
            int resourceCount;
            for (int index = 0; index < count; index++)
            {
                storedPart = storedParts[index];
                if (storedPart == sourcePart)
                    continue;

                resourceCount = storedPart.snapshot.resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    resource = storedPart.snapshot.resources[resourceIndex];

                    if (resource.resourceName == resourceSnapshot.resourceName)
                    {
                        // Skip parts that are full
                        if (resource.amount >= resource.maxAmount)
                            continue;

                        resources.Add(resource);
                    }
                }
            }

            // Divide up our resource amount by the number of recipients
            count = resources.Count;
            double amountPerPart = resourceSnapshot.amount / count;
            double amountRemaining = 0;

            // Now distribute the resource
            for (int index = 0; index < count; index++)
            {
                resource = resources[index];

                if (resource.amount + amountPerPart <= resource.maxAmount)
                {
                    resource.amount += amountPerPart;
                }
                else
                {
                    amountRemaining += (resource.amount + amountPerPart) - resource.maxAmount;
                }
            }

            // Put back what we didn't use
            if (amountRemaining > 0)
                resourceSnapshot.amount = amountRemaining;
            else
                resourceSnapshot.amount = 0f;
        }
    }
}
