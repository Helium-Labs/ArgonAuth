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

	
	public class GameWalletProxy : SignatureBase
	{
		LogicsigSignature smartSig;
		
		public GameWalletProxy(LogicsigSignature logicSig) : base(logicSig) 
		{
		}

		public void ApproveTransferClient (byte[] signatureR,byte[] signatureS,byte[] startround,byte[] endround)
		{
			var abiHandle = Encoding.UTF8.GetBytes("Ax1");
			base.UpdateSmartSignature( new List<object> {abiHandle,signatureR,signatureS,startround,endround} );

		}

		public void ApproveTransferDelegated (byte[] signatureR,byte[] signatureS,byte[] proofKey,ulong startround,ulong endround)
		{
			var abiHandle = Encoding.UTF8.GetBytes("Ax1Delegated");
			base.UpdateSmartSignature( new List<object> {abiHandle,signatureR,signatureS,proofKey,startround,endround} );

		}

		public void ApprovePayment ()
		{
			var abiHandle = Encoding.UTF8.GetBytes("Payment");
			base.UpdateSmartSignature( new List<object> {abiHandle} );

		}

	}

}
