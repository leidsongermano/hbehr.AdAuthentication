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

using System.Configuration;

namespace hbehr.AdAuthentication.Standard
{
    public class LdapCustomAttributes: ConfigurationElement
    {
        [ConfigurationProperty("uniqueName", DefaultValue = "sAMAccountName", IsRequired = false)]
        public string UniqueName
        {
            get => (string)this["uniqueName"];
            set => this["uniqueName"] = value;
        }

        [ConfigurationProperty("displayName", DefaultValue = "DisplayName", IsRequired = false)]
        public string DisplayName
        {
            get => (string)this["displayName"];
            set => this["displayName"] = value;
        }

        [ConfigurationProperty("groupMember", DefaultValue = "member", IsRequired = false)]
        public string GroupMember
        {
            get => (string)this["groupMember"];
            set => this["groupMember"] = value;
        }

        [ConfigurationProperty("distinguishedName", DefaultValue = "distinguishedName", IsRequired = false)]
        public string DistinguishedName
        {
            get => (string)this["distinguishedName"];
            set => this["distinguishedName"] = value;
        }

        [ConfigurationProperty("mail", DefaultValue = "mail", IsRequired = false)]
        public string Mail
        {
            get => (string)this["mail"];
            set => this["mail"] = value;
        }

        [ConfigurationProperty("telephoneNumber", DefaultValue = "telephoneNumber", IsRequired = false)]
        public string TelephoneNumber
        {
            get => (string)this["telephoneNumber"];
            set => this["telephoneNumber"] = value;
        }

        [ConfigurationProperty("company", DefaultValue = "company", IsRequired = false)]
        public string Company
        {
            get => (string)this["company"];
            set => this["company"] = value;
        }
    }
}
