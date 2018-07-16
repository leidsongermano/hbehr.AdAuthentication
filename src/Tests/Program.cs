using System;
using System.Collections.Generic;
using System.Linq;
using hbehr.AdAuthentication.Standard;
using Novell.Directory.Ldap;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            AdAuthenticator adAuthenticator = new AdAuthenticator();
            ExecuteWithExceptionHandler("StartConnection", () =>
            {
                LdapConnection connection = adAuthenticator.StartConnection();
                Console.WriteLine($"IsConnected: {connection.Connected}");
                connection.Disconnect();
                connection.Dispose();
            });

            ExecuteWithExceptionHandler("AuthenticateAndReturnUser", () =>
            {
                AdUser user = adAuthenticator.AuthenticateAndReturnUser(@"uid=admin,ou=system", "secret");
                Console.WriteLine($"Result User: {user.Name}");
                user.AdGroups.ToList().ForEach(group => Console.WriteLine($"Group: {group.Name ?? group.Code}"));
            });

            ExecuteWithExceptionHandler("GetAdGroups", () =>
            {
                IEnumerable<AdGroup> groups = adAuthenticator.GetAdGroups();
                groups.ToList().ForEach(group => Console.WriteLine($"Group: {group.Name ?? group.Code}"));
                Console.WriteLine($"Total: {groups.Count()}");
            });

            ExecuteWithExceptionHandler("GetAdGroupsWithLogin", () =>
            {
                IEnumerable<AdGroup> groups = adAuthenticator.GetAdGroups("admin");
                groups.ToList().ForEach(group => Console.WriteLine($"Group: {group.Name ?? group.Code}"));
            });

            ExecuteWithExceptionHandler("GetAllUsers", () =>
            {
                IEnumerable<AdUser> users = adAuthenticator.GetAllUsers();
                users.ToList().ForEach(user => Console.WriteLine($"user: {user.Name ?? user.Login}"));
                Console.WriteLine($"Total: {users.Count()}");
            });

            ExecuteWithExceptionHandler("GetUserFromAdBy", () =>
            {
                AdUser user = adAuthenticator.GetUserFromAdBy("admin");
                Console.WriteLine($"Result: {user.Name}");
                user.AdGroups.ToList().ForEach(group => Console.WriteLine($"Group: {group.Name ?? group.Code}"));
            });

            ExecuteWithExceptionHandler("GetUsersByFilter", () =>
            {
                IEnumerable<AdUser> users = adAuthenticator.GetUsersByFilter("hook",2,out int total);
                Console.WriteLine($"Total: {total}");
                users.ToList().ForEach(user => Console.WriteLine($"user: {user.Name}"));
            });

            ExecuteWithExceptionHandler("GetUsersByNameFilter", () =>
            {
                IEnumerable<AdUser> users = adAuthenticator.GetUsersByNameFilter("hook", 2, out int total);
                Console.WriteLine($"Total: {total}");
                users.ToList().ForEach(user => Console.WriteLine($"user: {user.Name}"));
            });

            ExecuteWithExceptionHandler("SearchBy", () =>
            {
                LdapFilter filter = new LdapFilter
                {
                    SearchPath = adAuthenticator.GetLdapPath(),
                    SearchCriteria = "(&(objectclass=person)(sAMAccountName=*h*))"
                };

                IEnumerable<LdapEntry> users = adAuthenticator.SearchBy(filter);
                users.ToList().ForEach(user => Console.WriteLine($"user: {user.getAttribute("sAMAccountName")}"));
            });

            Console.ReadLine();
        }

        private static void ExecuteWithExceptionHandler(string actionName, Action action)
        {
            try
            {
                Console.WriteLine($"STARTING [{actionName}] ...");
                action();
                Console.WriteLine($"SUCCESS [{actionName}]!");
            }
            catch (LdapException ldapException)
            {
                Console.WriteLine(
                    $"ERROR: [{actionName}] {ldapException.Message} - {ldapException.Cause?.Message}");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: [{actionName}] {exception.Message}");
            }
            finally
            {
                Console.WriteLine();
            }
        }
    }
}