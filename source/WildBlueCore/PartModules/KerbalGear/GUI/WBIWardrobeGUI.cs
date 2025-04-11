using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using UnityEngine;
using Expansions.Missions;
using Expansions.Serenity;

namespace WildBlueCore.KerbalGear
{
    public class WBISuitCombo : SuitCombo
    {
        public bool isStockSuit = true;
    }

    public class WBIWardrobeGUI: Dialog<WBIWardrobeGUI>
    {
        #region Constants
        const string kWardrobeIconNode = "WARDROBE_IMAGE";
        #endregion

        #region Fields
        public Part part;
        #endregion

        #region Housekeeping
        SuitCombos suitCombos;
        SuitCombo[] maleSuitCombos;
        SuitCombo[] femaleSuitCombos;
        SuitCombo[] defaultSuits;
        SuitCombo[] vintageSuits;
        SuitCombo[] futureSuits;
        SuitCombo[] slimSuits;
        SuitCombo selectedCombo = null;
        List<ProtoCrewMember> crewList = null;
        ProtoCrewMember selectedCrew = null;
        Vector2 crewListScrollPos = Vector2.zero;
        Vector2 suitListScrollPos = Vector2.zero;
        GUILayoutOption[] crewPanelWidth = new GUILayoutOption[] { GUILayout.Width(200) };
        GUILayoutOption[] suitPanelWidth = new GUILayoutOption[] { GUILayout.Width(200) };
        GUILayoutOption[] suitPreviewPanelWidth = new GUILayoutOption[] { GUILayout.Width(300) };
        Texture2D suitSprite = null;
        Dictionary<string, string> wardrobeIcons = null;
        int selectedCrewIndex;
        #endregion

        #region Constructors
        public WBIWardrobeGUI() :
        base("Suit Switcher", 700, 400)
        {
            WindowTitle = Localizer.Format("#LOC_WILDBLUECORE_suitSwitcherTitle");
            Resizable = false;
        }
        #endregion

        #region Overrides
        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                // Get the suit combos
                suitCombos = GameDatabase.Instance.GetComponent<SuitCombos>();
                List<SuitCombo> maleSuits = new List<SuitCombo>();
                List<SuitCombo> femaleSuits = new List<SuitCombo>();

                if (suitCombos.helmetSuitPickerWindow != null)
                {
                    Debug.Log("[WBIWardrobeGUI] - suitCombos.helmetSuitPickerWindow is not null");
                }

                // Get stock suits
                SuitCombo suitCombo;
                int count = suitCombos.StockCombos.Count;
                for (int index = 0; index < count; index++)
                {
                    suitCombo = suitCombos.StockCombos[index];

                    if (suitCombo.gender.ToLower() == "male")
                        maleSuits.Add(suitCombo);
                    else
                        femaleSuits.Add(suitCombo);
                }

                // Get extra suits
                count = suitCombos.ExtraCombos.Count;
                for (int index = 0; index < count; index++)
                {
                    suitCombo = suitCombos.ExtraCombos[index];
                    if (suitCombo.gender.ToLower() == "male")
                        maleSuits.Add(suitCombo);
                    else
                        femaleSuits.Add(suitCombo);
                }
                maleSuitCombos = maleSuits.ToArray();
                femaleSuitCombos = femaleSuits.ToArray();

                // Get wardrobe icons
                getWardrobeIcons();

                Debug.Log("[WBIWardrobeGUI] - Male suits found: " + maleSuitCombos.Length);
                for (int index = 0; index < maleSuitCombos.Length; index++)
                    Debug.Log("[WBIWardrobeGUI] - " + maleSuitCombos[index].name + " | " + Localizer.Format(maleSuitCombos[index].displayName));

                Debug.Log("[WBIWardrobeGUI] - Female suits found: " + femaleSuitCombos.Length);
                for (int index = 0; index < femaleSuitCombos.Length; index++)
                    Debug.Log("[WBIWardrobeGUI] - " + femaleSuitCombos[index].name + " | " + Localizer.Format(femaleSuitCombos[index].displayName));

