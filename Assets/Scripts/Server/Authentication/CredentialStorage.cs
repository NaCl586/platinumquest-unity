using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Server.Authentication
{
    public class CredentialStorage
    {
        private const string Key = "Credential";

        public void Save(Credential credential)
        {
            string json = JsonConvert.SerializeObject(credential);

            string encrypted = PasswordEncryption.Encrypt(json);

            PlayerPrefs.SetString(Key, encrypted);

            PlayerPrefs.Save();
        }

        public Credential? Load()
        {
            if (!PlayerPrefs.HasKey(Key))
                return null;

            try
            {
                // Get encrypted JSON
                string encrypted = PlayerPrefs.GetString(Key);

                // Decrypt it first
                string json = PasswordEncryption.Decrypt(encrypted);

                // Then deserialize the decrypted JSON
                Credential? credential = JsonConvert.DeserializeObject<Credential>(json);

                return credential;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Failed to load remembered credentials. "
                        + $"Stored credential will be cleared. "
                        + $"Reason: {ex.Message}"
                );

                Clear();

                return null;
            }
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
