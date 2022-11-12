using CSharpChainModel;
using CSharpChainServer;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using System.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;
namespace CSharpChainNetwork
{
	static class Program
	{

		static string baseAddress;
		public static BlockchainServices blockchainServices;
		public static NodeServices nodeServices;
		static int blockSize = 12288;
		static bool useNetwork = true;

		public static void ShowCommandLine()
		{
			Console.WriteLine("");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("CSharpChain> ");
			Console.ResetColor();
		}

		static void Main(string[] args)
		{

			baseAddress = args[0];
			if (!baseAddress.EndsWith("/")) baseAddress += "/";

			// Start OWIN host 
			using (WebApp.Start<Startup>(url: baseAddress))
			{
				Console.WriteLine("");
				Console.WriteLine("CSharpChain Blockchain // dejan@mauer.si // www.mauer.si");
				Console.WriteLine("----------------------------------------");
				Console.WriteLine("This CSharpChain node is running on " + baseAddress);
				Console.WriteLine("Type 'help' if you are not sure what to do ;)");

				blockchainServices = new BlockchainServices();
				nodeServices = new NodeServices(blockchainServices.Blockchain);

				string commandLine;

				ShowCommandLine();
				
				do
				{
					ShowCommandLine();
		
					commandLine = Console.ReadLine().ToLower();
					commandLine += " ";
					var command = commandLine.Split(' ');

					switch (command[0])
					{
						case "quit":
						case "q":
							commandLine = "q";
							break;

						case "help":
						case "?":
							CommandHelp();
							break;

						case "node-add":
						case "na":
							// if param1 is numeric then translate to localhost port
							if (command[1].All(char.IsDigit)) command[1] = "http://localhost:" + command[1];
							CommandNodeAdd(command[1]);
							break;

						case "node-remove":
						case "nr":
							// if param1 is numeric then translate to localhost port
							if (command[1].All(char.IsDigit)) command[1] = "http://localhost:" + command[1];
							CommandNodeRemove(command[1]);
							break;

						case "nodes-list":
						case "nl":
							CommandListNodes(nodeServices.Nodes);
							break;

						case "transactions-add":
						case "ta":
							CommandTransactionsAdd(command[1], command[2], command[3], command[4]);
							break;

						case "transactions-pending":
						case "tp":
							CommandListPendingTransactions(blockchainServices.Blockchain.PendingTransactions);
							break;

						case "blockchain-mine":
						case "bm":
							CommandBlockchainMine(command[1]);
							break;

						case "bv":
						case "blockchain-valid":
							CommandBlockchainValidity();
							break;

						case "blockchain-length":
						case "bl":
							//CommandBlockchainLength();
							SimpleBlockchainLength();
							break;

						case "block":
						case "b":
							CommandBlock(int.Parse(command[1]));
							break;

						case "balance-get":
						case "bal":
							CommandBalance(command[1]);
							break;

						case "blockchain-update":
						case "update":
						case "bu":
							CommandBlockchainUpdate();
							break;
						case "gen": 
							if(command[1].Length > 0)
                            {
								GenerateBlocks(int.Parse(command[1]));
								
							}else
                            {
								ShowIncorrectCommand();
							}
							break;
							/*
						case "write":
						case "w":
							WriteFromFixedLengthToBinary();
							break;
							*/
						case "read":
						case "r":
							ReadFromConvertedBinary();
							
							break;
						case "search":
						case "s":
							SearchTransactionsByNode(command[1]);
							break;
						case "t":
							InternalGetLastBlock();
							
							break;
						default:
							ShowIncorrectCommand();
							break;
					}
				} while (commandLine != "q");
			}
		}
		#region InternalFunctions 

		static void ShowIncorrectCommand()
        {
			Console.WriteLine("Ups! I don't understand...");
			Console.WriteLine("");
		}

		

		static double InternalIndicateWeighting(int wallet)
        {
			double weight = 0;
            if (((wallet >= 0) && (wallet < 250))|| (wallet >= 1750 && wallet < 2000))
            {
				weight = 0.1;
            }else if ((wallet >= 250 && wallet < 500)||(wallet >= 1500 && wallet < 1750)){
				weight = 0.3;
			}else if ((wallet >= 500 && wallet < 750)|| (wallet >= 1250)&& wallet < 1500)
            {
				weight = 0.5;
            }else if ((wallet >= 750 && wallet < 1000)||(wallet > 1000 && wallet < 1250))
            {
				weight = 0.8;
            }else if (wallet == 1000)
            {
				weight = 0.9;
            }

			return weight;
        }

