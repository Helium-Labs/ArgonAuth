using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fido2NetLib.Development;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MySql.Data.MySqlClient;
public class PlanetScaleDatabase
{
    private MySqlConnection _conn;

    public PlanetScaleDatabase(string connectionString)
    {
        _conn = new MySqlConnection(connectionString);
    }

    public Fido2User GetOrAddUser(string username, Func<Fido2User> createUserFunc)
    {
        Fido2User user = null;

        _conn.Open();

        using (var cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", _conn))
        {
            cmd.Parameters.AddWithValue("@username", username);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    user = new Fido2User
                    {
                        DisplayName = reader.GetString("display_name"),
                        Name = reader.GetString("username"),
                        Id = (byte[])reader["user_id"]
                    };
                }
            }
        }

        if (user == null)
        {
            user = createUserFunc();

            using (var cmd = new MySqlCommand("INSERT INTO users (username, display_name, user_id) VALUES (@username, @display_name, @user_id)", _conn))
            {
                cmd.Parameters.AddWithValue("@username", user.Name);
                cmd.Parameters.AddWithValue("@display_name", user.DisplayName);
                cmd.Parameters.AddWithValue("@user_id", user.Id);
                cmd.ExecuteNonQuery();
            }
        }

        _conn.Close();

        return user;
    }

    public List<StoredCredential> GetCredentialsByUser(Fido2User user)
    {
        var credentials = new List<StoredCredential>();

        _conn.Open();

        using (var cmd = new MySqlCommand("SELECT * FROM credentials WHERE user_id = @user_id", _conn))
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

        _conn.Close();

        return credentials;
    }

    public async Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        var users = new List<Fido2User>();

        await _conn.OpenAsync(cancellationToken);

        using (var cmd = new MySqlCommand("SELECT u.* FROM users u JOIN credentials c ON u.user_id = c.user_id WHERE c.credential_id = @credential_id", _conn))
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

        _conn.Close();

        return users;
    }

    public void AddCredentialToUser(Fido2User
        user, StoredCredential storedCredential)
    {
        _conn.Open();
        using (var cmd = new MySqlCommand("INSERT INTO credentials (credential_id, public_key, user_id, signature_counter, cred_type, reg_date, aa_guid) VALUES (@credential_id, @public_key, @user_id, @signature_counter, @cred_type, @reg_date, @aa_guid)", _conn))
        {
            cmd.Parameters.AddWithValue("@credential_id", storedCredential.Descriptor.Id);
            cmd.Parameters.AddWithValue("@public_key", storedCredential.PublicKey);
            cmd.Parameters.AddWithValue("@user_id", storedCredential.UserHandle);
            cmd.Parameters.AddWithValue("@signature_counter", storedCredential.SignatureCounter);
            cmd.Parameters.AddWithValue("@cred_type", storedCredential.CredType);
            cmd.Parameters.AddWithValue("@reg_date", storedCredential.RegDate);
            cmd.Parameters.AddWithValue("@aa_guid", storedCredential.AaGuid);
            cmd.ExecuteNonQuery();
        }

        _conn.Close();
    }

    public Fido2User GetUser(string username)
    {
        Fido2User user = null;

        _conn.Open();

        using (var cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username", _conn))
        {
            cmd.Parameters.AddWithValue("@username", username);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    user = new Fido2User
                    {
                        DisplayName = reader.GetString("display_name"),
                        Name = reader.GetString("username"),
                        Id = (byte[])reader["user_id"]
                    };
                }
            }
        }

        _conn.Close();

        return user;
    }

    public StoredCredential GetCredentialById(byte[] id)
    {
        StoredCredential credential = null;

        _conn.Open();

        using (var cmd = new MySqlCommand("SELECT * FROM credentials WHERE credential_id = @credential_id", _conn))
        {
            cmd.Parameters.AddWithValue("@credential_id", id);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    credential = new StoredCredential
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

        _conn.Close();

        return credential;
    }

    public async Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken)
    {
        var credentials = new List<StoredCredential>();

        await _conn.OpenAsync(cancellationToken);

        using (var cmd = new MySqlCommand("SELECT * FROM credentials WHERE user_id = @user_id", _conn))
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

        _conn.Close();

        return credentials;
    }

    public void UpdateCounter(byte[] credentialId, uint counter)
    {
        _conn.Open();

        using (var cmd = new MySqlCommand("UPDATE credentials SET signature_counter = @counter WHERE credential_id = @credential_id", _conn))
        {
            cmd.Parameters.AddWithValue("@counter", counter);
            cmd.Parameters.AddWithValue("@credential_id", credentialId);
            cmd.ExecuteNonQuery();
        }
        _conn.Close();
    }
}