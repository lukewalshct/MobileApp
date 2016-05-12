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
    class MessageScrollAdapter : BaseAdapter<MessageListItem>
    {
        private IReadOnlyList<MessageListItem> messageListItems;
        private Activity context;

        public MessageScrollAdapter(Activity context, IReadOnlyList<MessageListItem> messageListItems) : base() {
            this.context = context;
            this.messageListItems = messageListItems;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override MessageListItem this[int position]
        {
            get { return this.messageListItems[position]; }
        }
        public override int Count
        {
            get { return this.messageListItems.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
            {              
                view = context.LayoutInflater.Inflate(Resource.Layout.MessageListItem, null);
            }
            MessageListItem vItem = this.messageListItems[position];

            view.FindViewById<TextView>(Resource.Id.messageText).Text = vItem.message;
            view.FindViewById<TextView>(Resource.Id.senderText).Text = vItem.sender;
            view.FindViewById<TextView>(Resource.Id.timText).Text = vItem.timestamp;       
                        
            return view;
        }        
    }
}