		static void SimpleBlockchainLength()
        {
			Stream stream = File.Open("C:/temp/Master.dat", FileMode.Open);
			BinaryReader binary = new BinaryReader(stream, Encoding.ASCII);

			Console.WriteLine(binary.BaseStream.Length/blockSize);

			stream.Close();
			binary.Close();
        }

        #region Internal Searching functions
        //Function is unused but useful for finding certain blocks from binary data 
        static Block [] InternalSeekBlocksFromFile(int [] keys)
        {
			int[] desiredBlocks = keys;
			string tempFile = "C:/temp/temp.txt";
			var engine = new FileHelperEngine<Block>();
			byte[] blockData;
			Stream stream = File.Open("C:/temp/Master.dat", FileMode.Open);
			StreamWriter temp = new StreamWriter(tempFile);
			BinaryReader binReader = new BinaryReader(stream,Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;
			foreach (int block in desiredBlocks)
			{
				if (block * blockSize < fileLength)
				{
						stream.Seek(block * blockSize, SeekOrigin.Begin);
						blockData = binReader.ReadBytes(blockSize);
						temp.WriteLine(Encoding.ASCII.GetString(blockData));
				}
			}

			stream.Close();
			temp.Close();
            
			
			Block [] result = engine.ReadFile("C:/temp/temp.txt");

			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
			}
			return result;
			
		}
		
