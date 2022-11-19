using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;

namespace CSharpChainModel
{
    class TransactionsConvertor : ConverterBase
    {
        public override object StringToField(string from)
        {
            List<Transaction> list = new List<Transaction>();
            int identifier = 1;
            string recieved = "";
            string sent = "";
            decimal amount = 0;
            string desc = "";


            string result = from.Replace("Transactions:", "");
            while (result.Contains("%"))
            {
                switch (identifier)
                {
                    case 1:
                        recieved = result.Substring(0, result.IndexOf("-"));
                        identifier++;
                        result = result.Substring(result.IndexOf("-") + 1);
                        break;
                    case 2:
                        sent = result.Substring(0, result.IndexOf("+"));
                        identifier++;
                        result = result.Substring(result.IndexOf("+") + 1);
                        break;
                    case 3:
                        desc = result.Substring(0, result.IndexOf("*"));
                        identifier++;
                        result = result.Substring(result.IndexOf("*") + 1);
                        break;
                    case 4:
                        amount = Decimal.Parse(result.Substring(0, result.IndexOf("%")));
                        identifier = 1;
                        result = result.Substring(result.IndexOf("%") + 1);
                        list.Add(new Transaction(sent, recieved, amount, desc));

                        break;
                }

                //Console.WriteLine(result);
            }

            return list;
        }

        public override string FieldToString(object from)
        {
            List<Transaction> list = (List<Transaction>)from;
            string transactions = "Transactions:";
            foreach(Transaction item in list)
            {
                transactions += item.ReceiverAddress + "-";
                transactions += item.SenderAddress + "+";
                transactions += item.Description + "*";
                transactions += item.Amount + "%";
            }




            return transactions;
        }
    }
}
