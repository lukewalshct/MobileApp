using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using LWalshFinalClient.Data_Models;
using System.Threading.Tasks;

namespace LWalshFinalClient.Resources
{
    class VoteScrollAdapter : BaseAdapter<VoteListItem>
    {
        private IReadOnlyList<VoteListItem> voteListItems;
        private Activity context;
        Button voteYesButton;
        Button voteNoButton;        

        public VoteScrollAdapter(Activity context, IReadOnlyList<VoteListItem> voteListItems) : base() {
            this.context = context;
            this.voteListItems = voteListItems;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override VoteListItem this[int position]
        {
            get { return this.voteListItems[position]; }
        }
        public override int Count
        {
            get { return this.voteListItems.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
            {
                //view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);
                //view = context.LayoutInflater.Inflate(Resource.Layout.ContactListItem, null);
                view = context.LayoutInflater.Inflate(Resource.Layout.VoteListItem, null);
            }
            //var contactName = view.FindViewById<TextView>(Resource.Id.ContactName);
            //var contactImage = view.FindViewById<ImageView>(Resource.Id.ContactImage);
            //contactName.Text = this.voteListItems[position].targetMember;
            VoteListItem vItem = this.voteListItems[position];
            view.FindViewById<TextView>(Resource.Id.memberName).Text = vItem.targetMember;
            view.FindViewById<TextView>(Resource.Id.voteType).Text = "Type of vote: " + vItem.voteType;       
            view.FindViewById<TextView>(Resource.Id.balanceChangeText).Text = "Balance change: " + vItem.balanceChange.ToString();
            view.FindViewById<TextView>(Resource.Id.statusText).Text = vItem.voteStatus  + ": " + vItem.statusText;
            view.FindViewById<TextView>(Resource.Id.descriptionText).Text = vItem.description;
            voteYesButton = view.FindViewById<Button>(Resource.Id.voteYesButton);
            voteNoButton = view.FindViewById<Button>(Resource.Id.voteNoButton);
            TextView disableReason = view.FindViewById<TextView>(Resource.Id.disableReason);

            HouseholdMember member = ((VoteActivity)this.context).currentMember;
            bool alreadyVoted = false;
            if (member != null)
            {
                alreadyVoted = vItem.membersVotedIDs.Contains(member.Id);
            }
            if (vItem.voteStatus != "In Progress" || alreadyVoted)
            {
                voteYesButton.Enabled = false;
                voteNoButton.Enabled = false;
                disableReason.Text = vItem.voteStatus == "In Progress" ? "Already voted" : "Voting ended";
                disableReason.Visibility = ViewStates.Visible;
            }
            else
            {
                voteYesButton.Enabled = true;
                voteNoButton.Enabled = true;
                disableReason.Visibility = ViewStates.Invisible;
            }

            voteYesButton.Tag = position;
            voteNoButton.Tag = position;

            voteYesButton.Click += voteClick;
            voteNoButton.Click += voteClick;

            return view;
        }


        private void voteClick(object sender, EventArgs e)
        {
            try
            {
                int position = int.Parse((((Button)sender).Tag).ToString());
                VoteListItem item = this.voteListItems[position];

                if (sender == this.voteYesButton)
                {
                    ((VoteActivity)this.context).sendVote(true, item.voteID);
                }
                else if (sender == this.voteNoButton)
                {
                    ((VoteActivity)this.context).sendVote(false, item.voteID);
                }
            }
            catch
            {

            }
            ((VoteActivity)this.context).updateDisplay();
        }
    }
}