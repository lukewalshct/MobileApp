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
    /// <summary>
    /// Adapter for the voting list view which displays all the votes for a household.
    /// </summary>
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
                view = context.LayoutInflater.Inflate(Resource.Layout.VoteListItem, null);
            }
            
            //display the data in the appropriate fields
            VoteListItem vItem = this.voteListItems[position];
            view.FindViewById<TextView>(Resource.Id.memberName).Text = vItem.targetMember;
            view.FindViewById<TextView>(Resource.Id.voteType).Text = "Type of vote: " + vItem.voteType;       
            view.FindViewById<TextView>(Resource.Id.balanceChangeText).Text = "Balance change: " + vItem.balanceChange.ToString();
            view.FindViewById<TextView>(Resource.Id.statusText).Text = vItem.voteStatus  + ": " + vItem.statusText;
            view.FindViewById<TextView>(Resource.Id.descriptionText).Text = vItem.description;
            //the voting buttons are defined here in the scroll adapater
            voteYesButton = view.FindViewById<Button>(Resource.Id.voteYesButton);
            voteNoButton = view.FindViewById<Button>(Resource.Id.voteNoButton);
            //disable reason - tells the user why voting is disabled, either b/c they already voted or voting ended
            TextView disableReason = view.FindViewById<TextView>(Resource.Id.disableReason);

            //get the current member
            HouseholdMember member = ((VoteActivity)this.context).currentMember;
            //disable the voting buttons if the user already voted or voting ended
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

            //assign the scroll item's position to the button tag
            //this will then be used as a way to determine which vote it was for
            voteYesButton.Tag = position;
            voteNoButton.Tag = position;

            //add event handlers to the voting button click
            voteYesButton.Click += voteClick;
            voteNoButton.Click += voteClick;

            return view;
        }

        /// <summary>
        /// Event handler that triggers when one of the voting buttons in the scroll adapter
        /// is clicked. Sends arguments to the sendVote method in the Vote activity class that
        /// then makes a call to the vote resource.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void voteClick(object sender, EventArgs e)
        {
            try
            {
                //parse the position from the button's tag
                int position = int.Parse((((Button)sender).Tag).ToString());
                VoteListItem item = this.voteListItems[position];

                //call thee sendVote method
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
            //call the Vote activity's update display method
            ((VoteActivity)this.context).updateDisplay();
        }
    }
}