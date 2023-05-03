function bufferToBase64(buffer) {
    // create a Uint8Array view for the buffer
    var bytes = new Uint8Array(buffer);
    // convert the bytes to a binary string
    var binary = "";
    for (var i = 0; i < bytes.length; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    // encode the binary string to base64
    return window.btoa(binary);
}

async function getCredential(makeAssertionOptions) {
    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: makeAssertionOptions });
    } catch (err) {
        alert(err.message ? err.message : err);
    }
    return credential;
}

async function createCredential(makeCredentialOptions) {
    debugger;
    let credential;

    
    makeCredentialOptionsJson = JSON.parse(makeCredentialOptions);
    makeCredentialOptionsJson.challenge = new Uint8Array(makeCredentialOptionsJson.challenge);
    makeCredentialOptionsJson.user.id = new Uint8Array(makeCredentialOptionsJson.user.id);
   

    try {
        credential = await navigator.credentials.create({
            publicKey: makeCredentialOptionsJson
        });
    } catch (e) {
        var msg = "Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator."
        console.error(msg, e);
        alert(msg);
    }

    // convert credential to json serializeable
    const serializeable = {
        authenticatorAttachment: credential.authenticatorAttachment,
        id: window.btoa(credential.id),
        rawId: bufferToBase64(credential.rawId),
        response: {
            attestationObject: bufferToBase64(credential.response.attestationObject),
            clientDataJSON: bufferToBase64(credential.response.clientDataJSON)
        },
        type: credential.type
    };

    const json = JSON.stringify(serializeable);
    
    return json;
}

