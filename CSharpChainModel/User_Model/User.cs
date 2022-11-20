using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpChainModel
{
    public class User
    {
        public List<UserTransaction> transactions;
        public string name;
        public int transactionCount;
        public string locationCSV;
        public List<string> locationString;

        public User(string name)
        {
            this.name = name;
            this.transactionCount = 0;
            this.locationCSV = "";
            locationString = new List<string>();
        }
        public User()
        {

        }

    }
}
