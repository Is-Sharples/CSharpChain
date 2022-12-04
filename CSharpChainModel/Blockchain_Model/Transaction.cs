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

		public List<UserTransaction> SearchForTransactions(string text, string key, int blockNum)
        {
			string recieved = "";
			string sent = "";
			decimal amount = 0;
			string desc = "";
			
			List<UserTransaction> list = new List<UserTransaction>();
			/*
			 * Using arbitrary delimiters the mathematical symbols are used 
			 * to distinguish the different strings as data types:
			 * up till - is the receiver of the transaction
			 * from - to + is the sender 
			 * from + to * is the description 
			 * from * to % is the amount
			 * the % symbol is the end of the transaction
			*/
			
			while (text.Contains("%"))
			{
				
				if (text.Substring(0,text.IndexOf("+")).Contains(key))
                {	
					recieved = text.Substring(0, text.IndexOf("-"));
					sent = text.Substring(text.IndexOf("-") + 1, text.IndexOf("+")-text.IndexOf("-")-1);
					desc = text.Substring(text.IndexOf("+") + 1, text.IndexOf("*")-text.IndexOf("+")-1);
					amount = Decimal.Parse(text.Substring(text.IndexOf("*") + 1, text.IndexOf("%")-text.IndexOf("*")-1));
					list.Add(new UserTransaction(blockNum, sent, recieved, amount, desc));
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

		public Dictionary<string,char> GetUsersForIndex(Block block, User[] masterUsers)
        {
			Dictionary<string, char> result = new Dictionary<string, char>();
			HashSet<string> tempList = new HashSet<string>();
            foreach (Transaction trans in block.Transactions)
            {
				tempList.Add(trans.ReceiverAddress);
				tempList.Add(trans.SenderAddress);
            }
			foreach(User user in masterUsers)
            {
                if (tempList.Contains(user.name))
                {
					result.Add(user.name,'1');
                }else
                {
					result.Add(user.name,'0');
                }
            }
			
			return result;
		}

		public Dictionary<string,char> GetUsersForIndex(string text, User[] masterUsers)
		{
			string recieved = "";
			string sent = "";
			Dictionary<string, char> result = new Dictionary<string, char>();
			HashSet<string> tempList = new HashSet<string>();
			while (text.Contains("+"))
			{
				recieved = text.Substring(0, text.IndexOf("-"));
				sent = text.Substring(text.IndexOf("-") + 1, text.IndexOf("+") - text.IndexOf("-") - 1);
				tempList.Add(recieved);
				tempList.Add(sent);

				text = text.Substring(text.IndexOf("%") + 1);
			}

            foreach (User item in masterUsers)
            {
                if (tempList.Contains(item.name))
                {
					result.Add(item.name,'1');
                }else
                {
					result.Add(item.name,'0');
                }
            }

			string[] users = new string[tempList.Count];
			tempList.CopyTo(users);

			return result;
		}

	}
    }

