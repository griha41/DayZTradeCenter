using DayZTradeCenter.UI.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace DayZTradeCenter.UI.Web.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "DayZTradeCenter.UI.Web.Models.ApplicationDbContext";
        }

        protected override void Seed(ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            // Uncomment to debug code-first migrations code
            //if (System.Diagnostics.Debugger.IsAttached == false)
            //    System.Diagnostics.Debugger.Launch();

            CreateRoles(context);
            CreateUsers(context);
        }

        #region Helpers

        private static void CreateRoles(ApplicationDbContext context)
        {
            DefineRole(context, AdministratorRole);

            context.SaveChanges();
        }

        private static void CreateUsers(ApplicationDbContext context)
        {
            var mgr = new ApplicationUserManager(new UserStore<ApplicationUser>(context));

            DefineUser(mgr, DefaultUsers.Admin);
            DefineUser(mgr, DefaultUsers.TestUser);
        }

        private static void DefineRole(ApplicationDbContext context, string role)
        {
            context.Roles.AddOrUpdate(r => r.Name, new IdentityRole(role));
        }

        private static void DefineUser(ApplicationUserManager mgr, User user)
        {
            var email = user.UserName.ToLower() + "@mail.com";

            var applicationUser = new ApplicationUser
            {
                UserName = user.UserName,
                Email = email,
                EmailConfirmed = true
            };
            var result = mgr.Create(applicationUser, "qwerty");

            if (result.Succeeded)
            {
                if (user.IsAdmin)
                {
                    mgr.AddToRole(applicationUser.Id, AdministratorRole);
                }

                mgr.AddLogin(
                    applicationUser.Id,
                    new UserLoginInfo("Steam", "http://steamcommunity.com/openid/id/" + user.SteamId));
            }
        }

        #endregion

        private const string AdministratorRole = "Administrator";
    }
}