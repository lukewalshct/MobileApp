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

namespace LWalshFinalClient.Resources
{
    class MemberScrollAdapter : BaseAdapter<MemberListItem>
    {
        private IReadOnlyList<MemberListItem> memberListItems;
        private Activity context;
        public MemberScrollAdapter(Activity context, IReadOnlyList<MemberListItem> memberListItems) : base() {
            this.context = context;
            this.memberListItems = memberListItems;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override MemberListItem this[int position]
        {
            get { return this.memberListItems[position]; }
        }
        public override int Count
        {
            get { return this.memberListItems.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
            {
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);
            }
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = this.memberListItems[position].name;
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = this.memberListItems[position].balance;
            return view;
        }
    }
}