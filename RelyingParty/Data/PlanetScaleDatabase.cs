using System.Data;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MySql.Data.MySqlClient;
using System.Text.Json;
using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;
using RelyingParty.Models;

public class PlanetScaleDatabase
{
    private readonly string _connectionString;

    public PlanetScaleDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<MySqlConnection> GetMySqlConnection(CancellationToken cancellationToken = default)
    {
        var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }

    public async Task<Fido2User> GetOrAddUser(string username, Func<Fido2User> createUserFunc)
    {
        await using var conn = await GetMySqlConnection();
        Fido2User user;

        // Read and return the user if it already exists
        await using (var selectCmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", conn))
        {
            selectCmd.Parameters.AddWithValue("@username", username);
            await using var reader = await selectCmd.ExecuteReaderAsync();
        
            if (reader.Read())
            {
                user = new Fido2User
                {
                    DisplayName = reader.GetString("display_name"),
                    Name = reader.GetString("username"),
                    Id = (byte[])reader["user_id"]
                };
                return user;
            }
        } // The reader will be disposed of here

        // user does not exist. Create it and return it
        user = createUserFunc();
        await using var insertCmd = new MySqlCommand(
            "INSERT INTO users (username, display_name, user_id) VALUES (@username, @display_name, @user_id)",
            conn);
        insertCmd.Parameters.AddWithValue("@username", user.Name);
        insertCmd.Parameters.AddWithValue("@display_name", user.DisplayName);
        insertCmd.Parameters.AddWithValue("@user_id", user.Id);
        await insertCmd.ExecuteNonQueryAsync();

        return user;
    }


    public async Task UpdateUserJsonMetadata(string username, string newJsonMetadata)
    {
        await using var conn = await GetMySqlConnection();

        // Create the command to update the user's json_metadata
        await using var cmd = new MySqlCommand(
            "UPDATE users SET json_metadata = @json_metadata WHERE username = @username",
            conn);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@json_metadata", newJsonMetadata);

        // Execute the command
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<StoredCredential>> GetCredentialsByUser(Fido2User user)
    {
        var credentials = new List<StoredCredential>();

        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand("SELECT * FROM credentials WHERE user_handle = @user_handle", conn);
        cmd.Parameters.AddWithValue("@user_handle", user.Id); // Using user_handle instead of user_id

        await using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // Convert SET type for transports into an array of AuthenticatorTransport
            var transportsSet = reader.GetString("transports");
            var transports = transportsSet.Split(',')
                .Select(t => (AuthenticatorTransport)Enum.Parse(typeof(AuthenticatorTransport), t, true))
                .ToArray();

            // Parse device_public_keys from JSON and convert into List<byte[]>
            var devicePublicKeysJson = reader.GetString("device_public_keys");
            var devicePublicKeys = JsonConvert.DeserializeObject<List<byte[]>>(devicePublicKeysJson) ??
                                   new List<byte[]>();

            credentials.Add(new StoredCredential
            {
                Id = (byte[])reader["credential_id"],
                PublicKey = (byte[])reader["public_key"],
                SignCount = reader.GetUInt32("sign_count"),
                Transports = transports,
                IsBackupEligible = reader.GetBoolean("is_backup_eligible"),
                IsBackedUp = reader.GetBoolean("is_backed_up"),
                AttestationObject = (byte[])reader["attestation_object"],
                AttestationClientDataJSON = (byte[])reader["attestation_client_data_json"],
                DevicePublicKeys = devicePublicKeys,
                Descriptor = new PublicKeyCredentialDescriptor(
                    (PublicKeyCredentialType)Enum.Parse(typeof(PublicKeyCredentialType),
                        reader.GetString("descriptor_type"), true),
                    (byte[])reader["descriptor_id"]),
                UserHandle = (byte[])reader["user_handle"],
                AttestationFormat = reader.GetString("attestation_format"),
                RegDate = reader.GetDateTime("reg_date"),
                AaGuid = new Guid((byte[])reader["aa_guid"])
            });
        }


        return credentials;
    }

    public async Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId,
        CancellationToken cancellationToken)
    {
        var users = new List<Fido2User>();

        var conn = await GetMySqlConnection(cancellationToken);

        await using var cmd = new MySqlCommand(
            "SELECT u.* FROM users u JOIN credentials c ON u.user_id = c.user_handle WHERE c.credential_id = @credential_id",
            conn);
        cmd.Parameters.AddWithValue("@credential_id", credentialId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new Fido2User
            {
                DisplayName = reader.GetString("display_name"),
                Name = reader.GetString("username"),
                Id = (byte[])reader["user_id"]
            });
        }


