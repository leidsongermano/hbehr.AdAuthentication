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
    public class LdapConfigurationSection: ConfigurationSection
    {
        [ConfigurationProperty("host", DefaultValue = "", IsRequired = true)]
        public string Host
        {
            get => (string)this["host"];
            set => this["host"] = value;
        }

        [ConfigurationProperty("path", DefaultValue = "", IsRequired = true)]
        public string Path
        {
            get => (string)this["path"];
            set => this["path"] = value;
        }

        [ConfigurationProperty("domain", DefaultValue = "", IsRequired = false)]
        public string Domain
        {
            get => (string)this["domain"];
            set => this["domain"] = value;
        }

        [ConfigurationProperty("port", DefaultValue = "389", IsRequired = false)]
        public int Port
        {
            get => (int)this["port"];
            set => this["port"] = value;
        }

        [ConfigurationProperty("user", DefaultValue = "", IsRequired = true)]
        public string User
        {
            get => (string)this["user"];
            set => this["user"] = value;
        }

        [ConfigurationProperty("userPassword", DefaultValue = "", IsRequired = true)]
        public string UserPassword
        {
            get => (string)this["userPassword"];
            set => this["userPassword"] = value;
        }

        [ConfigurationProperty("objectClasses", IsRequired = false)]
        public LdapCustomObjectClasses ObjectClass
        {
            get => (LdapCustomObjectClasses)this["objectClasses"];
            set => this["objectClasses"] = value;
        }

        [ConfigurationProperty("attributes", IsRequired = false)]
        public LdapCustomAttributes Attribute
        {
            get => (LdapCustomAttributes)this["attributes"];
            set => this["attributes"] = value;
        }
    }
}