                // Get crew list
                crewList = part.protoModuleCrew;
                if (crewList != null && crewList.Count > 0)
                {
                    selectedCrew = crewList[0];
                    selectedCombo = suitCombos.GetCombo(selectedCrew.ComboId);
                    updateSuitCombos();
                    updateSuitSprite();
                }
            }
        }
        #endregion

        #region Drawing
        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            // Draw list of kerbals
            drawCrewList();

            // Draw suit selection list
            drawSuitSelectionList();

            drawSuitPreviewPanel();

            GUILayout.EndHorizontal();
        }

        void drawSuitPreviewPanel()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(Vector2.zero, suitPreviewPanelWidth);

            ProtoCrewMember.KerbalSuit suitType = ProtoCrewMember.KerbalSuit.Default;

            if (selectedCombo != null)
            {
                string suitTypeString = string.Empty;
                switch (selectedCombo.suitType.ToLower())
                {
                    case "vintage":
                        suitType = ProtoCrewMember.KerbalSuit.Vintage;
                        suitTypeString = Localizer.Format("#autoLOC_8012022");
                        break;
                    case "future":
                        suitType = ProtoCrewMember.KerbalSuit.Future;
                        suitTypeString = Localizer.Format("#autoLOC_8012023");
                        break;
                    case "slim":
                        suitType = ProtoCrewMember.KerbalSuit.Slim;
                        suitTypeString = Localizer.Format("#autoLOC_6011176");
                        break;
                    default:
                        suitType = ProtoCrewMember.KerbalSuit.Default;
                        suitTypeString = Localizer.Format("#autoLOC_8012021");
                        break;
                }
                GUILayout.Label("<color=white>" + Localizer.Format(selectedCombo.displayName) + " " + suitTypeString + "</color>");
            }

            // Suit sprite
            if (suitSprite != null)
                GUILayout.Label(suitSprite);

            GUILayout.EndScrollView();

            if (GUILayout.Button(Localizer.Format("#LOC_WILDBLUECORE_suitSwitcherSelectSuit")) && selectedCrew != null && selectedCombo != null)
            {
                selectedCrew.ComboId = selectedCombo.name;
                selectedCrew.SuitTexturePath = selectedCombo.suitTexture;
                selectedCrew.NormalTexturePath = selectedCombo.normalTexture;
                selectedCrew.suit = suitType;
                selectedCrew.UseStockTexture = suitCombos.StockCombos.Contains(selectedCombo);

                part.protoModuleCrew[selectedCrewIndex] = selectedCrew;

                Debug.Log("[WBIWardrobeGUI] - Changing wardrobe for: " + selectedCrew.name);
                Debug.Log("[WBIWardrobeGUI] - selectedCrew.ComboId: " + selectedCrew.ComboId);
                Debug.Log("[WBIWardrobeGUI] - selectedCrew.SuitTexturePath: " + selectedCrew.SuitTexturePath);
                Debug.Log("[WBIWardrobeGUI] - selectedCrew.NormalTexturePath: " + selectedCrew.NormalTexturePath);
                Debug.Log("[WBIWardrobeGUI] - selectedCrew.suit: " + selectedCrew.suit.ToString());
                Debug.Log("[WBIWardrobeGUI] - selectedCrew.UseStockTexture: " + selectedCrew.UseStockTexture.ToString());
            }

            GUILayout.EndVertical();
        }

        void drawSuitSelectionList()
        {
            GUILayout.BeginVertical();

            if (selectedCrew != null)
            {
                GUILayout.Label("<color=white>" + selectedCrew.name + " - " + selectedCrew.trait + "</color>");
                string suitName = Localizer.Format(suitCombos.GetCombo(selectedCrew.ComboId).displayName);
                GUILayout.Label("<color=white><b>Currently Wearing</b></color>");
                GUILayout.Label("<color=white>" + suitName + "</color>");
            }

            suitListScrollPos = GUILayout.BeginScrollView(suitListScrollPos, suitPanelWidth);

            SuitCombo suitCombo;

            // Default
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_8012021") + "</b></color>");
            for (int index = 0; index < defaultSuits.Length; index++)
            {
                suitCombo = defaultSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            // Vintage
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_8012022") + "</b></color>");
            for (int index = 0; index < vintageSuits.Length; index++)
            {
                suitCombo = vintageSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            // Future
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_8012023") + "</b></color>");
            for (int index = 0; index < futureSuits.Length; index++)
            {
                suitCombo = futureSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            // Slim
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_6011176") + "</b></color>");
            for (int index = 0; index < slimSuits.Length; index++)
            {
                suitCombo = slimSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        void drawCrewList()
        {
            if (crewList == null)
                return;
            else if (crewList.Count == 0)
                return;

            int count = crewList.Count;

            GUILayout.BeginVertical();

            crewListScrollPos = GUILayout.BeginScrollView(crewListScrollPos, crewPanelWidth);

            for (int index = 0; index < count; index++)
            {
                if (GUILayout.Button(crewList[index].name))
                {
                    selectedCrew = crewList[index];
                    selectedCrewIndex = index;
                    selectedCombo = suitCombos.GetCombo(selectedCrew.ComboId);
                    updateSuitCombos();
                    updateSuitSprite();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        #endregion

        #region Helpers
        void getWardrobeIcons()
        {
            wardrobeIcons = new Dictionary<string, string>();
            ConfigNode[] wardrobes = GameDatabase.Instance.GetConfigNodes(kWardrobeIconNode);
            ConfigNode node;

            for (int index = 0; index < wardrobes.Length; index++)
            {
                node = wardrobes[index];
                if (!node.HasValue("name") || !node.HasValue("image"))
                    continue;
                if (!wardrobeIcons.ContainsKey(node.GetValue("name")))
                    wardrobeIcons.Add(node.GetValue("name"), node.GetValue("image"));
            }
        }

        void updateSuitCombos()
        {
            List<SuitCombo> defaultCombos = new List<SuitCombo>();
            List<SuitCombo> vintageCombos = new List<SuitCombo>();
            List<SuitCombo> futureCombos = new List<SuitCombo>();
            List<SuitCombo> slimCombos = new List<SuitCombo>();
            SuitCombo[] combos = selectedCrew.gender == ProtoCrewMember.Gender.Male ? maleSuitCombos : femaleSuitCombos;
            SuitCombo combo;

            for (int index = 0; index < combos.Length; index++)
            {
                combo = combos[index];

                switch (combo.suitType.ToLower())
                {
                    case "vintage":
                        vintageCombos.Add(combo);
                        break;

                    case "future":
                        futureCombos.Add(combo);
                        break;

                    case "slim":
                        slimCombos.Add(combo);
                        break;

                    default:
                        defaultCombos.Add(combo);
                        break;
                }
            }

            defaultSuits = defaultCombos.ToArray();
            vintageSuits = vintageCombos.ToArray();
            futureSuits = futureCombos.ToArray();
            slimSuits = slimCombos.ToArray();
        }

        void updateSuitSprite()
        {
            if (selectedCombo != null && wardrobeIcons.ContainsKey(selectedCombo.name))
            {
                suitSprite = GameDatabase.Instance.GetTexture(wardrobeIcons[selectedCombo.name], false);
            }

            else if (selectedCombo != null && !string.IsNullOrEmpty(selectedCombo.sprite))
            {
                suitSprite = GameDatabase.Instance.GetTexture(selectedCombo.sprite, false);
            }

            else
            {
                suitSprite = null;
            }
        }
        #endregion
    }
}
