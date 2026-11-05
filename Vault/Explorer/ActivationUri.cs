// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

namespace Microsoft.Vault.Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using Microsoft.Vault.Library;

    public class ActivationUri : VaultLinkUri
    {
        private static readonly string OnlineActivationUri = $"https://elize.blob.core.windows.net/{Utils.ProductName}/{Utils.ProductName}.application";
        public static readonly ActivationUri Empty = new ActivationUri("vault:");

        public ActivationUri(string vaultUri) : base(vaultUri) { }

        public new static ActivationUri Parse()
        {
            string vaultUri = (Environment.GetCommandLineArgs().Length == 2) ? Environment.GetCommandLineArgs()[1] : null;
            // Arguments were not passed at all or activation happened via Application Reference (.appref-ms)
            if (string.IsNullOrEmpty(vaultUri)) return Empty;
            if (vaultUri.StartsWith("file:", StringComparison.CurrentCultureIgnoreCase)) return Empty;
            // Online activation
            if (vaultUri.StartsWith(OnlineActivationUri, StringComparison.CurrentCultureIgnoreCase))
            {
                vaultUri = vaultUri.Substring(OnlineActivationUri.Length).TrimStart('?');
            }
            if (string.IsNullOrEmpty(vaultUri)) return Empty;
            return new ActivationUri(vaultUri.TrimEnd('/', '\\'));
        }

        public void PerformAction(Vault vault)
        {
            switch (Action)
            {
                case Microsoft.Vault.Library.Action.Default:
                    CopyToClipboard(vault);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Action), $"Invalid action {Action}");
            }
        }

        private void CopyToClipboard(Vault vault)
        {
            PropertyObject po = null;
            switch (Collection)
            {
                case VaultUriCollection.Keys:
                    return;
                case VaultUriCollection.Certificates:
                    var cb = vault.GetCertificateAsync(ItemName, CancellationToken.None).GetAwaiter().GetResult();
                    var cert = vault.GetCertificateWithExportableKeysAsync(ItemName, Version, CancellationToken.None).GetAwaiter().GetResult();
                    po = new PropertyObjectCertificate(cb, cb.Policy, cert, null);
                    break;
                case VaultUriCollection.Secrets:
                    var s = vault.GetSecretAsync(ItemName, Version, CancellationToken.None).GetAwaiter().GetResult();
                    po = new PropertyObjectSecret(s, null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Collection), $"Invalid endpoint {Collection}");
            }
            po.CopyToClipboard(true);
        }

    }
}
