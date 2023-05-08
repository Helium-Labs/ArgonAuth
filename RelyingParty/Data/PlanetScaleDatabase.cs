using System.Data;
using System.Text;
using Fido2NetLib.Development;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MySql.Data.MySqlClient;
using System.Threading;

public class PlanetScaleDatabase
{
    private MySqlConnection _conn;
    private readonly string _connectionString;
    public PlanetScaleDatabase(string connectionString)
    {
        _conn = new MySqlConnection(connectionString);
        _connectionString = connectionString;
    }

    public async Task<MySqlConnection> GetMySqlConnection(CancellationToken cancellationToken = default)
    {
        _conn ??= new MySqlConnection(_connectionString);
        if(_conn.State != ConnectionState.Open)
        {
            // it's not open yet, so open it. Don't open a connection that's already open.
            await _conn.OpenAsync(cancellationToken);
        }
        return _conn;
    }

    public async Task<Fido2User> GetOrAddUser(string username, Func<Fido2User> createUserFunc)
    {

        var conn = await GetMySqlConnection();
        
        // Read and return the user if it already exists
        using (var cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", conn))
        {
            cmd.Parameters.AddWithValue("@username", username);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new Fido2User
                    {
                        DisplayName = reader.GetString("display_name"),
                        Name = reader.GetString("username"),
                        Id = (byte[])reader["user_id"]
                    };
                }
            }
        }

        // user does not exist. Create it and return it
        Fido2User user = createUserFunc();
        using (var cmd = new MySqlCommand("INSERT INTO users (username, display_name, user_id) VALUES (@username, @display_name, @user_id)", conn))
        {
            cmd.Parameters.AddWithValue("@username", user.Name);
            cmd.Parameters.AddWithValue("@display_name", user.DisplayName);
            cmd.Parameters.AddWithValue("@user_id", user.Id);
            cmd.ExecuteNonQuery();
        }
        
        return user;
    }

    public async Task<List<StoredCredential>> GetCredentialsByUser(Fido2User user)
    {
        var credentials = new List<StoredCredential>();
            
        var conn = await GetMySqlConnection();
    
        using (var cmd = new MySqlCommand("SELECT * FROM credentials WHERE user_id = @user_id", conn))
        {
            cmd.Parameters.AddWithValue("@user_id", user.Id);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    credentials.Add(new StoredCredential
                    {
                        Descriptor = new PublicKeyCredentialDescriptor((byte[])reader["credential_id"]),
                        PublicKey = (byte[])reader["public_key"],
                        UserHandle = (byte[])reader["user_id"],
                        SignatureCounter = reader.GetUInt32("signature_counter"),
                        CredType = reader.GetString("cred_type"),
                        RegDate = reader.GetDateTime("reg_date"),
                        AaGuid = reader.GetGuid("aa_guid")
                    });
                }
            }
        }

        return credentials;
    }

    public async Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        var users = new List<Fido2User>();

        var conn = await GetMySqlConnection(cancellationToken);

        using (var cmd = new MySqlCommand("SELECT u.* FROM users u JOIN credentials c ON u.user_id = c.user_id WHERE c.credential_id = @credential_id", conn))
        {
            cmd.Parameters.AddWithValue("@credential_id", credentialId);

            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    users.Add(new Fido2User
                    {
                        DisplayName = reader.GetString("display_name"),
                        Name = reader.GetString("username"),
                        Id = (byte[])reader["user_id"]
                    });
                }
            }
        }

        return users;
    }

    public async void AddCredentialToUser(Fido2User user, StoredCredential storedCredential)
    {
        var conn = await GetMySqlConnection();

        using (var cmd = new MySqlCommand("INSERT INTO credentials (credential_id, public_key, user_id, signature_counter, cred_type, reg_date, aa_guid) VALUES (@credential_id, @public_key, @user_id, @signature_counter, @cred_type, @reg_date, @aa_guid)", conn))
        {
            cmd.Parameters.AddWithValue("@credential_id", storedCredential.Descriptor.Id);
            cmd.Parameters.AddWithValue("@public_key", storedCredential.PublicKey);
            cmd.Parameters.AddWithValue("@user_id", storedCredential.UserHandle);
            cmd.Parameters.AddWithValue("@signature_counter", storedCredential.SignatureCounter);
            cmd.Parameters.AddWithValue("@cred_type", storedCredential.CredType);
            cmd.Parameters.AddWithValue("@reg_date", storedCredential.RegDate);
            cmd.Parameters.AddWithValue("@aa_guid","test");
            cmd.ExecuteNonQuery();
        }
    }

    public async Task<Fido2User?> GetUser(string username)
    {
        var conn = await GetMySqlConnection();

        using (var cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", conn))
        {
            cmd.Parameters.AddWithValue("@username", username);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new Fido2User
                    {
                        DisplayName = reader.GetString("display_name"),
                        Name = reader.GetString("username"),
                        Id = (byte[])reader["user_id"]
                    };
                }
            }
        }

        return null;
    }

    public async Task<StoredCredential?> GetCredentialById(byte[] id)
    {

        var conn = await GetMySqlConnection();

        using (var cmd = new MySqlCommand("SELECT * FROM credentials WHERE credential_id = @credential_id", conn))
        {
            cmd.Parameters.AddWithValue("@credential_id", id);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new StoredCredential
                    {
                        Descriptor = new PublicKeyCredentialDescriptor((byte[])reader["credential_id"]),
                        PublicKey = (byte[])reader["public_key"],
                        UserHandle = (byte[])reader["user_id"],
                        SignatureCounter = reader.GetUInt32("signature_counter"),
                        CredType = reader.GetString("cred_type"),
                        RegDate = reader.GetDateTime("reg_date"),
                        AaGuid = reader.GetGuid("aa_guid")
                    };
                }
            }
        }

        conn.Close();

        return null;
    }

    public async Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken)
    {
        var credentials = new List<StoredCredential>();
        var conn = await GetMySqlConnection(cancellationToken);

        using (var cmd = new MySqlCommand("SELECT * FROM credentials WHERE user_id = @user_id", conn))
        {
            cmd.Parameters.AddWithValue("@user_id", userHandle);

            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    credentials.Add(new StoredCredential
                    {
                        Descriptor = new PublicKeyCredentialDescriptor((byte[])reader["credential_id"]),
                        PublicKey = (byte[])reader["public_key"],
                        UserHandle = (byte[])reader["user_id"],
                        SignatureCounter = (uint)reader.GetInt32("signature_counter"),
                        CredType = reader.GetString("cred_type"),
                        RegDate = reader.GetDateTime("reg_date"),
                        AaGuid = reader.GetGuid("aa_guid")
                    });
                }
            }
        }

        return credentials;
    }

    public async void UpdateCounter(byte[] credentialId, uint counter)
    {
        var conn = await GetMySqlConnection();

        using (var cmd = new MySqlCommand("UPDATE credentials SET signature_counter = @counter WHERE credential_id = @credential_id", conn))
        {
            cmd.Parameters.AddWithValue("@counter", counter);
            cmd.Parameters.AddWithValue("@credential_id", credentialId);
            cmd.ExecuteNonQuery();
        }
    }
}