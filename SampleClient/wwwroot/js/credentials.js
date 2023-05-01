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
    let newCredential;

    makeCredentialOptionsJson = JSON.parse(makeCredentialOptions);
    makeCredentialOptionsJson.challenge = new Uint8Array(makeCredentialOptionsJson.challenge);
    makeCredentialOptionsJson.user.id = new Uint8Array(makeCredentialOptionsJson.user.id);
   

    try {
        newCredential = await navigator.credentials.create({
            publicKey: makeCredentialOptionsJson
        });
    } catch (e) {
        var msg = "Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator."
        console.error(msg, e);
        alert(msg);
    }
    return newCredential;
}

