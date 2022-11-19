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

		public Transaction()
        {

        }

        public override String ToString()
        {
			return $" Sender Address: {this.SenderAddress} \n " +
				$"Receiver Address: {this.ReceiverAddress} \n " +
				$"For the Amount: {this.Amount} \n " +
				$"Description: {this.Description} ";
		}

		public List<Transaction> SearchForTransactions(string text, string key)
        {
			string recieved = "";
			string sent = "";
			decimal amount = 0;
			string desc = "";
			
			List<Transaction> list = new List<Transaction>();
			/*
			 * Using arbitrary delimiters the mathematical symbols are used 
			 * to distinguish the different strings as data types:
			 * up till - is the receiver of the transaction
			 * from - to + is the sender 
			 * from + to * is the description 
			 * from * to % is the amount
			 * the % symbol is the end of the transaction
			*/
			
			while (text.Contains(key))
			{
				
				if (text.Substring(0,text.IndexOf("+")).Contains(key))
                {	
					recieved = text.Substring(0, text.IndexOf("-"));
					sent = text.Substring(text.IndexOf("-") + 1, text.IndexOf("+")-text.IndexOf("-")-1);
					desc = text.Substring(text.IndexOf("+") + 1, text.IndexOf("*")-text.IndexOf("+")-1);
					amount = Decimal.Parse(text.Substring(text.IndexOf("*") + 1, text.IndexOf("%")-text.IndexOf("*")-1));
					list.Add(new Transaction(sent, recieved, amount, desc));
				}
				text = text.Substring(text.IndexOf("%") + 1);
			}
			return list;
        }

		public UserTransaction ToUserTransaction(int blockNum)
        {	

			return new UserTransaction(blockNum, this.SenderAddress, this.ReceiverAddress, this.Amount, this.Description);
        }

		public List<string> GetUsersFromText(string text, List<string> users)
        {
			string recieved = "";
			string sent = "";
			

            while (text.Contains("+")){

                recieved = text.Substring(0, text.IndexOf("-"));
                if (!users.Contains(recieved))
                {
					users.Add(recieved);
                }
                sent = text.Substring(text.IndexOf("-") + 1, text.IndexOf("+") - text.IndexOf("-") - 1);
				
				if (!users.Contains(sent))
                {
					users.Add(sent);
				}		
				
				text = text.Substring(text.IndexOf("%") + 1);
            }
			return users;
		}

		public List<string> PartialGetUserCountFromText(string text)
		{
			string recieved = "";
			string sent = "";
			List<string> users = new List<string>();

			while (text.Contains("+"))
			{

				recieved = text.Substring(0, text.IndexOf("-"));
				sent = text.Substring(text.IndexOf("-") + 1, text.IndexOf("+") - text.IndexOf("-") - 1);
				users.Add(recieved);
				users.Add(sent);

				text = text.Substring(text.IndexOf("%") + 1);
			}
			return users;
		}
	}
    }