        return users;
    }

    public string GetValueFromJsonByKey(string jsonString, string key)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty(key, out JsonElement valueElement))
            {
                return valueElement.ToString();
            }
            else
            {
                throw new KeyNotFoundException($"Key '{key}' not found in the JSON string.");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON string.", ex);
        }
    }

    public string CreateJsonFromDictionary(Dictionary<string, string> dictionary)
    {
        // Serialize the dictionary into a JSON string
        string json = JsonConvert.SerializeObject(dictionary);

        return json;
    }

    public Dictionary<string, string>? CreateDictionaryFromJson(string json)
    {
        // Deserialize the JSON string into a dictionary
        Dictionary<string, string>? dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        return dictionary;
    }

    public async Task AddCredentialToUser(Fido2User user, StoredCredential storedCredential)
    {
        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand(@"INSERT INTO credentials 
    (credential_id, public_key, sign_count, transports, is_backup_eligible, is_backed_up, attestation_object, attestation_client_data_json, descriptor_type, descriptor_id, user_handle, attestation_format, reg_date, aa_guid) 
    VALUES 
    (@credential_id, @public_key, @sign_count, @transports, @is_backup_eligible, @is_backed_up, @attestation_object, @attestation_client_data_json, @descriptor_type, @descriptor_id, @user_handle, @attestation_format, @reg_date, @aa_guid)",
            conn);
        cmd.Parameters.AddWithValue("@credential_id", storedCredential.Id);
        cmd.Parameters.AddWithValue("@public_key", storedCredential.PublicKey);
        cmd.Parameters.AddWithValue("@sign_count", storedCredential.SignCount);
        cmd.Parameters.AddWithValue("@transports",
            string.Join(",",
                storedCredential.Transports.Select(t =>
                    t.ToString()
                        .ToLower()))); // Assuming the enum ToString() gives the correct string representation
        cmd.Parameters.AddWithValue("@is_backup_eligible", storedCredential.IsBackupEligible);
        cmd.Parameters.AddWithValue("@is_backed_up", storedCredential.IsBackedUp);
        cmd.Parameters.AddWithValue("@attestation_object", storedCredential.AttestationObject);
        cmd.Parameters.AddWithValue("@attestation_client_data_json", storedCredential.AttestationClientDataJSON);
        cmd.Parameters.AddWithValue("@descriptor_type",
            storedCredential.Descriptor.Type.ToString()
                .ToLower()); // Assuming the enum ToString() gives the correct string representation
        cmd.Parameters.AddWithValue("@descriptor_id", storedCredential.Descriptor.Id);
        cmd.Parameters.AddWithValue("@user_handle", storedCredential.UserHandle);
        cmd.Parameters.AddWithValue("@attestation_format", storedCredential.AttestationFormat);
        cmd.Parameters.AddWithValue("@reg_date", storedCredential.RegDate);
        cmd.Parameters.AddWithValue("@aa_guid", storedCredential.AaGuid.ToByteArray());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Fido2User?> GetUser(string username)
    {
        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", conn);
        cmd.Parameters.AddWithValue("@username", username);
        await using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var user = new Fido2User
            {
                DisplayName = reader.GetString("display_name"),
                Name = reader.GetString("username"),
                Id = (byte[])reader["user_id"]
            };

            return user;
        }


        return null;
    }

    public async Task<string?> GetUserJsonMetadata(string username)
    {
        await using var conn = await GetMySqlConnection();

        // Create the command to get the user's json_metadata
        await using var cmd = new MySqlCommand("SELECT json_metadata FROM users WHERE username = @username", conn);
        cmd.Parameters.AddWithValue("@username", username);

        await using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            // Get the json_metadata field as a string
            var result = reader.GetString("json_metadata");

            return result;
        }


        // User not found
        return null;
    }


    public async Task<StoredCredential?> GetCredentialById(byte[] id)
    {
        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand("SELECT * FROM credentials WHERE credential_id = @credential_id", conn);
        cmd.Parameters.AddWithValue("@credential_id", id);
        await using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            // Convert SET type for transports into an array of AuthenticatorTransport
            var transportsSet = reader.GetString("transports");
            var transports = transportsSet.Split(',')
                .Select(t => (AuthenticatorTransport)Enum.Parse(typeof(AuthenticatorTransport), t, true))
                .ToArray();

            // Parse device_public_keys from JSON and convert into List<byte[]>
            var devicePublicKeysJson = reader.GetString("device_public_keys");
            var devicePublicKeys = JsonConvert.DeserializeObject<List<byte[]>>(devicePublicKeysJson) ??
                                   new List<byte[]>();

            return new StoredCredential
            {
                Id = (byte[])reader["credential_id"],
                PublicKey = (byte[])reader["public_key"],
                SignCount = reader.GetUInt32("sign_count"),
                Transports = transports,
                IsBackupEligible = reader.GetBoolean("is_backup_eligible"),
                IsBackedUp = reader.GetBoolean("is_backed_up"),
                AttestationObject = (byte[])reader["attestation_object"],
                AttestationClientDataJSON = (byte[])reader["attestation_client_data_json"],
                DevicePublicKeys = devicePublicKeys,
                Descriptor = new PublicKeyCredentialDescriptor(
                    (PublicKeyCredentialType)Enum.Parse(typeof(PublicKeyCredentialType),
                        reader.GetString("descriptor_type"), true),
                    (byte[])reader["descriptor_id"]),
                UserHandle = (byte[])reader["user_handle"],
                AttestationFormat = reader.GetString("attestation_format"),
                RegDate = reader.GetDateTime("reg_date"),
                AaGuid = new Guid((byte[])reader["aa_guid"])
            };
        }


        return null;
    }

    public async Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle,
        CancellationToken cancellationToken)
    {
        var credentials = new List<StoredCredential>();
        var conn = await GetMySqlConnection(cancellationToken);

        await using var cmd = new MySqlCommand("SELECT * FROM credentials WHERE user_id = @user_id", conn);
        cmd.Parameters.AddWithValue("@user_id", userHandle);
        await using var reader = cmd.ExecuteReader();
        while (await reader.ReadAsync(cancellationToken))
        {
            // Convert SET type for transports into an array of AuthenticatorTransport
            var transportsSet = reader.GetString("transports");
            var transports = transportsSet.Split(',')
                .Select(t => (AuthenticatorTransport)Enum.Parse(typeof(AuthenticatorTransport), t, true))
                .ToArray();

            // Parse device_public_keys from JSON and convert into List<byte[]>
            var devicePublicKeysJson = reader.GetString("device_public_keys");
            var devicePublicKeys = JsonConvert.DeserializeObject<List<byte[]>>(devicePublicKeysJson) ??
                                   new List<byte[]>();

            credentials.Add(new StoredCredential
            {
                Id = (byte[])reader["credential_id"],
                PublicKey = (byte[])reader["public_key"],
                SignCount = reader.GetUInt32("sign_count"),
                Transports = transports,
                IsBackupEligible = reader.GetBoolean("is_backup_eligible"),
                IsBackedUp = reader.GetBoolean("is_backed_up"),
                AttestationObject = (byte[])reader["attestation_object"],
                AttestationClientDataJSON = (byte[])reader["attestation_client_data_json"],
                DevicePublicKeys = devicePublicKeys,
                Descriptor = new PublicKeyCredentialDescriptor(
                    (PublicKeyCredentialType)Enum.Parse(typeof(PublicKeyCredentialType),
                        reader.GetString("descriptor_type"), true),
                    (byte[])reader["descriptor_id"]),
                UserHandle = (byte[])reader["user_handle"],
                AttestationFormat = reader.GetString("attestation_format"),
                RegDate = reader.GetDateTime("reg_date"),
                AaGuid = new Guid((byte[])reader["aa_guid"])
            });
        }


        return credentials;
    }

    public async Task UpdateCounter(byte[] credentialId, uint counter)
    {
        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand(
            "UPDATE credentials SET sign_count = @counter WHERE credential_id = @credential_id", conn);
        cmd.Parameters.AddWithValue("@counter", counter);
        cmd.Parameters.AddWithValue("@credential_id", credentialId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateDevicePublicKeys(byte[] credentialId, List<byte[]> newDevicePublicKeys)
    {
        await using var conn = await GetMySqlConnection();
        await using var cmd = new MySqlCommand(
            "UPDATE credentials SET device_public_keys = @device_public_keys WHERE credential_id = @credential_id",
            conn);
        cmd.Parameters.AddWithValue("@device_public_keys", JsonConvert.SerializeObject(newDevicePublicKeys));
        cmd.Parameters.AddWithValue("@credential_id", credentialId);
        await cmd.ExecuteNonQueryAsync();
    }

    // update the table didt, with the new value of the DIDT public key.
    // If there is an existing value, it will be overwritten. Where user_id is the user's ID,
    // and didt is the DIDT public key. Key is the user_id.
    public async Task UpsertDidt(byte[] credential_id, byte[] didt, byte[] signature)
    {
        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand(
            "INSERT INTO DIDT (credential_id, didt, signature) VALUES (@credential_id, @didt, @signature) ON DUPLICATE KEY UPDATE didt = @didt, signature = @signature",
            conn);
        cmd.Parameters.AddWithValue("@credential_id", credential_id);
        cmd.Parameters.AddWithValue("@didt", didt);
        cmd.Parameters.AddWithValue("@signature", signature);
        await cmd.ExecuteNonQueryAsync();
    }

    // select the Didt where the user_id is the user's ID.
    public async Task<LSIGSign.Models.SignedDidt?> GetSignedDidt(byte[] credentialId)
    {
        await using var conn = await GetMySqlConnection();

        await using var cmd = new MySqlCommand("SELECT * FROM DIDT WHERE credential_id = @credential_id", conn);
        cmd.Parameters.AddWithValue("@credential_id", credentialId);
        await using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var didt = (byte[])reader["didt"];
            var signature = (byte[])reader["signature"];
            var signedDidt = new LSIGSign.Models.SignedDidt(didt, signature);
            return signedDidt;
        }

        return null;
    }
}