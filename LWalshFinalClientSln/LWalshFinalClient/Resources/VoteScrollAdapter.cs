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
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);
            }
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = this.voteListItems[position].targetMember;
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = this.voteListItems[position].voteType;
            return view;
        }
    }
}