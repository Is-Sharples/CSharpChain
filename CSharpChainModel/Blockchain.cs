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

		public Blockchain()
		{
			this.Chain = new List<Block>();
			this.Chain.Add(InternalGetLastBlock());

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
			Stream stream = File.Open("C:/temp/test.dat", FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			string tempFile = "C:/temp/temp.txt";
			StreamWriter temp = new StreamWriter(tempFile);
			var engine = new FileHelperEngine<Block>();
			int blockSize = 12288;

			stream.Seek(-blockSize, SeekOrigin.End);
			string tempString = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
			temp.WriteLine(tempString);
			temp.Close();
			stream.Close();

			Block[] tempGenesis = engine.ReadString(tempString);

			binReader.Close();

			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
			}


			if (tempGenesis.Length > 0)
			{
				return tempGenesis[0];
			}
			return CreateGenesisBlock();

		}



	}
}
