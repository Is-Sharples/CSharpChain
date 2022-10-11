
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;

namespace CSharpChainModel
{
	[FixedLengthRecord]
	public class Block
	{
		[FieldFixedLength(30)]
		public string PreviousHash;
		[FieldFixedLength(10)]
		[FieldConverter(ConverterKind.Date, "ddMMyyyy")]
		public DateTime TimeStamp;
		[FieldFixedLength(250)]
		[FieldConverter(typeof (TransactionsConvertor))]
		public List<Transaction> Transactions;
		[FieldFixedLength(30)]
		public string Hash;
		[FieldFixedLength(10)]
		public long Nonce;

		public Block(DateTime timeStamp, List<Transaction> transactions, string previousHash)
		{
			this.TimeStamp = timeStamp;
			this.PreviousHash = previousHash;
			this.Transactions = transactions;
			this.Hash = "";
			this.Nonce = 0;
		}

		public Block()
        {

        }

		public override String ToString()
		{
			return $" TimeStamp: {this.TimeStamp} \n " +
				$"PreviousHash: {this.PreviousHash} \n " +
				$"Hash: {this.Hash} \n " +
				$"Nonce: {this.Nonce} ";
		}

	}
}
