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

namespace LWalshFinalClient.Resources
{
    class VoteScrollAdapter : BaseAdapter<VoteListItem>
    {
        private IReadOnlyList<VoteListItem> voteListItems;
        private Activity context;
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
            view.FindViewById<TextView>(Resource.Id.memberName).Text = this.voteListItems[position].targetMember;
            view.FindViewById<TextView>(Resource.Id.voteType).Text = "Type of vote: " + this.voteListItems[position].voteType;
            view.FindViewById<TextView>(Resource.Id.balanceChangeText).Text = "Balance change: " + this.voteListItems[position].balanceChange.ToString();
            view.FindViewById<TextView>(Resource.Id.statusText).Text = this.voteListItems[position].statusText;
            view.FindViewById<TextView>(Resource.Id.descriptionText).Text = this.voteListItems[position].description;

            return view;
        }
    }
}