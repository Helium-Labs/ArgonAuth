using AlgoStudio.Core;
using AlgoStudio.Core.Attributes;



namespace RelyingParty.Algorand.Contracts
{
    public class FrictionlessEscrow : SmartContract
    {
        // User that owns the contract, which they use on its behalf
        [Storage(StorageType.Global)]
        byte[] OwnerAddress;

        // Application Contract Address. Covers MBR, and fees via fee pooling.
        [Storage(StorageType.Global)]
        byte[] FunderAddress;


        protected override int ApprovalProgram(in AppCallTransactionReference transaction)
        {
            // Defer to router
            InvokeSmartContractMethod();

            //check if this is a creation
            if (transaction.ApplicationID == 0)
            {
                // initialise the owner, specified as the first argument
                return 1;
            }

            // Check if this is a deletion
            if (transaction.OnCompletion == 5)
            {
                if (Balance > 0)
                {
                    // There's a
                    return 0;
                }

                if (MinBalance > 100000)
                {
                    //it's opted in to something
                    return 0;
                }

                // Check the deletion is being executed by the creator. Otherwise fail.
                AccountReference deleter = transaction.Sender;
                byte[] creatorAddress = CreatorAddress;
                AccountReference creator = creatorAddress.ToAccountReference();
                if (deleter == creator)
                {
                    return 1;
                }
            }

            // Fail on any other call
            return 0;

        }

        protected override int ClearStateProgram(in AppCallTransactionReference transaction)
        {
            return 1;
        }

        /**
         * Lifecycle Methods
         */
        [SmartContractMethod(OnCompleteType.NoOp, "Initialise")]
        public int Initialise(AccountReference owner, AccountReference funder, AppCallTransactionReference current)
        {
            int defaultChecks()
            {
                // assert sender is the creator of the smart contract
                byte[] creatorAddress = CreatorAddress;
                AccountReference creatorAccount = creatorAddress.ToAccountReference();
                if (current.Sender != creatorAccount)
                {
                    return 0;
                }
                //do not permit any kind of rekey
                if (current.RekeyTo != ZeroAddress) return 0;
                //do not allow anything else than a single asset transfer
                if (GroupSize != 1) return 0;
                return 1;
            }

            int passesDefaultChecks = defaultChecks();
            if (passesDefaultChecks == 0) return 0;

            FunderAddress = funder.Address();
            OwnerAddress = owner.Address();
            return 1;
        }

        /**
         * Asset Management Methods
         */
        [SmartContractMethod(OnCompleteType.NoOp, "Payment")]
        public int Payment(ulong microAlgoAmount, AccountReference recipient, AppCallTransactionReference current)
        {
            int defaultChecks()
            {
                // assert owner is the sender
                byte[] owner = OwnerAddress;
                AccountReference ownerAccount = owner.ToAccountReference();
                if (current.Sender != ownerAccount)
                {
                    return 0;
                }
                //do not permit any kind of rekey
                if (current.RekeyTo != ZeroAddress) return 0;
                //do not allow anything else than a single asset transfer
                if (GroupSize != 1) return 0;
                return 1;
            }

            int passesDefaultChecks = defaultChecks();
            if (passesDefaultChecks == 0) return 0;

            // inner transaction
            [InnerTransactionCall]
            void makePayment()
            {
                new Payment(recipient, microAlgoAmount);
            }
            makePayment();
            return 1;
        }

        [SmartContractMethod(OnCompleteType.NoOp, "AssetTransfer")]
        public int AssetTransfer(ulong amount, AssetReference asset, AccountReference recipient, AppCallTransactionReference current)
        {
            int defaultChecks()
            {
                // assert owner is the sender
                byte[] owner = OwnerAddress;
                AccountReference ownerAccount = owner.ToAccountReference();
                if (current.Sender != ownerAccount)
                {
                    return 0;
                }
                //do not permit any kind of rekey
                if (current.RekeyTo != ZeroAddress) return 0;
                //do not allow anything else than a single asset transfer
                if (GroupSize != 1) return 0;
                return 1;
            }

            int passesDefaultChecks = defaultChecks();
            if (passesDefaultChecks == 0) return 0;


            // inner transaction
            [InnerTransactionCall]
            void makeAssetTransfer()
            {
                new AssetTransfer(asset.Id, amount, recipient);
            }
            makeAssetTransfer();
            return 1;
        }

