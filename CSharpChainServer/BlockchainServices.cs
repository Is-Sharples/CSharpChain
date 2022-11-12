using CSharpChainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using FileHelpers;

namespace CSharpChainServer
{
    public class BlockchainServices
	{
		public Blockchain Blockchain
		{
			get
			{
				return blockchain;
			}

		}

		public object Program { get; private set; }

		private Blockchain blockchain;

		public BlockchainServices()
		{
			// generate initial blockchain with genesis block
			blockchain = new Blockchain();

			// calculate hash of genesis block
			Block genesisBlock = blockchain.Chain[0];
			BlockServices blockServices = new BlockServices(genesisBlock);
			if (blockchain.Genesis.PreviousHash.Trim() == "0")
            {
				string genesisBlockHash = blockServices.BlockHash();
				blockchain.Chain[0].Hash = genesisBlockHash;
			}
			
		}

		public void UpdateWithLongestBlockchain ()
		{
			string longestBlockchainNode = "";
			int maxBlockchainLength = 0;

		}

		public void RefreshBlockchain()
        {
			this.blockchain = new Blockchain();
        }

		public Block LatestBlock()
		{
			return blockchain.Chain.Last();
		}

		public Block Block(int index)
		{
            try{
				return blockchain.Chain[index];
            }
            catch
            {
				Console.WriteLine("Out of Bounds Error, returning Genesis block");
				return blockchain.Chain[0];
            }
			
		}

		public int BlockchainLength()
		{
			return blockchain.Chain.Count();
		}

		public void AddTransaction(Transaction transaction)
		{
			blockchain.PendingTransactions.Add(transaction);
		}

		public List<Transaction> PendingTransactions()
		{
			return blockchain.PendingTransactions;
		}

		public Block MineBlock(string miningRewardAddress)
		{
			// add mining reward transaction to block
			Transaction trans = new Transaction("SYSTEM", miningRewardAddress, blockchain.MiningReward, "Mining reward");
			blockchain.PendingTransactions.Add(trans);


			Block block = new Block(DateTime.Now, blockchain.PendingTransactions, LatestBlock().Hash);
			var blockServices = new BlockServices(block);
			blockServices.MineBlock(blockchain.Difficulty);
			blockchain.Chain.Add(block);
			Console.WriteLine($"  Mining Reward has been assigned to: http://localhost:{miningRewardAddress}");
			//clear pending transactions (all pending transactions are in a block
			blockchain.PendingTransactions = new List<Transaction>();
			return block;
		}

		public bool isBlockchainValid()
		{
			var engine = new FileHelperEngine<Block>();
			int blockSize = 12288;
			Stream readStream = File.Open("C:/temp/Master.dat", FileMode.Open);
			BinaryReader binReader = new BinaryReader(readStream, Encoding.ASCII);
			Block previous = new Block();
			Console.WriteLine("Validating...");
			
			for (long i = 0; i < binReader.BaseStream.Length/blockSize; i++)
			{	
				readStream.Seek(i * blockSize, SeekOrigin.Begin);
				string blockData = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
				Block block = engine.ReadString(blockData)[0];
				if(block.PreviousHash.Trim() == "0")
                {
					//Console.WriteLine("Genesis Block");
                }else
                {
                    if (block.PreviousHash != previous.Hash)
                    {
						Console.WriteLine(i);
						Console.WriteLine("Current Blocks Previous Hash:"+ block.PreviousHash);
						Console.WriteLine("Actual previous Hash"+previous.Hash);
						return false;
                    }
                }
				previous = block;
				
			}
			readStream.Close();
			binReader.Close();

			return true;
		}

		public decimal Balance(string address)
		{
			decimal balance = 0;

			foreach (Block block in blockchain.Chain)
			{
				foreach (Transaction transaction in block.Transactions)
				{
					if (transaction.SenderAddress == address)
					{
						balance = balance - transaction.Amount;
					}

					if (transaction.ReceiverAddress == address)
					{
						balance = balance + transaction.Amount;
					}
				}
			}
			return balance;
		}



	}
}
