using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CrmPlayground
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [XmlRoot("Data")]
    public sealed class LiveDevice
    {
        #region Properties
        [XmlAttribute("version")]
        public int Version { get; set; }

        [XmlElement("User")]
        public DeviceUserName User { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "This is required for proper XML Serialization")]
        [XmlElement("Token")]
        public XmlNode Token { get; set; }

        [XmlElement("Expiry")]
        public string Expiry { get; set; }

        [XmlElement("ClockSkew")]
        public string ClockSkew { get; set; }
        #endregion
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class DeviceUserName
    {
        private string _encryptedPassword;
        private string _decryptedPassword;
        private bool _encryptedValueIsUpdated;

        #region Constants
        private const string UserNamePrefix = "11";
        #endregion

        #region Constructors
        public DeviceUserName()
        {
            this.UserNameType = "Logical";
        }
        #endregion

        #region Properties
        [XmlAttribute("username")]
        public string DeviceName { get; set; }

        [XmlAttribute("type")]
        public string UserNameType { get; set; }

        [XmlElement("Pwd")]
        public string EncryptedPassword
        {
            get
            {
                this.ThrowIfNoEncryption();

                if (!this._encryptedValueIsUpdated) {
                    this._encryptedPassword = this.Encrypt(this._decryptedPassword);
                    this._encryptedValueIsUpdated = true;
                }

                return this._encryptedPassword;
            }

            set
            {
                this.ThrowIfNoEncryption();
                this.UpdateCredentials(value, null);
            }
        }

        public string DeviceId
        {
            get
            {
                return UserNamePrefix + DeviceName;
            }
        }

        [XmlIgnore] 
        public string DecryptedPassword
        {
            get
            {
                return this._decryptedPassword;
            }

            set
            {
                this.UpdateCredentials(null, value);
            }
        }

        private bool IsEncryptionEnabled
        {
            get
            {
                return false;
            }
        }
        #endregion

        #region Methods
        public ClientCredentials ToClientCredentials()
        {
            ClientCredentials credentials = new ClientCredentials();
            credentials.UserName.UserName = this.DeviceId;
            credentials.UserName.Password = this.DecryptedPassword;

            return credentials;
        }

        private void ThrowIfNoEncryption()
        {
            if (!this.IsEncryptionEnabled) {
                throw new NotSupportedException("Not supported when DeviceIdManager.UseEncryptionApis is false.");
            }
        }

        private void UpdateCredentials(string encryptedValue, string decryptedValue)
        {
            bool isValueUpdated = false;
            if (string.IsNullOrEmpty(encryptedValue) && string.IsNullOrEmpty(decryptedValue)) {
                isValueUpdated = true;
            } else if (string.IsNullOrEmpty(encryptedValue)) {
                if (this.IsEncryptionEnabled) {
                    encryptedValue = this.Encrypt(decryptedValue);
                    isValueUpdated = true;
                } else {
                    encryptedValue = null;
                    isValueUpdated = false;
                }
            } else {
                this.ThrowIfNoEncryption();

                decryptedValue = this.Decrypt(encryptedValue);
                isValueUpdated = true;
            }

            this._encryptedPassword = encryptedValue;
            this._decryptedPassword = decryptedValue;
            this._encryptedValueIsUpdated = isValueUpdated;
        }

        private string Encrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            byte[] encryptedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private string Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            byte[] decryptedBytes = ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser);
            if (null == decryptedBytes || 0 == decryptedBytes.Length) {
                return null;
            }

            return Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);
        }
        #endregion
    }
}
