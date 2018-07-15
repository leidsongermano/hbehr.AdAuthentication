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
using Novell.Directory.Ldap;

namespace hbehr.AdAuthentication.Standard
{
    internal class Validator
    {
        private readonly AdAuthenticator _adAuthenticator;
        
        public Validator(AdAuthenticator adAuthenticator)
        {
            _adAuthenticator = adAuthenticator;
        }

        internal Validator ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_adAuthenticator.LdapConfiguration.Host))
            {
                throw new Exception("LDAP Host not configured");
            }
            return this;
        }

        internal Validator ValidateParameters(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentNullException(nameof(login));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }
            return this;
        }

        internal Validator ValidateUserPasswordAtAd(string login, string password)
        {
            using (LdapConnection ldapConnection = new LdapConnection())
            {
                ldapConnection.Connect(_adAuthenticator.LdapConfiguration.Host, _adAuthenticator.LdapConfiguration.Port);
                ldapConnection.Bind(LdapConnection.Ldap_V3, login, password);

                if (ldapConnection.Connected)
                    ldapConnection.Disconnect();
                return this;
            }
        }
    }
}