# Client Implementation Strategy

A business user (`bUser`) uses the service through which they can create clients (`client`) to interface with the service on their website, through which their customers (`cUser`) can access the service. A `client` is the following data entity:
- `clientId`: a base64 encoded ED25519 public key
- A list of whitelisted origins (`origin`): a string that represents a URL that the `client` is allowed to use to access the service from

A `cUser` is specific to a particular `client`, necessitating a `clientId` identifier associated with each `cUser`. A `bUser` can be considered a `cUser` with an undefined `clientId`, where hardcoded origin checks are used to classify the registering user. A compromised `bUser` can't access assets of `cUsers`, 
