using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Ryan_BugTracker.Startup))]
namespace Ryan_BugTracker
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
