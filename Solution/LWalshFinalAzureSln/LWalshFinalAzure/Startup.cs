using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(LWalshFinalAzure.Startup))]

namespace LWalshFinalAzure
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureMobileApp(app);
        }
    }
}