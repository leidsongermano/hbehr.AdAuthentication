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
using System.Configuration;
using System.Linq;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Controls;

namespace hbehr.AdAuthentication.Standard
{
    public class AdAuthenticator
    {
        public AdAuthenticator()
        {
            string sectionClass = nameof(LdapConfigurationSection);
            string section = Char.ToLowerInvariant(sectionClass[0]) + sectionClass.Substring(1);
            LdapConfiguration = (LdapConfigurationSection)ConfigurationManager.GetSection($"{section}/{section.Replace("Section", string.Empty)}");
        }

        internal LdapConfigurationSection LdapConfiguration { get; }

        public AdAuthenticator ConfigureSetLdapHost(string ldapHost)
        {
            LdapConfiguration.Host = ldapHost;
            return this;
        }

        public AdUser AuthenticateAndReturnUser(string login, string password)
        {
            new Validator(this)
                .ValidateConfiguration()
                .ValidateParameters(login, password)
                .ValidateUserPasswordAtAd(login, password);

            return GetUserFromAdBy(login);
        }

        public LdapConnection StartConnection()
        {
            LdapConnection ldapConnection = new LdapConnection();
            ldapConnection.Connect(LdapConfiguration.Host, LdapConfiguration.Port);
            ldapConnection.Bind(LdapConnection.Ldap_V3, LdapConfiguration.User,
                LdapConfiguration.UserPassword);
            return ldapConnection;
        }

        public IEnumerable<LdapEntry> SearchBy(string searchPath, string searchCriteria, LdapPagination pageInfo = null)
        {
            List<LdapEntry> results = new List<LdapEntry>();
            if (pageInfo == null)
                pageInfo = new LdapPagination();

            using (LdapConnection ldapConnection = StartConnection())
            {
                LdapSearchConstraints constraints = ldapConnection.SearchConstraints;
                while (pageInfo.TotalResults == -1 || pageInfo.TotalResults < pageInfo.ContentCount)
                {
                    SetLdapControls(pageInfo, constraints);

                    LdapSearchResults searchResults = ldapConnection.Search(
                        searchPath, LdapConnection.SCOPE_SUB,
                        searchCriteria, null, false, constraints);

                    ProcessResults(searchResults, results);

                    TotalResultsCallback(pageInfo, searchResults);
                }

                if (ldapConnection.Connected)
                    ldapConnection.Disconnect();
            }

            return results;
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

        private static void TotalResultsCallback(LdapPagination pageInfo, LdapSearchResults searchResults)
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

        private void SetLdapControls(LdapPagination pageInfo, LdapSearchConstraints constraints)
        {
            LdapControl[] ldapControls = {
                new LdapSortControl(new LdapSortKey(pageInfo.OrderBy ?? LdapConfiguration.Attribute.UniqueName),
                    true),
                new LdapVirtualListControl(pageInfo.TotalResults, 0, pageInfo.TotalPerPage, pageInfo.ContentCount)
            };
            constraints.setControls(ldapControls);
        }

        public IEnumerable<AdGroup> GetAdGroups(string loginWithPath = null)
        {
            new Validator(this).ValidateConfiguration();
            string searchCriteria = $"(objectclass={LdapConfiguration.ObjectClass.Group})";
            if (!string.IsNullOrWhiteSpace(loginWithPath))
                searchCriteria = $"(&{searchCriteria}({LdapConfiguration.Attribute.GroupMember}={loginWithPath}))";

            IEnumerable<LdapEntry> results = SearchBy(LdapConfiguration.Path, searchCriteria);

            return results.Select(found => new AdGroup
            {
                Code = found.getAttribute(LdapConfiguration.Attribute.UniqueName).StringValue,
                Name = found.getAttribute(LdapConfiguration.Attribute.DisplayName).StringValue
            });
        }

        public AdUser GetUserFromAdBy(string login)
        {
            new Validator(this).ValidateConfiguration();
            string searchCriteria =
                $"(&(objectClass={LdapConfiguration.ObjectClass.User})({LdapConfiguration.Attribute.UniqueName}=*{login}*))";
            LdapEntry userEntry = SearchBy(LdapConfiguration.Path, searchCriteria).FirstOrDefault();

            return new AdUser(userEntry, GetAdGroups(userEntry.GetDistinguishedName()));
        }

        public IEnumerable<AdUser> GetAllUsers()
        {
            new Validator(this).ValidateConfiguration();
            string searchCriteria = $"(objectclass={LdapConfiguration.ObjectClass.User})";

            IEnumerable<LdapEntry> results = SearchBy(LdapConfiguration.Path, searchCriteria);

            return results.Select(found => new AdUser(found));
        }

        public IEnumerable<AdUser> GetUsersByFilter(string text, int page, out int total, int itemsPerPage = 5)
        {
            LdapPagination pageInfo = new LdapPagination
            {
                TotalPerPage = itemsPerPage,
                CurrentPage = page
            };

            string searchCriteria =
                $"(&(objectClass={LdapConfiguration.ObjectClass.User})({LdapConfiguration.Attribute.UniqueName}=*{text}*))";
            IEnumerable<LdapEntry> results = SearchBy(LdapConfiguration.Path, searchCriteria, pageInfo);

            total = pageInfo.ContentCount;
            return results.Select(found => new AdUser(found));
        }

        public IEnumerable<AdUser> GetUsersByNameFilter(string text, int page, out int total, int itemsPerPage = 5)
        {
            LdapPagination pageInfo = new LdapPagination
            {
                TotalPerPage = itemsPerPage,
                CurrentPage = page,
                OrderBy = LdapConfiguration.Attribute.DisplayName
            };

            string searchCriteria =
                $"(&(objectClass={LdapConfiguration.ObjectClass.User})({LdapConfiguration.Attribute.DisplayName}=*{text}*))";
            IEnumerable<LdapEntry> results = SearchBy(LdapConfiguration.Path, searchCriteria, pageInfo);

            total = pageInfo.ContentCount;
            return results.Select(found => new AdUser(found));
        }
    }
}
