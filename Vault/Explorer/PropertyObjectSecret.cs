﻿using Microsoft.Azure.KeyVault;
using Microsoft.PS.Common.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PS.Common.Vault.Explorer
{
    /// <summary>
    /// Secret object to edit via PropertyGrid
    /// </summary>
    public class PropertyObjectSecret : PropertyObject
    {
        /// <summary>
        /// Original secret
        /// </summary>
        private readonly Secret _secret;

        public PropertyObjectSecret(Secret secret, PropertyChangedEventHandler propertyChanged) :
            base(secret.SecretIdentifier, secret.Tags, secret.Attributes.Enabled, secret.Attributes.Expires, secret.Attributes.NotBefore, propertyChanged)
        {
            _secret = secret;
            _contentType = ContentTypeEnumConverter.GetValue(secret.ContentType);
            _value = _contentType.FromRawValue(secret.Value);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetCustomTags()
        {
            // Add tags based on all named groups in the value regex
            Match m = SecretKind.ValueRegex.Match(Value);
            if (m.Success)
            {
                for (int i = 0; i < m.Groups.Count; i++)
                {
                    string groupName = SecretKind.ValueRegex.GroupNameFromNumber(i);
                    if (groupName == i.ToString()) continue; // Skip unnamed groups
                    yield return new KeyValuePair<string, string>(groupName, m.Groups[i].Value);
                }
            }
        }

        public SecretAttributes ToSecretAttributes() => new SecretAttributes()
        {
            Enabled = Enabled,
            Expires = Expires,
            NotBefore = NotBefore
        };


        public string GetClipboardValue()
        {
            return ContentType.IsCertificate() ? CertificateValueObject.FromValue(Value).Password : Value;
        }

        public byte[] GetValueAsByteArray()
        {
            return ContentType.IsCertificate() ? Convert.FromBase64String(CertificateValueObject.FromValue(Value).Data) : Encoding.UTF8.GetBytes(Value);
        }

        public void SaveToFile(string fullName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullName));
            switch (ContentTypeUtils.FromExtension(Path.GetExtension(fullName)))
            {
                case ContentType.Secret: // Serialize the entire secret as encrypted JSON for current user
                    File.WriteAllText(fullName, new SecretFile(_secret).Serialize());
                    break;
                default:
                    File.WriteAllBytes(fullName, GetValueAsByteArray());
                    break;
            }
        }
    }
}
