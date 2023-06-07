using Fido2NetLib;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json.Converters;
using Algorand.Algod.Model.Transactions;

namespace LSIGSign.Models
{
    public class LSIGSignOptionsModel
    {
        // to identify the users didt
        public string Base64EncodedCredentialID { get; set; }

        // the tx to sign with the smartsig
        public Transaction Transaction { get; set; }

        // the client session key signature of the tx
        public string Base64EncodedTxSessSignature { get; set; }
    }

    public class SignedDidt
    {
        public byte[] didt { get; set; }
        public byte[] signature { get; set; }

        // constructor
        public SignedDidt(byte[] didt, byte[] signature)
        {
            this.didt = didt;
            this.signature = signature;
        }
    }
}
