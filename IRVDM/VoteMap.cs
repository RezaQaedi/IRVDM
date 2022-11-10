using CitizenFX.Core;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace IRVDM
{
    internal struct VoteMapFormat
    {
        public string Title;
        public string SubTitle;
        public string Description;
        public Dictionary<string, string> Details;
        public string Txd;
        public string Txn;
        public bool TitleAlpha;
        public bool Verifyed;
        public int Icon;
        public bool Cheked;
        public int Rp;
        public int Money;
        public int IconColor;
    }

    internal class VoteMap : ControlsSelectBase
    {
        public string Title { get; private set; }
        public string RightTitle { get; set; } = "";
        private bool IsDisabled = false;
        private readonly string ScaleformName = "MP_NEXT_JOB_SELECTION";
        private Scaleform VoteScaleform;
        private readonly List<VoteMapFormat> VoteItems = new List<VoteMapFormat>();

        public event Action<string> OnVote;

        public VoteMap(string title, List<VoteMapFormat> items)
        {
            Title = title;
            VoteItems = items;

            Load();

            base.OnIndexChanged += VoteMap_OnIndexChanged;
            base.OnItemSelected += VoteMap_OnItemSelected;
        }

        private void VoteMap_OnItemSelected(ControlsSelectBase arg1, int arg2)
        {
            SetSelectionForThisItem(arg2, true);
            SetDetailForThisItem(arg2);
            Disable(true);
            OnVote?.Invoke(VoteItems.Find(p => VoteItems.IndexOf(p) == arg2).Title);
        }

        private void VoteMap_OnIndexChanged(ControlsSelectBase arg1, int arg2, int arg3)
        {
            SetSelectionForThisItem(arg2);
            SetHoverForThisItem(arg2);
            SetDetailForThisItem(arg2);
        }

        private async void Load()
        {
            if (VoteScaleform == null)
                VoteScaleform = new Scaleform("MP_NEXT_JOB_SELECTION");

            if (!VoteScaleform.IsLoaded)
                RequestScaleformMovie(ScaleformName);

            while (!HasScaleformMovieLoaded(VoteScaleform.Handle))
            {
                await BaseScript.Delay(0);
            }

            VoteScaleform.CallFunction("SET_TITLE", Title, RightTitle.ToString());

            int i = 0;
            foreach (VoteMapFormat item in VoteItems)
            {
                VoteScaleform.CallFunction("SET_GRID_ITEM", i, item.Title, item.Txd, item.Txn, true, item.Verifyed, item.Icon, item.Cheked, item.Rp, item.Money, item.TitleAlpha, item.IconColor);
                i++;
            }

            SetSelectionForThisItem(SelectedIndex);
            SetHoverForThisItem(SelectedIndex);
            SetDetailForThisItem(SelectedIndex);

            VoteScaleform.CallFunction("INIT_LOBBY_LIST_SCROLL", 1, 1, 1, 1, 1); //box list on the right
            VoteScaleform.CallFunction("SET_LOBBY_LIST_VISIBILITY", false); //list on the right
        }

        private void SetDetailForThisItem(int index)
        {
            int i = 0;
            foreach (var item in VoteItems[index].Details)
            {
                VoteScaleform.CallFunction("SET_DETAILS_ITEM", i, 1, 1, 1, 1, 1, item.Key, item.Value);
                i++;
            }
        }

        private void SetSelectionForThisItem(int index, bool select = false)
        {
            VoteScaleform.CallFunction("SET_SELECTION", index, VoteItems[index].SubTitle, VoteItems[index].Description, !select);
        }

        private void SetHoverForThisItem(int index)
        {
            VoteScaleform.CallFunction("SET_HOVER", index, false);
        }

        public void ShowPlayerVoteOnThisItem(int index, string name, int r = 255, int g = 0, int b = 0)
        {
            VoteScaleform.CallFunction("SHOW_PLAYER_VOTE", index, name, r, g, b);
        }

        public void ShowPlayerVoteOnThisItem(string itemTitel, string name, int r = 255, int g = 0, int b = 0)
        {
            int index = VoteItems.IndexOf(VoteItems.Find(p => p.Title == itemTitel));
            VoteScaleform.CallFunction("SHOW_PLAYER_VOTE", index, name, r, g, b);
        }

        public void SetVotesForThisItem(string itemTitel, int vote, int color = 13, bool haveTick = false)
        {
            int index = VoteItems.IndexOf(VoteItems.Find(p => p.Title == itemTitel));
            VoteScaleform.CallFunction("SET_GRID_ITEM_VOTE", index, vote, color, haveTick, true);
        }

        public void SetVotesForThisItem(int index, int vote, int color = 13, bool haveTick = false)
        {
            VoteScaleform.CallFunction("SET_GRID_ITEM_VOTE", index, vote, color, haveTick, true);
        }

        public void Disable(bool IsDisable)
        {
            //VoteScaleform.CallFunction("SET_ITEMS_GREYED_OUT", IsDisable);
            IsDisabled = IsDisable;
        }

        public void Draw()
        {
            if (VoteScaleform == null)
                return;

            if (VoteScaleform.IsLoaded)
            {
                if (!IsDisabled)
                {
                    base.Process();
                }


                VoteScaleform.Render2D();
            }
        }

        public void Dispose()
        {
            VoteScaleform.Dispose();
            VoteScaleform = null;
        }
    }
}
