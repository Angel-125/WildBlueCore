using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildBlueCore.Utilities
{
    internal class MutablePartSet : PartSet
    {
        #region Constructors
        public MutablePartSet(Vessel vessel) : base(vessel)
        {
        }
        #endregion

        #region API
        public Dictionary<int, ResourcePrioritySet> GetPullList()
        {
            return pullList;
        }

        public Dictionary<int, ResourcePrioritySet> GetPushList()
        {
            return pushList;
        }

        public void RemovePartFromLists(Part part)
        {
            removePartFromMap(part, pullList);
            removePartFromMap(part, pushList);
        }

        public void BuildLists(Part part)
        {
            int count = part.Resources.Count;
            if (count <= 0)
                return;

            for (int index = 0; index < count; index++)
            {
                GetOrCreateList(part.Resources[index].info.id, true);
                GetOrCreateList(part.Resources[index].info.id, false);
            }
        }
        #endregion

        #region Helpers
        private void removePartFromMap(Part part, Dictionary<int, ResourcePrioritySet> resourceMap)
        {
            // Iterate through the map
            int[] keys = resourceMap.Keys.ToArray();
            ResourcePrioritySet prioritySet;
            for (int mapIndex = 0; mapIndex < keys.Length; mapIndex++)
            {
                prioritySet = resourceMap[keys[mapIndex]];
                removePartFromLists(part, prioritySet.lists);
                removePartFromHashSet(part, prioritySet.set);
            }
        }

        private void removePartFromLists(Part part, List<List<PartResource>> listOfLists)
        {
            int count = listOfLists.Count;
            List<PartResource> resources;
            int doomedIndex = -1;
            int resourceCount;
            for (int listIndex = 0; listIndex < count; listIndex++)
            {
                resources = listOfLists[listIndex];
                resourceCount = resources.Count;
                for (int index = 0; index < resourceCount; index++)
                {
                    if (resources[index].part == part)
                    {
                        doomedIndex = index;
                        break;
                    }
                }

                if (doomedIndex >= 0)
                    resources.RemoveAt(doomedIndex);

                doomedIndex = -1;
            }
        }

        private void removePartFromHashSet(Part part, HashSet<PartResource> hashSet)
        {
            HashSet<PartResource>.Enumerator enumerator = hashSet.GetEnumerator();
            PartResource resource;
            List<PartResource> doomed = new List<PartResource>();
            while (enumerator.MoveNext())
            {
                resource = enumerator.Current;
                if (resource.part == part)
                {
                    doomed.Add(resource);
                }
            }

            int count = doomed.Count;
            for (int index = 0; index < count; index++)
            {
                hashSet.Remove(doomed[index]);
            }
        }
        #endregion
    }
}