		static List<UserTransaction> InternalSeekTransactionsFromFile(string key)
        {
			List<UserTransaction> userTransactions = new List<UserTransaction>();
			Transaction utilities = new Transaction();
			Stream stream = File.Open("C:/temp/Master.dat", FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;

			Console.WriteLine($"Started Searching for {key}");
			showLine();
			//For Every block in the chain, read from binary file, block by block 
			if(fileLength % blockSize == 0)
            {
				for (int i = 0; i < fileLength / blockSize; i++)
				{
					if (i == (fileLength / blockSize) * (0.25))
					{
						Console.WriteLine("25% is done");
					}
					else if (i == (fileLength / blockSize) * (0.5))
					{
						Console.WriteLine("Half way there, 50% is done");
					}
					else if (i == (fileLength / blockSize) * (0.75))
					{
						Console.WriteLine("Nearly There,75% is done");
					}
					//initialise new block to store transactions


					if (i * blockSize < fileLength)
					{
						//Go to Byte: 512 * block Num 
						stream.Seek(i * blockSize, SeekOrigin.Begin);
						string blockData = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
						blockData = blockData.Substring(85, 12129);
						//parse from transactions characters to transactional data and store in list
						List<Transaction> result = utilities.SearchForTransactions(blockData, key);

						if (result.Count > 0)
						{
							int count = 0;
							foreach (Transaction trans in result)
							{
								count++;
								userTransactions.Add(trans.ToUserTransaction(count));
							}
						}
						else
						{
							//Console.WriteLine($"No Transactions Found for {key} in Block: {i}");
						}
					}
				}
			}else
            {
				Console.WriteLine("ERROR: Reading File!!");
            }
			
			stream.Close();
			return userTransactions;
		}

		static void InternalGetAllUsers()
        {
			Transaction utilities = new Transaction();
			Stream stream = File.Open("C:/temp/Master.dat", FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;
			List<string> users = new List<string>();

			Console.WriteLine("Uploading Users");
			for (int i = 0; i < fileLength / blockSize; i++)
            {
				if (i == (fileLength / blockSize)* (0.25))
				{
					Console.WriteLine("25% is done");
				}
				else if (i == (fileLength / blockSize) * (0.5))
				{
					Console.WriteLine("Half way there, 50% is done");
				}else if (i == (fileLength / blockSize) * (0.75))
				{
					Console.WriteLine("Nearly There,75% is done");
				}

				stream.Seek(i * blockSize, SeekOrigin.Begin);
				string blockData = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
				blockData = blockData.Substring(85, 12129);
				List<string> result = utilities.GetUsersFromText(blockData);
				foreach(string user in result)
                {
                    if (!users.Contains(user))
                    {
						users.Add(user);
                    }
                }
			}
			users.Sort();
			blockchainServices.Blockchain.Users = users;
			stream.Close();
			binReader.Close();
		}

		static void SearchTransactionsByNode(string key)
        {
			Stopwatch timer = new Stopwatch();
			timer.Start();
			if(key == "-") {
				Console.WriteLine("Invalid Token Inputted");
			}else
            {
				List<UserTransaction> result = InternalSeekTransactionsFromFile(key.Trim());
				showLine();
				if (result.Count == 0)
                {
					Console.WriteLine("No Transactions Found");
                }else
                {
					int count = 0;
					foreach (UserTransaction uTrans in result)
                    {
						count++;
                    }

					Console.WriteLine($"Transaction for {key}: "+count);
					Console.WriteLine($"Time Taken for Searching for {key}:" + timer.Elapsed.ToString());
                }
			}
			timer.Stop();
        }
        #endregion
        static void InternalWriteToFixedLengthRecord(string text)
        {
			List<Block> list = blockchainServices.Blockchain.Chain;

            if (blockchainServices.Blockchain.Genesis.PreviousHash.Trim() != "0")
            {
				if (list[0].Hash == blockchainServices.Blockchain.Genesis.Hash)
				{
					list.RemoveAt(0);
				}
			}		
			var engine = new FileHelperEngine<Block>();
			
			engine.WriteFile($"C:/temp/{text}.txt", list);
			Console.WriteLine($"Written to {text}.txt");
			
        }

		static Block InternalGetLastBlock()
        {
			Stream stream = File.Open("C:/temp/Master.dat", FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			string tempFile = "C:/temp/temp.txt";
			StreamWriter temp = new StreamWriter(tempFile);
			var engine = new FileHelperEngine<Block>();


			stream.Seek(-blockSize, SeekOrigin.End);
			string tempString = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
			Console.WriteLine(tempString);
			temp.WriteLine(tempString);
			temp.Close();
			stream.Close();
			
			Block[] tempGenesis = engine.ReadString(tempString);
			
			binReader.Close();

            if (File.Exists(tempFile))
            {
				File.Delete(tempFile);
            }


			if(tempGenesis.Length > 0)
            {
				return tempGenesis[0];
            }
			return new Block();
			
		}

		static async void InternalOverWriteFromStorage ()
        {
			Stopwatch timer = new Stopwatch();
			timer.Start();
			Block[] output;
			Console.WriteLine("Uploading Blockchain From Local Files, Please do not mine blocks until it is finished");
			await Task.Run(() =>
			{
				var engine = new FileHelperEngine<Block>();
				output = engine.ReadFile("C:/temp/convert.txt");
				List<Block> tempChain;
				tempChain = output.ToList();
				blockchainServices.Blockchain.Chain = tempChain;
				InternalGetAllUsers();
				Console.WriteLine("Engine Has finished");
			});
			Console.WriteLine("Time Taken to execute code"+timer.Elapsed.ToString());
			timer.Stop();
			ShowCommandLine();
		}

		static void InternalReadFromBinaryToConvert(string filepath)
        {
			byte[] byteToString;
			
			Stream readStream = File.Open(filepath, FileMode.Open);
			BinaryReader binReader = new BinaryReader(readStream, Encoding.ASCII);
			StreamWriter streamWriter = new StreamWriter("C:/temp/convert.txt");
			long readerLength = binReader.BaseStream.Length;
			int blockLength = blockSize;
			//writing to the file directly from reader

			for (int i = 0; i < (readerLength / blockLength); i++)
			{
				byteToString = binReader.ReadBytes(blockLength);
				string temp = Encoding.ASCII.GetString(byteToString);
				streamWriter.WriteLine(temp);
			}
			Console.WriteLine("Wrote from Master.dat to convert.txt sucessfully");
			//closing the streams 
			streamWriter.Close();
			binReader.Close();
			readStream.Close();
		}
		//Write to Temp file and write to master file asyncshrounsaly 

		static void showLine()
        {
			Console.WriteLine("-----------------");
		}

		#endregion
		static void WriteFromFixedLengthToBinary(string savePath)
		{
			//Write from Blockchain to Text
			InternalWriteToFixedLengthRecord(savePath);

			string origin = $"C:/temp/{savePath}.txt";
			StreamReader textReader = new StreamReader(origin);
			string filepath = "C:/temp/Master.dat";
			Stream writeStream = File.Open(filepath, FileMode.Append);
			BinaryWriter binaryWriter = new BinaryWriter(writeStream, Encoding.ASCII);
			
			//Converting from string to bytes for text sanitation to avoid weird ascii translations
			byte[] byteToString = Encoding.ASCII.GetBytes(textReader.ReadToEnd().Replace("\r\n", string.Empty));
			binaryWriter.Write(byteToString);

			//Closing the writing streams
			writeStream.Close();
			textReader.Close();
			binaryWriter.Close();
			//--------------------------------------------------------
			showLine();
			Console.WriteLine("Successfull Write to Master.dat");
			//2 streams for binary reader and final stream for text writer
			InternalReadFromBinaryToConvert(filepath);
		}

		static void ReadFromConvertedBinary()
		{
			var engine = new FileHelperEngine<Block>();
			Block[] output = engine.ReadFile("C:/temp/convert.txt");
			List<Block> tempChain;
			tempChain = output.ToList();

			Console.WriteLine("Overwrite chain from memory? Type Yes/Y for yes.");
			string temp = Console.ReadLine();
			if (temp == "yes" || temp == "y")
			{
				blockchainServices.Blockchain.Chain = tempChain;
			}

			foreach (Block block in output)
			{
				Console.WriteLine(block.ToString());
				foreach (Transaction trans in block.Transactions)
				{
					showLine();
					Console.WriteLine(trans.ToString());
				}
				showLine();
			}
			Console.WriteLine("Read file from convert.txt");

		}

		static void GenerateBlocks(int blocks)
		{
			int transNo = 512;
			int blockNo = blocks;
			Random rnd = new Random();
			//Creating Transactions
			for (int i = 0; i < transNo * blockNo; i++)
			{
				int amount = rnd.Next(1, 1000);
				int baseIP = 3000;
				int sender = rnd.Next(0, 2000);
				int receiver = rnd.Next(0, 2000);

				if (rnd.Next(10) * InternalIndicateWeighting(sender) < 4.5)
				{
					sender = rnd.Next(0, 2000);
				}

				if (rnd.Next(10) * InternalIndicateWeighting(receiver) < 4.5)
				{
					receiver = rnd.Next(0, 2000);
				}
				while (sender == receiver)
				{
					receiver = rnd.Next(0, 200);
				}
				CommandTransactionsAdd((baseIP + sender).ToString(), (baseIP + receiver).ToString(), amount.ToString(), i.ToString());

				if ((i % 512) == 0 && blockchainServices.Blockchain.PendingTransactions.Count != 1)
				{
					CommandBlockchainMine("3002");

				}
			}
			CommandBlockchainMine("3002");
			WriteFromFixedLengthToBinary("temp");

			blockchainServices.RefreshBlockchain();
		}

		#region Blockchain Commands
		static void CommandBlockchainMine(string RewardAddress)
		{
			Console.WriteLine($"  Mining new block... Difficulty {blockchainServices.Blockchain.Difficulty}.");
			blockchainServices.MineBlock(RewardAddress);
			Console.WriteLine($"  Block has been added to blockchain. Blockhain length is {blockchainServices.BlockchainLength().ToString()}.");
			Console.WriteLine("");
			if (useNetwork)
			{
				NetworkBlockchainMine(blockchainServices.LatestBlock());
			}
			Console.WriteLine("");
		}

		static void CommandListNodes(List<string> Nodes)
		{
			foreach (string node in Nodes)
			{
				Console.WriteLine($"  Node: {node}");
			}
			Console.WriteLine("");
		}

		static void CommandListPendingTransactions(List<Transaction> PendingTransactions)
		{
			foreach (Transaction transaction in PendingTransactions)
			{
				Console.WriteLine($"  Transaction: {transaction.Amount} from {transaction.SenderAddress} to {transaction.ReceiverAddress} with description {transaction.Description}");
			}
			Console.WriteLine("");
		}

		static void CommandNodeAdd(string NodeUrl)
		{
			if (!NodeUrl.EndsWith("/")) NodeUrl += "/";
			nodeServices.AddNode(NodeUrl);
			Console.WriteLine($"  Node {NodeUrl} added to list of blockchain peer nodes.");
			if (useNetwork)
			{
				NetworkRegister(NodeUrl);
				CommandBlockchainUpdate();
			}
			Console.WriteLine("");
		}

		static void CommandHelp()
		{
			Console.WriteLine("Commands:");
			Console.WriteLine("h, help = list of commands.");
			Console.WriteLine("q, quit = exit the program.");
			Console.WriteLine("na, node-add [url] = connect current node to other node.");
			Console.WriteLine("nr, node-remove [url] = disconnect current node from other node.");
			Console.WriteLine("nl, nodes-list = list connected nodes.");
			Console.WriteLine("ta, transaction-add [senderAddress] [receiverAddress] [Amount] [Description] = create new transaction.");
			Console.WriteLine("np, transactions-pending = list of transactions not included in block.");
			Console.WriteLine("bm, blockchain-mine [rewardAddress] = create block from pending transactions,");
			Console.WriteLine("bv, blockchain-valid = Validates blockchain.");
			Console.WriteLine("bl, blockchain-length = number of blocks in blockchain.");
			Console.WriteLine("b, block [index] = list info about specified block.");
			Console.WriteLine("bu, update, blockchain-update = update blockchain to the longest in network.");
			Console.WriteLine("bal, balance-get [address] = get balance for specified address.");
			Console.WriteLine("gen, for generating a transaction auto");
			Console.WriteLine("w, write, For Writing the blockchain to FixedLength Records");
			Console.WriteLine("r, read, For reading from binary file");
			Console.WriteLine();
			Console.WriteLine("Email me: dejan@mauer.si");
			Console.WriteLine();

		}

		static void CommandBlockchainValidity()
		{
			var result = blockchainServices.isBlockchainValid();
			if (result == true)
			{
				Console.WriteLine($"  Blockchain is valid.");
			}
			else
			{
				Console.WriteLine($"  Blockchain is invalid.");
			}
			Console.WriteLine("");
		}


		static void CommandBlockchainLength()
		{
			var length = blockchainServices.BlockchainLength();
			Console.WriteLine($"  Blockchain length is {length.ToString()}.");
			Console.WriteLine("");
		}

		static void CommandBlock(int Index)
		{
			var block = blockchainServices.Block(Index);
			Console.WriteLine($"  Block {Index}:");
			Console.WriteLine($"    Hash: {block.Hash}:");
			Console.WriteLine($"    Nonce: {block.Nonce}:");
			Console.WriteLine($"    Previous hash: {block.PreviousHash}:");
			Console.WriteLine($"    #Transactions : {block.Transactions.Count}:");
			showLine();
			foreach (Transaction trans in block.Transactions)
			{
				Console.WriteLine(trans.ToString());
				showLine();
			}

		}

		static void CommandBalance(string Address)
		{
			var balance = blockchainServices.Balance(Address);
			Console.WriteLine($"  Balance for address {Address} is {balance.ToString()}.");
			Console.WriteLine("");
		}

		static void CommandBlockchainUpdate()
		{
			Console.WriteLine($"  Updating blockchain with the longest found on network.");
			if (useNetwork)
			{
				NetworkBlockchainUpdate();
			}
			Console.WriteLine("");
		}

		static void CommandNodeRemove(string NodeUrl)
		{
			if (!NodeUrl.EndsWith("/")) NodeUrl += "/";
			nodeServices.RemoveNode(NodeUrl);
			Console.WriteLine($"  Node {NodeUrl} removed to list of blockchain peer nodes.");
			Console.WriteLine("");
		}

		static void CommandTransactionsAdd(string SenderAddress, string ReceiverAddress, string Amount, string Description)
		{
			Transaction transaction = new Transaction(SenderAddress, ReceiverAddress, Decimal.Parse(Amount), Description);
			blockchainServices.AddTransaction(transaction);
			Console.WriteLine($"  {Amount} from {SenderAddress} to {ReceiverAddress} transaction added to list of pending transactions.");
			Console.WriteLine("");

			if (useNetwork)
			{
				NetworkTransactionAdd(transaction);
			}
			Console.WriteLine("");
		}

		#endregion


		#region NetworkSend

		static async void NetworkRegister(string NewNodeUrl)
		{
			// automatically notify node you are registering about this node
			using (var client = new HttpClient())
			{
				try
				{
					Console.WriteLine($"  Initiating API call: calling {NewNodeUrl} node-register {baseAddress}");

					var content = new FormUrlEncodedContent(
						new Dictionary<string, string>
						{
							{"" , baseAddress}
						}
					);

					var response = await client.PostAsync(NewNodeUrl + "api/network/register", content);
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("  " + ex.Message);
					Console.ResetColor();
				}
			}
		}

		static async void NetworkUnregister(string UnregisterNodeUrl)
		{
			// automatically notify node you are registering about this node
			using (var client = new HttpClient())
			{
				try
				{
					Console.WriteLine($"  Initiating API call: calling {UnregisterNodeUrl} node-unregister {baseAddress}");

					var content = new FormUrlEncodedContent(
						new Dictionary<string, string>
						{
							{"" , baseAddress}
						}
					);

					var response = await client.PostAsync(UnregisterNodeUrl + "api/network/unregister", content);
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("  " + ex.Message);
					Console.ResetColor();
				}
			}
		}

		static async void NetworkTransactionAdd(Transaction Transaction)
		{
			// automatically notify node you are registering about this node
			using (var client = new HttpClient())
			{
				try
				{
					// iterate all registered nodes
					foreach (string node in nodeServices.Nodes)
					{
						Console.WriteLine($"  Initiating API call: calling {node} transaction-add {Transaction.Description}");
						var response = await client.PostAsJsonAsync<Transaction>(node + "api/transactions/add", Transaction);
						Console.WriteLine();
					}
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("  " + ex.Message);
					Console.ResetColor();
				}
			}
		}

		static async void NetworkBlockchainUpdate()
		{
			int maxLength = 0;
			string maxNode = "";

			// find the node with the longest blockchain
			using (var client = new HttpClient())
			{
				try
				{

					// iterate all registered nodes
					foreach (string node in nodeServices.Nodes)
					{
						Console.Write($"  Initiating API call: calling {node} blockchain-length.");
						var response = await client.GetAsync(node + "api/blockchain/length");

						int newLength = JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync());

						if (newLength > maxLength)
						{
							maxNode = node;
							maxLength = newLength;
						}
						Console.WriteLine(response);
						Console.WriteLine();
					}
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("  " + ex.Message);
					Console.ResetColor();
				}

				Console.WriteLine($"    Max blockchain length found on {maxNode} with length {maxLength}.");
				if (blockchainServices.BlockchainLength() >= maxLength)
				{
					Console.WriteLine($"    No blockchain found larger than existing one.");
					Console.WriteLine();
					return;
				}


				// get missing blocks
				try
				{
					for (int i = blockchainServices.BlockchainLength(); i < maxLength; i++)
					{
						Block newBlock;
						Console.WriteLine($"      Requesting block {i} from {maxNode}...");
						var response = await client.GetAsync(maxNode + $"api/blockchain/getblock?id={i}");
						newBlock = JsonConvert.DeserializeObject<Block>(await response.Content.ReadAsStringAsync());

						blockchainServices.Blockchain.Chain.Add(newBlock);
						if (!blockchainServices.isBlockchainValid())
						{
							blockchainServices.Blockchain.Chain.Remove(newBlock);
							Console.WriteLine($"    After adding block {i} blockchain is not valid anymore. Canceling...");
							break;
						}

					}

				}
				catch (Exception)
				{

					throw;
				}

				Console.WriteLine($"    Updated!");
				Console.WriteLine();
			}
		}


		static async void NetworkBlockchainMine(Block NewBlock)
		{
			// automatically notify node you are registering about this node
			using (var client = new HttpClient())
			{
				try
				{
					// iterate all registered nodes
					foreach (string node in nodeServices.Nodes)
					{
						Console.WriteLine($"  Initiating API call: calling {node} blockchain-newBlock {NewBlock.Hash}");
						var response = await client.PostAsJsonAsync<Block>(node + "api/blockchain/newblock", NewBlock);
						Console.WriteLine();
					}
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("  " + ex.Message);
					Console.ResetColor();
				}
			}
		}



		#endregion
	}
}

