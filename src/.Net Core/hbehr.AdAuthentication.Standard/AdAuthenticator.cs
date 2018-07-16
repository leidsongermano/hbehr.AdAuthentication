/*
 * The MIT License (MIT)
 * Copyright (c) 2014 - 2018 Leidson Germano

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Controls;

namespace hbehr.AdAuthentication.Standard
{
    public class AdAuthenticator
    {
        internal LdapConfigurationSection LdapConfiguration => LdapConfigurationSection.Current();

        public string GetLdapPath()
        {
            return LdapConfiguration.Path;
        }

        public AdUser AuthenticateAndReturnUser(string login, string password)
        {
            new Validator(this)
                .ValidateConfiguration()
                .ValidateParameters(login, password)
                .ValidateUserPasswordAtAd(login, password);

            login = RemoveDomainFromLogin(login);

            return GetUserFromAdBy(login);
        }

        private static string RemoveDomainFromLogin(string login)
        {
            if (login.Contains(@"\"))
                login = login.Split('\\').Last();
            if (login.Contains("@"))
                login = login.Split('@').First();
            return login;
        }

        public LdapConnection StartConnection()
        {
            LdapConnection ldapConnection = new LdapConnection();
            ldapConnection.Connect(LdapConfiguration.Host, LdapConfiguration.Port);
            ldapConnection.Bind(LdapConnection.Ldap_V3, LdapConfiguration.User,
                LdapConfiguration.UserPassword);
            return ldapConnection;
        }

        public IEnumerable<LdapEntry> SearchBy(LdapFilter filter)
        {
            List<LdapEntry> results = new List<LdapEntry>();

            using (LdapConnection ldapConnection = StartConnection())
            {
                if (filter.TotalResults > 1)
                {
                    LdapFilter countFilter = new LdapFilter
                    {
                        OrderBy = filter.OrderBy,
                        TotalPerPage = 1,
                        SearchCriteria = filter.SearchCriteria,
                        SearchPath = filter.SearchPath
                    };
                    ExecuteSearch(countFilter, ldapConnection, (searchResults) => searchResults.Count());
                    filter.ContentCount = countFilter.ContentCount;
                }

                while (filter.ContentCount == -1 || filter.TotalResults < filter.ContentCount)
                {
                    ExecuteSearch(filter, ldapConnection, (searchResults) => ProcessResults(searchResults, results));

                    filter.TotalResults = results.Count;
                    if (filter.SinglePage)
                        break;
                }

                if (ldapConnection.Connected)
                    ldapConnection.Disconnect();
            }

            return results;
        }

        private void ExecuteSearch(LdapFilter filter, LdapConnection ldapConnection, Action<LdapSearchResults> middleAction)
        {
            LdapSearchConstraints constraints = ldapConnection.SearchConstraints;
            SetLdapControls(filter, constraints);

            LdapSearchResults searchResults = ldapConnection.Search(
                filter.SearchPath, LdapConnection.SCOPE_SUB,
                filter.SearchCriteria, null, false, constraints);

            middleAction(searchResults);
            TotalResultsCallback(filter, searchResults);
        }

        private static void ProcessResults(LdapSearchResults searchResults, ICollection<LdapEntry> results)
        {
            while (searchResults.HasMore())
            {
                try
                {
                    LdapEntry entry = searchResults.Next();
                    results.Add(entry);
                }
                catch (LdapException ldapException)
                {
                    if (!(ldapException is LdapReferralException))
                    {
                        break;
                    }
                }
            }
        }

        private static void TotalResultsCallback(LdapFilter pageInfo, LdapSearchResults searchResults)
        {
            LdapControl[] controls = searchResults.ResponseControls;
            if (controls == null)
            {
                pageInfo.ContentCount = pageInfo.TotalResults;
            }
            else
            {
                foreach (LdapControl control in controls)
                {
                    if (control is LdapVirtualListResponse response)
                    {
                        pageInfo.ContentCount = response.ContentCount;
                    }
                }
            }
        }

        private void SetLdapControls(LdapFilter pageInfo, LdapSearchConstraints constraints)
        {
            LdapControl[] ldapControls = {
                new LdapSortControl(new LdapSortKey(pageInfo.OrderBy ?? LdapConfiguration.Attribute.UniqueName),
                    true),
                new LdapVirtualListControl(pageInfo.TotalResults, 0, pageInfo.TotalPerPage-1, pageInfo.ContentCount)
            };
            constraints.setControls(ldapControls);
        }

        public IEnumerable<AdGroup> GetAdGroups(string loginWithPath = null)
        {
            new Validator(this).ValidateConfiguration();
            string searchCriteria = $"(objectClass={LdapConfiguration.ObjectClass.Group})";
            if (!string.IsNullOrWhiteSpace(loginWithPath))
                searchCriteria = $"(&{searchCriteria}({LdapConfiguration.Attribute.GroupMember}={loginWithPath}))";

            LdapFilter filter = new LdapFilter
            {
                SearchPath = LdapConfiguration.Path,
                SearchCriteria = searchCriteria
            };

            IEnumerable<LdapEntry> results = SearchBy(filter);

            return results.Select(found => new AdGroup
            {
                Code = found.GetUniqueName(),
                Name = found.GetDisplayName()
            });
        }

        public AdUser GetUserFromAdBy(string login)
        {
            new Validator(this).ValidateConfiguration();
            string searchCriteria =
                $"(&(objectClass={LdapConfiguration.ObjectClass.User})({LdapConfiguration.Attribute.UniqueName}={login}))";

            LdapFilter filter = new LdapFilter
            {
                SearchPath = LdapConfiguration.Path,
                SearchCriteria = searchCriteria
            };
            LdapEntry userEntry = SearchBy(filter).FirstOrDefault();

            if (userEntry == null)
                throw new Exception(
                    $"User {login} not found on {LdapConfiguration.Path} using ObjectClass {LdapConfiguration.ObjectClass.User} and uniqueName {LdapConfiguration.Attribute.UniqueName}");

            return new AdUser(userEntry, GetAdGroups(userEntry.GetDistinguishedName()));
        }

        public IEnumerable<AdUser> GetAllUsers()
        {
            new Validator(this).ValidateConfiguration();
            string searchCriteria = $"(objectclass={LdapConfiguration.ObjectClass.User})";

            LdapFilter filter = new LdapFilter
            {
                SearchPath = LdapConfiguration.Path,
                SearchCriteria = searchCriteria
            };
            IEnumerable<LdapEntry> results = SearchBy(filter);

            return results.Select(found => new AdUser(found));
        }

        public IEnumerable<AdUser> GetUsersByFilter(string text, int page, out int total, int itemsPerPage = 5)
        {
            string searchCriteria =
                $"(&(objectClass={LdapConfiguration.ObjectClass.User})({LdapConfiguration.Attribute.UniqueName}=*{text}*))";

            LdapFilter filter = new LdapFilter
            {
                SearchPath = LdapConfiguration.Path,
                SearchCriteria = searchCriteria,
                TotalPerPage = itemsPerPage,
                CurrentPage = page,
                SinglePage = true
            };
            IEnumerable<LdapEntry> results = SearchBy(filter);

            total = filter.ContentCount;
            return results.Select(found => new AdUser(found));
        }

        public IEnumerable<AdUser> GetUsersByNameFilter(string text, int page, out int total, int itemsPerPage = 5)
        {
            string searchCriteria =
                $"(&(objectClass={LdapConfiguration.ObjectClass.User})({LdapConfiguration.Attribute.DisplayName}=*{text}*))";

            LdapFilter filter = new LdapFilter
            {
                SearchPath = LdapConfiguration.Path,
                SearchCriteria = searchCriteria,
                TotalPerPage = itemsPerPage,
                CurrentPage = page,
                OrderBy = LdapConfiguration.Attribute.DisplayName,
                SinglePage = true
            };
            IEnumerable<LdapEntry> results = SearchBy(filter);

            total = filter.ContentCount;
            return results.Select(found => new AdUser(found));
        }
    }
}
