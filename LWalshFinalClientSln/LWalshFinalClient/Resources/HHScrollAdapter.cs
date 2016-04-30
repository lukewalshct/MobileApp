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
    class HHScrollAdapter : BaseAdapter<HHListItem>
    {
        private IReadOnlyList<HHListItem> HHListItems;
        private Activity context;
        public HHScrollAdapter(Activity context, IReadOnlyList<HHListItem> HHListItems) : base() {
            this.context = context;
            this.HHListItems = HHListItems;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override HHListItem this[int position]
        {
            get { return this.HHListItems[position]; }
        }
        public override int Count
        {
            get { return this.HHListItems.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
            {
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            }
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = this.HHListItems[position].householdName;
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = this.HHListItems[position].householdLandlord;
            return view;
        }
    }
}