using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Identity2_Rbac.Startup))]
namespace Identity2_Rbac
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
