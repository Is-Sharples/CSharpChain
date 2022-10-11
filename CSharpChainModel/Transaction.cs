using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileHelpers;
namespace CSharpChainModel
{	
	[FixedLengthRecord]
	public class Transaction
    {
		[FieldFixedLength(10)]
		public string SenderAddress;
		[FieldFixedLength(10)]
		public string ReceiverAddress;
		[FieldFixedLength(10)]
		public decimal Amount;
		[FieldFixedLength(10)]
		public string Description;
		

		public Transaction(string senderAddress, string receiverAddress, decimal amount, string description)
		{
			this.SenderAddress = senderAddress;
			this.ReceiverAddress = receiverAddress;
			this.Amount = amount;
			this.Description = description;
		}

        public override String ToString()
        {
			return $" Sender Address: {this.SenderAddress} \n " +
				$"Receiver Address: {this.ReceiverAddress} \n " +
				$"For the Amount: {this.Amount} \n " +
				$"Description: {this.Description} ";
		}

    }
}
