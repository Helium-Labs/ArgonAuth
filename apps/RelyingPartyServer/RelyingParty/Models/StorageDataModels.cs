﻿using Fido2NetLib.Objects;

namespace RelyingParty.Models;


public class StoredCredential
{
    /// <summary>
    /// The Credential ID of the public key credential source.
    /// </summary>
    public byte[] Id { get; set; }

    /// <summary>
    /// The credential public key of the public key credential source.
    /// </summary>
    public byte[] PublicKey { get; set; }

    /// <summary>
    /// The latest value of the signature counter in the authenticator data from any ceremony using the public key credential source.
    /// </summary>
    public uint SignCount { get; set; }

    /// <summary>
    /// The value returned from getTransports() when the public key credential source was registered.
    /// </summary>
    public AuthenticatorTransport[]? Transports { get; set; }

    /// <summary>
    /// The value of the BE flag when the public key credential source was created.
    /// </summary>
    public bool IsBackupEligible { get; set; }

    /// <summary>
    /// The latest value of the BS flag in the authenticator data from any ceremony using the public key credential source.
    /// </summary>
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// The value of the attestationObject attribute when the public key credential source was registered. 
    /// Storing this enables the Relying Party to reference the credential's attestation statement at a later time.
    /// </summary>
    public byte[] AttestationObject { get; set; }

    /// <summary>
    /// The value of the clientDataJSON attribute when the public key credential source was registered. 
    /// Storing this in combination with the above attestationObject item enables the Relying Party to re-verify the attestation signature at a later time.
    /// </summary>
    public byte[] AttestationClientDataJSON { get; set; }

    public List<byte[]> DevicePublicKeys { get; set; }

    public PublicKeyCredentialDescriptor Descriptor { get; set; }

    public byte[] UserHandle { get; set; }

    public string AttestationFormat { get; set; }

    public DateTime RegDate { get; set; }

    public Guid AaGuid { get; set; }
}
public class AuthExchange
{
    public string JwtClaims { get; set; }
    public string State { get; set; }
    public string CodeChallenge { get; set; }
    // ... include other fields as necessary ...
}

// Define the model for an email verification entry
public class EmailVerification
{
    public string Email { get; set; }
    public string CodeHash { get; set; }
    public DateTime Timestamp { get; set; }
}