        /**
         * UTILITY METHODS
         */
        [SmartContractMethod(OnCompleteType.NoOp, "OptInAsset")]
        public int OptInAsset(AssetReference asset, AppCallTransactionReference current)
        {
            int defaultChecks()
            {
                // only allow the funder to opt in an asset
                // assert the sender is the funder
                byte[] funderAddress = FunderAddress;
                AccountReference funderAccount = funderAddress.ToAccountReference();
                if (current.Sender != funderAccount)
                {
                    return 0;
                }
                //do not permit any kind of rekey
                if (current.RekeyTo != ZeroAddress) return 0;
                //do not allow anything else than a single asset transfer
                if (GroupSize != 1) return 0;
                return 1;
            }

            int passesDefaultChecks = defaultChecks();
            if (passesDefaultChecks == 0) return 0;

            // inner transaction
            [InnerTransactionCall]
            void OptInToNewAsset()
            {
                byte[] appAddress = CurrentApplicationAddress;
                AccountReference contract = appAddress.ToAccountReference();
                //opt in to new
                new AssetAccept(asset.Id, contract, contract, 0);
            }
            OptInToNewAsset();
            return 1;
        }

        [SmartContractMethod(OnCompleteType.NoOp, "CloseOutAsset")]
        public int CloseOutAsset(AssetReference assetReference, AppCallTransactionReference current)
        {
            byte[] funderAddress = FunderAddress;
            AccountReference funderAccount = funderAddress.ToAccountReference();

            // Close out and return 0.1A to the funder
            [InnerTransactionCall]
            void CloseOutAndReturnMBRAlgoToFunder(ulong asset)
            {
                ulong minBalanceBefore = MinBalance;
                new AssetTransfer(asset, 0, funderAccount, null, funderAccount, 0);
                ulong mbrPerAsset = minBalanceBefore - MinBalance;
                new Payment(funderAccount, mbrPerAsset);
            }

            int defaultChecks()
            {
                // only allow the funder to opt out an asset
                // assert the sender is the funder
                byte[] funderAddress = FunderAddress;
                AccountReference funderAccount = funderAddress.ToAccountReference();
                if (current.Sender != funderAccount)
                {
                    return 0;
                }
                //do not permit any kind of rekey
                if (current.RekeyTo != ZeroAddress) return 0;
                //do not allow anything else than a single asset transfer
                if (GroupSize != 1) return 0;
                return 1;
            }

            int passesDefaultChecks = defaultChecks();
            if (passesDefaultChecks == 0) return 0;

            // check we have opted in the asset, and its balance is 0.
            byte[] appAddress = CurrentApplicationAddress;
            AccountReference contract = appAddress.ToAccountReference();
            ulong assetBalance = contract.AssetBalance(assetReference);
            if (assetBalance != 0) return 0;

            // inner grpup transaction to close out and return MBR
            ulong assetIndex = assetReference.Id;
            CloseOutAndReturnMBRAlgoToFunder(assetIndex);

            // all checks passed
            return 1;
        }

        /* 
        [SmartContractMethod(OnCompleteType.NoOp, "Compose")]
        public int Compose(ulong amount, AssetReference asset, AccountReference recipient, AppCallTransactionReference current)
        {
            [InnerTransactionCall]
            void makePayment()
            {
                var paymentGroup = (new Payment(split1, amountToPayToRecipient1), new Payment(split2, amountToPayToRecipient2));
            }

            return 1;
        }
        */

        // TODO - Add support for inner app calls
        /*
        [SmartContractMethod(OnCompleteType.NoOp, "AppCall")]
        public int AppCall(SmartContractReference smartContract, AccountReference recipient, AppCallTransactionReference current)
        {
            // inner transaction
            [InnerTransactionCall]
            void makeAppCall()
            {
                var app = new AppCall(smartContract., appArgs, recipient);
                app
            }
            makeAppCall();
            return 0;
        }
        */
    }
}