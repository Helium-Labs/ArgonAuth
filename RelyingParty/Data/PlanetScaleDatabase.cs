using System.Data;
using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using RelyingParty.Models;

namespace RelyingParty.Data;

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

    private async Task<List<Fido2User>> ExecuteSqlCommandAndGetUsersAsync(MySqlCommand cmd,
        CancellationToken cancellationToken)
    {
        var users = new List<Fido2User>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new Fido2User
            {
                DisplayName = reader.GetString("display_name"),
                Name = reader.GetString("username"),
                Id = Convert.FromBase64String(reader.GetString("user_id_b64"))
            });
        }

        return users;
    }

    public async Task<Fido2User> GetOrAddUser(string username, Func<Fido2User> createUserFunc)
    {
        await using var conn = await GetMySqlConnection();
        var selectCmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", conn);
        selectCmd.Parameters.AddWithValue("@username", username);
        var users = await ExecuteSqlCommandAndGetUsersAsync(selectCmd, default);

        if (users.Count > 0)
        {
            return users[0];
        }

        // user does not exist. Create it and return it
        Fido2User user = createUserFunc();
        await using var insertCmd = new MySqlCommand(
            "INSERT INTO users (username, display_name, user_id_b64) VALUES (@username, @display_name, @user_id_b64)",
            conn);
        insertCmd.Parameters.AddWithValue("@username", user.Name);
        insertCmd.Parameters.AddWithValue("@display_name", user.DisplayName);
        insertCmd.Parameters.AddWithValue("@user_id_b64", Convert.ToBase64String(user.Id));
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

    public async Task<List<StoredCredential>> GetCredentialsByUser(Fido2User user, CancellationToken cancellationToken)
    {
        await using var conn = await GetMySqlConnection(cancellationToken);

        await using var cmd =
            new MySqlCommand("SELECT * FROM credentials WHERE user_handle_b64 = @user_handle_b64", conn);
        cmd.Parameters.AddWithValue("@user_handle_b64", Convert.ToBase64String(user.Id));

        var credentials = await ReadCredentialsAsync(cmd, cancellationToken);
        // Log the credentials as a JSON string
        Console.WriteLine("Credentials: " + JsonConvert.SerializeObject(credentials));
        return credentials;
    }

    public async Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId,
        CancellationToken cancellationToken)
    {
        var conn = await GetMySqlConnection(cancellationToken);
        var cmdText =
            "SELECT u.* FROM users u JOIN credentials c ON u.user_id_b64 = c.user_handle_b64 WHERE c.credential_id_b64 = @credential_id_b64";
        await using var cmd = new MySqlCommand(
            cmdText,
            conn);
        cmd.Parameters.AddWithValue("@credential_id_b64", Convert.ToBase64String(credentialId));

        var users = await ExecuteSqlCommandAndGetUsersAsync(cmd, cancellationToken);

        return users;
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

    private async Task<List<StoredCredential>> ReadCredentialsAsync(MySqlCommand cmd,
        CancellationToken cancellationToken)
    {
        var credentials = new List<StoredCredential>();
        await using var reader = cmd.ExecuteReader();
        while (await reader.ReadAsync(cancellationToken))
        {
            // Convert SET type for transports into an array of AuthenticatorTransport
            string? transportsSet = reader.IsDBNull(reader.GetOrdinal("transports"))
                ? null
                : reader.GetString("transports");
            var transports = transportsSet?.Split(',')
                .Select(t => (AuthenticatorTransport)Enum.Parse(typeof(AuthenticatorTransport), t, true))
                .ToArray();

            // Parse device_public_keys from JSON and convert into List<byte[]>
            var devicePublicKeys = DecodeDevicePublicKeys(reader.GetString("device_public_keys_b64"));
            // @TODO: Handle actual storage of Public Key Type (public-key or invalid) from database
            credentials.Add(new StoredCredential
            {
                Id = Convert.FromBase64String(reader.GetString("credential_id_b64")),
                PublicKey = Convert.FromBase64String(reader.GetString("public_key_b64")),
                SignCount = reader.GetUInt32("sign_count"),
                Transports = transports,
                IsBackupEligible = reader.GetBoolean("is_backup_eligible"),
                IsBackedUp = reader.GetBoolean("is_backed_up"),
                AttestationObject = Convert.FromBase64String(reader.GetString("attestation_object_b64")),
                AttestationClientDataJSON =
                    Convert.FromBase64String(reader.GetString("attestation_client_data_json_b64")),
                DevicePublicKeys = devicePublicKeys,
                Descriptor = new PublicKeyCredentialDescriptor(
                    PublicKeyCredentialType.PublicKey,
                    Convert.FromBase64String(reader.GetString("descriptor_id_b64"))),
                UserHandle = Convert.FromBase64String(reader.GetString("user_handle_b64")),
                AttestationFormat = reader.GetString("attestation_format"),
                RegDate = reader.GetDateTime("reg_date"),
                AaGuid = new Guid(Convert.FromBase64String(reader.GetString("aa_guid_b64")))
            });
        }

        return credentials;
    }

    private List<byte[]> DecodeDevicePublicKeys(string devicePublicKeysJson)
    {
        // Deserialize the JSON string into a list of Base64 strings
        List<string>? base64Strings = JsonConvert.DeserializeObject<List<string>>(devicePublicKeysJson);
        if (base64Strings == null)
            return new List<byte[]>();
        // Convert each Base64 string back into a byte array and return the resulting list
        return base64Strings.Select(base64 => Convert.FromBase64String(base64)).ToList();
    }

    private string EncodeDevicePublicKeys(List<byte[]> devicePublicKeys)
    {
        return JsonConvert.SerializeObject(
            devicePublicKeys.Select(Convert.ToBase64String).ToList()
        );
    }

    public async Task AddCredentialToUser(StoredCredential storedCredential)
    {
        await using var conn = await GetMySqlConnection();

        // filter out null values from the list
        string devicePublicKeysJson = EncodeDevicePublicKeys(storedCredential.DevicePublicKeys);

        string? transports = null;
        if (storedCredential.Transports != null)
            transports = string.Join(",",
                storedCredential.Transports.Select(t =>
                    t.ToString()
                        .ToLower()));

        var cmdText = @"INSERT INTO credentials 
            (credential_id_b64, public_key_b64, sign_count, transports, is_backup_eligible, 
             is_backed_up, attestation_object_b64, attestation_client_data_json_b64, device_public_keys_b64,
             descriptor_type, descriptor_id_b64, user_handle_b64, attestation_format, 
             reg_date, aa_guid_b64) 
            VALUES 
            (@credential_id_b64, @public_key_b64, @sign_count, @transports, @is_backup_eligible, 
             @is_backed_up, @attestation_object_b64, @attestation_client_data_json_b64, @device_public_keys_b64,
             @descriptor_type, @descriptor_id_b64, @user_handle_b64, @attestation_format, 
             @reg_date, @aa_guid_b64)";

        await using var cmd = new MySqlCommand(cmdText, conn);

        cmd.Parameters.AddWithValue("@credential_id_b64", Convert.ToBase64String(storedCredential.Id));
        cmd.Parameters.AddWithValue("@sign_count", storedCredential.SignCount);
        if (transports != null)
        {
            cmd.Parameters.AddWithValue("@transports", transports);
        }
        else
        {
            cmd.Parameters.AddWithValue("@transports", DBNull.Value);
        }

        if (!string.IsNullOrEmpty(devicePublicKeysJson))
        {
            cmd.Parameters.AddWithValue("@device_public_keys_b64", devicePublicKeysJson);
        }
        else
        {
            cmd.Parameters.AddWithValue("@device_public_keys_b64", DBNull.Value);
        }

        cmd.Parameters.AddWithValue("@is_backup_eligible", storedCredential.IsBackupEligible);
        cmd.Parameters.AddWithValue("@is_backed_up", storedCredential.IsBackedUp);
        cmd.Parameters.AddWithValue("@descriptor_type", "public-key");
        cmd.Parameters.AddWithValue("@attestation_format", storedCredential.AttestationFormat);
        cmd.Parameters.AddWithValue("@reg_date", storedCredential.RegDate);

        cmd.Parameters.AddWithValue("@public_key_b64", Convert.ToBase64String(storedCredential.PublicKey));
        cmd.Parameters.AddWithValue("@attestation_object_b64",
            Convert.ToBase64String(storedCredential.AttestationObject));
        cmd.Parameters.AddWithValue("@attestation_client_data_json_b64",
            Convert.ToBase64String(storedCredential.AttestationClientDataJSON));
        cmd.Parameters.AddWithValue("@descriptor_id_b64", Convert.ToBase64String(storedCredential.Descriptor.Id));
        cmd.Parameters.AddWithValue("@user_handle_b64", Convert.ToBase64String(storedCredential.UserHandle));
        cmd.Parameters.AddWithValue("@aa_guid_b64", Convert.ToBase64String(storedCredential.AaGuid.ToByteArray()));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Fido2User?> GetUser(string username)
    {
        await using var conn = await GetMySqlConnection();
        await using var cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", conn);
        cmd.Parameters.AddWithValue("@username", username);
        var users = await ExecuteSqlCommandAndGetUsersAsync(cmd, default);
        if (users.Count > 0)
        {
            return users[0];
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

    public static bool IsValidUtf8(byte[] data)
    {
        try
        {
            // Attempt to decode the byte array
            Encoding.UTF8.GetString(data);

            // If decoding succeeded without an exception, the data is valid UTF-8
            return true;
        }
        catch (DecoderFallbackException)
        {
            // If a DecoderFallbackException was thrown, the data is not valid UTF-8
            return false;
        }
    }

    public async Task<StoredCredential?> GetCredentialById(byte[] credentialId)
    {
        await using var conn = await GetMySqlConnection();
        var cmdText = "SELECT * FROM credentials WHERE credential_id_b64 = @credential_id_b64";
        await using var cmd = new MySqlCommand(cmdText, conn);
        cmd.Parameters.AddWithValue("@credential_id_b64", Convert.ToBase64String(credentialId));

        var credentials = await ReadCredentialsAsync(cmd, default);
        if (credentials.Count > 0)
        {
            return credentials[0];
        }

        return null;
    }

    public async Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle,
        CancellationToken cancellationToken)
    {
        var conn = await GetMySqlConnection(cancellationToken);

        await using var cmd =
            new MySqlCommand("SELECT * FROM credentials WHERE user_handle_b64 = @user_handle_b64", conn);
        cmd.Parameters.AddWithValue("@user_handle_b64", Convert.ToBase64String(userHandle));

        var credentials = await ReadCredentialsAsync(cmd, cancellationToken);

        return credentials;
    }

    public async Task UpdateCounter(byte[] credentialId, uint counter)
    {
        await using var conn = await GetMySqlConnection();
        var idB64 = Convert.ToBase64String(credentialId);
        await using var cmd = new MySqlCommand(
            "UPDATE credentials SET sign_count = @counter WHERE credential_id_b64 = @credential_id_b64", conn);
        cmd.Parameters.AddWithValue("@counter", counter);
        cmd.Parameters.AddWithValue("@credential_id_b64", idB64);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateDevicePublicKeys(byte[] credentialId, List<byte[]> newDevicePublicKeys)
    {
        var idB64 = Convert.ToBase64String(credentialId);
        await using var conn = await GetMySqlConnection();
        await using var cmd = new MySqlCommand(
            "UPDATE credentials SET device_public_keys = @device_public_keys WHERE credential_id_b64 = @credential_id_b64",
            conn);
        cmd.Parameters.AddWithValue("@device_public_keys", JsonConvert.SerializeObject(newDevicePublicKeys));
        cmd.Parameters.AddWithValue("@credential_id_b64", idB64);
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