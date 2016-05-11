using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using LWalshFinalClient.Resources;
using System.Web.Script.Serialization;

namespace LWalshFinalClient
{
    /// <summary>
    /// This class represents the main and first screen any user sees when the app is launched.
    /// It's a landing page where the user can login, quit the app, create a household, view 
    /// their household, and households their friends belongs to. Clicking on one of these households
    /// brings the user to that household's screen.
    /// </summary>
    [Activity(Label = "Home", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public MobileServiceClient client;

        Button loginButton;
        Button quitButton;
        Button createHHButton;                
        bool isLoggedIn;
        bool isRegistered;        
        bool isHomeScreen;
        string currentUserID;
        List<HHListItem> HHListItems;
        ListView householdsListView;
        TextView HHNameTextView;
        TextView HHLandlordTextView;
        Switch friendsHHSwitch;
        LinearLayout hhSwitchLayout;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //initialize mobile service client
            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
           
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Assign buttons, views, etc
            this.loginButton = FindViewById<Button>(Resource.Id.loginButton);
            this.quitButton = FindViewById<Button>(Resource.Id.quitButton);
            this.createHHButton = FindViewById<Button>(Resource.Id.createHHButton);            
            this.householdsListView = FindViewById<ListView>(Resource.Id.HHListView);
            this.HHNameTextView = FindViewById<TextView>(Resource.Id.HHName);
            this.HHLandlordTextView = FindViewById<TextView>(Resource.Id.HHLandlord);
            this.friendsHHSwitch = FindViewById<Switch>(Resource.Id.whichHHSwitch);
            this.hhSwitchLayout = FindViewById<LinearLayout>(Resource.Id.hhSwitchLayout);

            //add event handling
            this.loginButton.Click += loginClick;
            this.createHHButton.Click += createHHClick;
            this.quitButton.Click += quitClick;
            this.householdsListView.ItemClick += householdsListViewItemClick;
            this.friendsHHSwitch.CheckedChange += friendsHHSwitchChange;            

            //set default booleans
            this.isLoggedIn = false;
            this.isRegistered = false;                        
                        
            //if the activity was instantiated by an intent with parameters, get the parameters
            //and initialize the appropriate class variables
            //this is done so that if the user navigates back to the home screen from one of the
            //other screens, the user's authentication data is preserved so the screen can show
            //the relevant data
            getIntentParameters();
            //update the display
            updateDisplay();
        }

        /// <summary>
        /// A method that retrieves the parameters from the intent that created
        /// the activity. This ensures that the user authentication data is preserved.
        /// If these parameters exist, the MobileServicesClient is overwritten
        /// by a new client that uses the user's authentication data.
        /// </summary>
        private void getIntentParameters()
        {
            if (this.Intent.Extras != null)
            {                
                var isLoggedInString = this.Intent.Extras.GetString("isLoggedIn");
                this.currentUserID = this.Intent.Extras.GetString("currentUserID");
                if (isLoggedInString == "true")
                {
                    this.isLoggedIn = true;
                    this.isRegistered = true;
                }
                string clientUserID = this.Intent.Extras.GetString("clientUserID");
                this.client.CurrentUser = new MobileServiceUser(clientUserID);
                this.client.CurrentUser.MobileServiceAuthenticationToken = this.Intent.Extras.GetString("clientAuthToken");
            }
        }

        /// <summary>
        /// Updates the display.
        /// </summary>
        private async void updateDisplay()
        {
            this.loginButton.Text = this.isLoggedIn ? "Logout" : "Login";
            //only display households if the user is logged in and registered
            this.householdsListView.Visibility = (this.isLoggedIn && this.isRegistered) ? ViewStates.Visible : ViewStates.Invisible;
            this.hhSwitchLayout.Visibility = (this.isLoggedIn && this.isRegistered) ? ViewStates.Visible : ViewStates.Invisible;
            this.HHNameTextView.Visibility = ViewStates.Gone;
            this.HHLandlordTextView.Visibility = ViewStates.Gone;
            this.householdsListView.Enabled = true;
            //retrieve the households
            await updateHouseholdList();
            //display the households
            displayHouseholds();
        }

        /// <summary>
        /// Makes a call to the Azure API to get the list of households. If the toggle switch
        /// is for the user's household, it gets the user's households and assigns them to the class
        /// scope households list; else if the toggle switch is for the user's friends it will retrieve
        /// the user's friends' households.
        /// </summary>
        /// <returns>Bool indicating success</returns>
        private async Task<bool> updateHouseholdList()
        {
            this.HHListItems = null;
            //if the user is viewing his/her own households, get households for that user
            if(!this.friendsHHSwitch.Checked)
            {
                try
                {
                    //make a call to the househlds controller to get the user's households
                    string uri = "household/byuser/" + this.currentUserID.Substring(9);
                    JToken result = await this.client.InvokeApiAsync(uri, HttpMethod.Get, null);

                    //if successful request, assign the user's households to the class scope households list
                    if(result != null && result.HasValues)
                    {
                        List<HHListItem> households = new List<HHListItem>();

                        foreach(var hh in result)
                        {
                            HHListItem hhListItem = new HHListItem();
                                                        
                            hhListItem.name = (string)((JObject)hh)["name"];                            
                            hhListItem.landlordName = (string)((JObject)hh)["landlordName"];
                            hhListItem.id = (string)((JObject)hh)["id"];
                            
                            households.Add(hhListItem);
                        }

                        this.HHListItems = households;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            //else get the list of households the users friends are a part of but the user is not a member
            else
            {
                try
                {
                    //make a call to the friend controller to get the user's friends' households
                    string uri = "user/byid/" + this.currentUserID.Substring(9) + "/friends";
                    JToken result = await this.client.InvokeApiAsync(uri, HttpMethod.Get, null);

                    //if successful request, assign these households to the class scope households list
                    if (result != null && result.HasValues)
                    {
                        List<HHListItem> households = new List<HHListItem>();

                        foreach (var hh in result)
                        {
                            HHListItem hhListItem = new HHListItem();

                            hhListItem.name = (string)((JObject)hh)["name"];
                            hhListItem.landlordName = (string)((JObject)hh)["landlordName"];
                            hhListItem.id = (string)((JObject)hh)["id"];

                            households.Add(hhListItem);
                        }

                        this.HHListItems = households;
                    }
                }
                catch (Exception ex)
                {

                }            
            }
            return true;
        }

        /// <summary>
        /// Displays the list of households to the user as a scrollable list. This is accomplished by using
        /// an adapter (HHScrollAdapter) which binds the data to the listview (householdsListView).
        /// </summary>
        private void displayHouseholds()
        {
            //if there are households, display them
            if (this.HHListItems != null && this.HHListItems.Count > 0)
            {
                HHScrollAdapter householdsAdapter = new HHScrollAdapter(this, this.HHListItems);
                this.householdsListView.Adapter = householdsAdapter;
                this.householdsListView.Visibility = ViewStates.Visible;
            }
            //else hide the list view
            else
            {
                this.householdsListView.Visibility = ViewStates.Gone;
            }
        }

        /// <summary>
        /// Event handler triggered when the switch between user and friend households is toggled.
        /// The result is that the display is updated.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void friendsHHSwitchChange(Object sender, EventArgs e)
        {
            updateDisplay();
        }

        /// <summary>
        /// Event handler that triggers when the login button is clicked. If the user is not logged
        /// in it will prompt Facebook's authentication screen. If the user is already logged in it 
        /// will log the user out. 
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private async void loginClick(Object sender, EventArgs e)
        {
            try {
                //if user isn't logged in; log the user in
                if (client.CurrentUser == null || client.CurrentUser.UserId == null)
                {
                    this.loginButton.Enabled = false;
                    MobileServiceAuthenticationProvider providerType = MobileServiceAuthenticationProvider.Facebook;
                    await AuthenticateUserAsync(providerType);
                    this.isLoggedIn = true;
                    this.isRegistered = await registerUser();
                    this.loginButton.Enabled = true;
                }
                //else logout
                else
                {
                    this.isLoggedIn = false;
                    this.isRegistered = false;
                    this.currentUserID = "";
                    this.HHListItems = null;
                    await client.LogoutAsync();
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetMessage("You are now logged out");
                    builder.Create().Show();
                }
            }
            catch(Exception ex)
            {

            }
            //update the display
            updateDisplay();
        }

        /// <summary>
        /// Authenticates the user through the given IDP.
        /// </summary>
        /// <param name="providerType">IDP type for authentication.</param>
        public async Task AuthenticateUserAsync(MobileServiceAuthenticationProvider providerType)
        {

            while (client.CurrentUser == null || client.CurrentUser.UserId == null)
            {
                string errorMessage = "";

                try
                {
                    // Authenticate using provider type passed in. 
                    await client.LoginAsync(this, providerType);                   
                }
                catch (InvalidOperationException ex)
                {
                    errorMessage = "You must log in. Login Required" + ex.Message;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                if (errorMessage != "")
                {
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetMessage(errorMessage);
                    builder.Create().Show();
                }
            }
        }

        /// <summary>
        /// Event handler triggered when a user clicks the "Create Household" button. This
        /// method sends a request to Azure Household resource to create a new household. The
        /// new household appears in the user's household list after the screen is updated.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private async void createHHClick(Object sender, EventArgs e)
        {
            string message = "";
            //check to make sure the user is logged in; else the request is denied
            if (!this.isLoggedIn || !this.isRegistered)
            {
                message = "You must be logged in to create a new household.";
            }
            else
            {
                try
                {
                    //call the household resource to create a new household and display a success message
                    JToken result = await this.client.InvokeApiAsync("household/create", HttpMethod.Post, null);

                    if (result.HasValues)
                    {
                        message = (string)result["message"];
                        updateDisplay();
                    }
                }
                catch (MobileServiceInvalidOperationException ex)
                {
                    message = ex.Message;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(message);
            builder.Create().Show();            
        }
        
        /// <Summary>
        /// Quits the app.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        async void quitClick(object sender, EventArgs e)
        {
            this.FinishAffinity();
        }


        /// <summary>
        /// Registers the user with the Azure service if they are logged in.
        /// </summary>
        /// <returns>Bool indicating success</returns>
        private async Task<bool> registerUser()
        {
            string message = "";
            if (!this.isLoggedIn)
            {
                message = "Registration failed, please try again.";
                return false;
            }
            else
            {
                try
                {
                    //make a call to the registration resource
                    JToken result = await this.client.InvokeApiAsync("registration", null);

                    if (result.HasValues)
                    {
                        //set the current user id for use in future calls
                        this.currentUserID = (string)result["message"];                        
                        message = "You are logged in and may now access your households!";
                        return true;
                    }
                }
                catch (MobileServiceInvalidOperationException ex)
                {
                    message = ex.Message;                    
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(message);
            builder.Create().Show();
            return false;     
        }

        /// <summary>
        /// Event handler triggered by a user clicking on one of the households in the list view.
        /// The user is then brought to the Household screen for that household.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void householdsListViewItemClick(Object sender, AdapterView.ItemClickEventArgs e)
        {
            //check to make sure there are household items in the list
            //the HHListItem object represents an item in the list view which is a simplified
            //version of a household for display purposes
            if (this.HHListItems != null && this.HHListItems.Count > 0)
            {
                //retrieve the household List Item the user clicked on
                HHListItem item = this.HHListItems[e.Position];

                //prepare a new Household Activity (screen) to be launched
                //we want to preserve the data about the user and login status (user should be logged in)
                //when the household screen is created
                //create a new activity and intent
                Type activityType = typeof(HouseholdActiviy);
                Intent newActivity = new Intent(this, activityType);
                var bundle = new Bundle();
                //put the relevant data fields into the bundle as strings. These will be retrieved
                //by the new household screen
                bundle.PutString("MyData", "Data from Activity1");
                bundle.PutString("isLoggedIn", "true");
                bundle.PutString("currentUserID", this.currentUserID);
                bundle.PutString("currentHHID", item.id);
                bundle.PutString("clientUserID", this.client.CurrentUser.UserId);
                bundle.PutString("clientAuthToken", this.client.CurrentUser.MobileServiceAuthenticationToken);
                newActivity.PutExtras(bundle);
                
                //launch the new activity/screen
                StartActivity(newActivity);
            }
        }
    }
}

