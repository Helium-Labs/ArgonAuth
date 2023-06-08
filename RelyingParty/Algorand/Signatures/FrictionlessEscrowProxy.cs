using System;
using Algorand.Algod;
using Algorand.Algod.Model;
using Algorand.Algod.Model.Transactions;
using AlgoStudio;
using Algorand;
using AlgoStudio.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxies
{

	
	public class FrictionlessEscrowProxy : ProxyBase
	{
		
		public FrictionlessEscrowProxy(DefaultApi defaultApi, ulong appId) : base(defaultApi, appId) 
		{
		}

		/**
         * Lifecycle Methods
         */
		public async Task<int> Initialise (Account sender, ulong? fee, Address owner,Address funder,string note, List<BoxRef> boxes)
		{
			var abiHandle = Encoding.UTF8.GetBytes("Initialise");
			var result = await base.CallApp(null, fee, AlgoStudio.Core.OnCompleteType.NoOp, 1000, note, sender,  new List<object> {abiHandle}, null, null,new List<Address> {owner,funder},boxes);
			return BitConverter.ToInt32(ReverseIfLittleEndian(result.First().ToArray()), 0);

		}

		/**
         * Asset Management Methods
         */
		public async Task<int> Payment (Account sender, ulong? fee, Address recipient,ulong microAlgoAmount,string note, List<BoxRef> boxes)
		{
			var abiHandle = Encoding.UTF8.GetBytes("Payment");
			var result = await base.CallApp(null, fee, AlgoStudio.Core.OnCompleteType.NoOp, 1000, note, sender,  new List<object> {abiHandle,microAlgoAmount}, null, null,new List<Address> {recipient},boxes);
			return BitConverter.ToInt32(ReverseIfLittleEndian(result.First().ToArray()), 0);

		}

		public async Task<int> AssetTransfer (Account sender, ulong? fee, Address recipient,ulong asset,ulong amount,string note, List<BoxRef> boxes)
		{
			var abiHandle = Encoding.UTF8.GetBytes("AssetTransfer");
			var result = await base.CallApp(null, fee, AlgoStudio.Core.OnCompleteType.NoOp, 1000, note, sender,  new List<object> {abiHandle,amount}, null, new List<ulong> {asset},new List<Address> {recipient},boxes);
			return BitConverter.ToInt32(ReverseIfLittleEndian(result.First().ToArray()), 0);

		}

		/**
         * UTILITY METHODS
         */
		public async Task<int> OptInAsset (Account sender, ulong? fee, ulong asset,string note, List<BoxRef> boxes)
		{
			var abiHandle = Encoding.UTF8.GetBytes("OptInAsset");
			var result = await base.CallApp(null, fee, AlgoStudio.Core.OnCompleteType.NoOp, 1000, note, sender,  new List<object> {abiHandle}, null, new List<ulong> {asset},null,boxes);
			return BitConverter.ToInt32(ReverseIfLittleEndian(result.First().ToArray()), 0);

		}

		public async Task<int> CloseOutAsset (Account sender, ulong? fee, ulong assetReference,string note, List<BoxRef> boxes)
		{
			var abiHandle = Encoding.UTF8.GetBytes("CloseOutAsset");
			var result = await base.CallApp(null, fee, AlgoStudio.Core.OnCompleteType.NoOp, 1000, note, sender,  new List<object> {abiHandle}, null, new List<ulong> {assetReference},null,boxes);
			return BitConverter.ToInt32(ReverseIfLittleEndian(result.First().ToArray()), 0);

		}

		public async Task<byte[]> OwnerAddress()
		{
			var key="OwnerAddress";
			var result= await base.GetGlobalByteSlice(key);
			return result;

		}

		public async Task<byte[]> FunderAddress()
		{
			var key="FunderAddress";
			var result= await base.GetGlobalByteSlice(key);
			return result;

		}

	}

}
