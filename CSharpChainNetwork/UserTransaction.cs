using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpChainModel
{
    public class UserTransaction : Transaction
    {
        public int blockIndex;
        
        public UserTransaction(int blockIndex, string senderAddress, string receiverAddress, decimal amount, string description)
        {
            this.blockIndex = blockIndex;
            this.ReceiverAddress = receiverAddress;
            this.SenderAddress = senderAddress;
            this.Amount = amount;
            this.Description = description;
        }

        public override string ToString()
        {
            return $" Sender Address: {this.SenderAddress} \n " +
                $"Receiver Address: {this.ReceiverAddress} \n " +
                $"For the Amount: {this.Amount} \n " +
                $"Description: {this.Description}\n " + 
                $"Block Index {this.blockIndex}";
        }
    }
}
