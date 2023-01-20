using FileHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpChainModel
{
	public class Blockchain
	{
		public List<Block> Chain;
		public List<string> Nodes;
		public int Difficulty;
		public List<Transaction> PendingTransactions;
		public int MiningReward;
		public List<string> Users;
		public Block Genesis;
		public Blockchain()
		{
			this.Chain = new List<Block>();
			this.Chain.Add(InternalGetLastBlock());
			this.Genesis = this.Chain[0];
			this.Nodes = new List<string>();
			this.Users = new List<string>();
			this.Difficulty = 1;
			this.PendingTransactions = new List<Transaction>();
			this.MiningReward = 100;
		}

		private Block CreateGenesisBlock()
		{
			Block genesis = new Block(new DateTime(2000, 01, 01), new List<Transaction>(), "0");
			return genesis;
		}

		Block InternalGetLastBlock()
		{
            if (!File.Exists("C:/temp/Master.dat"))
            {
				return CreateGenesisBlock();
			}
			Stream stream = File.Open("C:/temp/Master.dat", FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			var engine = new FileHelperEngine<Block>();
			int blockSize = 37888;

			stream.Seek(-blockSize, SeekOrigin.End);
			string tempString = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
			
			stream.Close();

			Block[] tempGenesis = engine.ReadString(tempString);

			binReader.Close();



			if (tempGenesis.Length > 0)
			{
				return tempGenesis[0];
			}
			return CreateGenesisBlock();

		}



	}
}
