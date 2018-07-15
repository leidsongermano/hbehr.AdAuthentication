using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using hbehr.AdAuthentication.Standard;
using Microsoft.Extensions.Configuration;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Controls;

namespace Tests
{
    class Program
    {
        private static List<string> exitCodes = new List<string> { "quit", "exit", "q", "" };

        static void Main(string[] args)
        {
            try
            {

                AdAuthenticator adAuthenticator = new AdAuthenticator();

                using (LdapConnection ldapConnection = new LdapConnection())
                {
                    LdapSearchConstraints constraints = ldapConnection.SearchConstraints;

                    List<string> names = new List<string>();
                    int totalResults = 1;
                    int contentCount = -1;
                    while (contentCount == -1 || totalResults < contentCount)
                    {
                        //Console.WriteLine(ldapConnection.GetSchemaDN());
                        

                        var ldapControls = new LdapControl[]
                        {
                            //new LdapSortControl(new LdapSortKey("sAMAccountName"), true),
                            //new LdapVirtualListControl(startIndex: totalResults,beforeCount:0,afterCount:1000,contentCount:contentCount)
                        };
                        constraints.setControls(ldapControls);

                        //ldapConnection.Constraints = constraints;

                        LdapSearchResults searchResults = ldapConnection.Search(
                            "DC=radixengrj,DC=matriz", LdapConnection.SCOPE_SUB,
                            "(sAMAccountName=andre.melo)", null, false, constraints);


                        while (searchResults.HasMore())
                        {
                            try
                            {
                                LdapEntry entry = searchResults.Next();
                                names.Add(entry.getAttribute("sAMAccountName").StringValue);
                                Console.WriteLine(
                                    $@"{totalResults++} - {entry.getAttribute("sAMAccountName")}");


                            }
                            catch (LdapException ldapException)
                            {
                                if (!(ldapException is LdapReferralException))
                                {
                                    Console.WriteLine("Search stopped with exception " + ldapException);
                                    break;
                                }
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine($"[ERRO] {exception.Message}");
                            }
                        }

                        // Server should send back a control irrespective of the 
                        // status of the search request
                        LdapControl[] controls = searchResults.ResponseControls;
                        if (controls == null)
                        {
                            Console.WriteLine("No controls returned");
                            contentCount = totalResults;
                        }
                        else
                        {
                            // Multiple controls could have been returned
                            foreach (LdapControl control in controls)
                            {
                                if (control is LdapVirtualListResponse)
                                {
                                    contentCount = ((LdapVirtualListResponse)control).ContentCount;
                                }
                            }
                        }

                    };

                    /* We are done - disconnect */
                    if (ldapConnection.Connected)
                        ldapConnection.Disconnect();

                    Console.WriteLine(names.Count);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}