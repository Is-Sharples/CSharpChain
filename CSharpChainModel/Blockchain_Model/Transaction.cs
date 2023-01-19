using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using FileHelpers;
using System.Text;

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

		public string Hash()
        {
			
			string toHash = $"{ReceiverAddress}{SenderAddress}{Description}{Amount}";

			Byte[] hashBytes;
			using (var algorithm = SHA1.Create())
			{
				hashBytes = algorithm.ComputeHash(Encoding.ASCII.GetBytes(toHash));
			}

			string toReturn = BitConverter.ToString(hashBytes).Replace("-", "");
			return toReturn.Substring(0,15);

		}

		public List<Transaction> ExperimentalSearchForTransactions(string text, string key)
		{
			string recieved = "";
			string sent = "";
			decimal amount = 0;

			List<Transaction> list = new List<Transaction>();
			List<string >arr = text.Split('%').ToList<string>();
			arr.RemoveAt(arr.Count-1);
            foreach (string trans in arr)
            {
				int plus = trans.IndexOf('+');
				if (trans.Substring(0,plus).Contains(key))
                {
					int minus = trans.IndexOf('-');
					int times = trans.IndexOf('*');
					recieved = trans.Substring(0,minus);
					sent = trans.Substring(minus+1,plus-(minus+1));
					amount = decimal.Parse(trans.Substring(times+1));
					list.Add(new Transaction(sent,recieved,amount,""));
                }
            }
			return list;
		}

		public UserTransaction ToUserTransaction(int blockNum)
        {	

			return new UserTransaction(blockNum, this.SenderAddress, this.ReceiverAddress, this.Amount, this.Description);
        }

		public HashSet<string> GetUsersForPointerIndex(string text)
        {
			HashSet<string> users = new HashSet<string>();
			List<string> arr = text.Split('%').ToList<string>();
			arr.RemoveAt(arr.Count - 1);
            foreach (string trans in arr)
            {
				int minus = trans.IndexOf('-');
				int plus = trans.IndexOf('+');
				users.Add(trans.Substring(0, minus));
				users.Add(trans.Substring(minus + 1, plus - minus-1));
            }
			users.Remove("System2");
			users.Remove("SYSTEM");
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

/*
			 * Using arbitrary delimiters the mathematical symbols are used 
			 * to distinguish the different strings as data types:
			 * up till - is the receiver of the transaction
			 * from - to + is the sender 
			 * from + to * is the description 
			 * from * to % is the amount
			 * the % symbol is the end of the transaction
			*/