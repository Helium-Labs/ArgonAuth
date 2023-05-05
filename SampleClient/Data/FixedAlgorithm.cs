namespace GameServer
{
    //Required because unable to configure NSwag server side.
    public enum Algorithm
    {
        [System.Runtime.Serialization.EnumMember(Value = @"RS1")]
        RS1 = -65535,

        [System.Runtime.Serialization.EnumMember(Value = @"RS512")]
        RS512 = -259,

        [System.Runtime.Serialization.EnumMember(Value = @"RS384")]
        RS384 = -258,

        [System.Runtime.Serialization.EnumMember(Value = @"RS256")]
        RS256 = -257,

        [System.Runtime.Serialization.EnumMember(Value = @"ES256K")]
        ES256K = -47,

        [System.Runtime.Serialization.EnumMember(Value = @"PS512")]
        PS512 = -39,

        [System.Runtime.Serialization.EnumMember(Value = @"PS384")]
        PS384 = -38,

        [System.Runtime.Serialization.EnumMember(Value = @"PS256")]
        PS256 = -37,

        [System.Runtime.Serialization.EnumMember(Value = @"ES512")]
        ES512 = -36,

        [System.Runtime.Serialization.EnumMember(Value = @"ES384")]
        ES384 = -35,

        [System.Runtime.Serialization.EnumMember(Value = @"EdDSA")]
        EdDSA = -8,

        [System.Runtime.Serialization.EnumMember(Value = @"-7")]
        ES256 = -7,

    }
}
