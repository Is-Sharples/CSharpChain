using CSharpChainModel;
using CSharpChainNetwork.SQL_Class;
using CSharpChainNetwork.PointerIndex;
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
using FileHelpers;
using Weighted_Randomizer;
using System.Data.SQLite;


namespace CSharpChainNetwork
{
	static class Program
	{
		
		static string database = "C:/temp/SQLite/blockchain";
		static string master = "C:/temp/Master.dat";
		static string baseAddress;
		public static BlockchainServices blockchainServices;
		public static NodeServices nodeServices;
		static int blockSize = 12288;
		static bool useNetwork = true;
		static int maxUsers = 2000;
		static Transaction utilities = new Transaction();
		static User[] masterUsers = new User[maxUsers];
		

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
				for(int i = 0; i < masterUsers.Length; i++)
                {
					int temp = 3000 + i;
					masterUsers[i] = new User($"{temp}");
                }
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
							SimpleBlockchainLength(true);
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
							GenerateSQLLite(true);
							if (command[1].Length > 0)
                            {
								GenerateBlocks(int.Parse(command[1]));
								
							}else
                            {
								ShowIncorrectCommand();
							}
							break;
						case "read":
						case "r":
							ReadFromConvertedBinary();
							
							break;
						case "search":
						case "s":
							SearchTransactionsByNode(command[1], command[2]);
							break;
						case "f":
						case "freq":
							GetFrequencyDistribution();
							break;
						case "gensql":
							GenerateSQLLite(true);
							GetLocationOfBlocks();
							break;
						case "t":
							HashSet<string> users = new HashSet<string>();
							users.Add("4000");
							users.Add("3999");
							PointerForIndex test = new PointerForIndex();
							Dictionary<string, string> res = test.CreatePointerIndexFile(222,users);
                            if (res != null)
                            {
								HashSet<long> positionsToEdit = test.PreparePositionsForEditBlockIndexFile(res, 222);
								test.EditBlockIndexFile(positionsToEdit,12345);
							}
							break;
						case "ss":
							SearchForWalletInSQLite(command[1]);
							break;

						case "script":
							Stopwatch timer = new Stopwatch();
							timer.Start();
                            for (int i = 0; i < 5; i++)
                            {
								GenerateBlocks(10000);
								Console.WriteLine($"finished loop:{i}");
							}
							Console.WriteLine("Time Taken for 50000 blocks" + timer.Elapsed.ToString());
							GetFrequencyDistribution();
							Console.WriteLine("Time Taken for 50000 blocks and freq report:"+ timer.Elapsed.ToString());
							timer.Stop();
							break;
						case "cls":
							Console.Clear();
							break;
						default:
							ShowIncorrectCommand();
							break;
					}
				} while (commandLine != "q");
			}
		}
        #region InternalFunctions 

        #region Block Generation
        static IWeightedRandomizer<string> randomizer = new DynamicWeightedRandomizer<string>();
		static int[] weight = new int[2000];
		//Used to setup Weights for generating blocks
		static void InternalSetupWeights()
        {
			for (int i = 0; i < weight.Length; i++)
			{
				weight[i] = InternalNormalDistribution(3000 + i, 4000, 300);
			}

			for (int i = 0; i < 2000; i++)
			{
				int temp = 3000 + i;
				randomizer.Add(temp.ToString(), weight[i]);
			}
		}
		//Used to calculate weights
		static int InternalNormalDistribution(double wallet, double mean, double standardDev)
		{
			double ans = (wallet - mean) / standardDev;
			ans = ans * ans;
			ans = -0.5 * ans;
			ans = Math.Exp(ans);
			//ans = (1 / (standardDev * Math.Sqrt(2 * Math.PI)))* ans;
			ans = standardDev * ans;
			return (int)ans;
		}
		#endregion

		#region Internal Searching functions

		static void InternalParseBlockLocations(string[] locations, string key)
		{
			Stream stream = File.Open(master, FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;
			List<Block> blocks = new List<Block>();
			Transaction utils = new Transaction();
			List<Transaction> Transactions = new List<Transaction>();
			int blockNum = 0;
			for (int i = 0; i < locations.Length; i++)
			{
				string current = locations[i].Trim();
				if (current == "F")
				{
					blockNum++;
				}
				else if (current == "T")
				{
					stream.Seek(blockNum * blockSize, SeekOrigin.Begin);
					string temp = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
					temp = temp.Substring(85, 12129);
					List<Transaction> res = utils.SearchForTransactionsFromIndex(temp, key);
					Transactions.AddRange(res);
					blockNum++;
				}
				else if (current.Any(char.IsDigit) && current.Contains('F'))
				{
					int num = int.Parse(current.Replace("F", ""));
					blockNum += num;
				}
				else if (current.Any(char.IsDigit) && current.Contains('T'))
				{
					int num = int.Parse(current.Replace("T", ""));
					int temp = blockNum;
					for (int j = blockNum; j < temp + num; j++)
					{
						stream.Seek(blockNum * blockSize, SeekOrigin.Begin);
						string stringResult = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
						stringResult = stringResult.Substring(85, 12129);
						List<Transaction> result = utils.SearchForTransactionsFromIndex(stringResult, key);
						Transactions.AddRange(result);
						blockNum++;
					}
				}
			}
			decimal amount = 0;
			foreach (Transaction trans in Transactions)
			{
				amount += trans.Amount;
			}

			Console.WriteLine($"Wallet Balance for {key}: {amount}");
			stream.Close();
			binReader.Close();
		}

		static List<UserTransaction> InternalSeekTransactionsFromFile(string key)
        {
			List<UserTransaction> userTransactions = new List<UserTransaction>();
			Transaction utilities = new Transaction();
			Stream stream = File.Open(master, FileMode.Open);
			BinaryReader binReader = new BinaryReader(stream, Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;
			List<Transaction> transactions = new List<Transaction>();
			Console.WriteLine($"Started Searching for {key}");
			showLine();
			//For Every block in the chain, read from binary file, block by block 
			if(fileLength % blockSize == 0)
            {
				Stopwatch timer = new Stopwatch();
				timer.Start();
				for (int i = 0; i < fileLength / blockSize; i++)
				{
					InternalShowProgress(i,fileLength/blockSize);

					if (i * blockSize < fileLength)
					{
						//Go to Byte: 512 * block Num 
						stream.Seek(i * blockSize, SeekOrigin.Begin);
						string blockData = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
						blockData = blockData.Substring(85, 12129);
						//parse from transactions characters to transactional data and store in list
						List<UserTransaction> result = utilities.SearchForTransactions(blockData, key, i);
						if (result.Count > 0)
						{
							userTransactions.AddRange(result);
						}
						else
						{
							//Console.WriteLine($"No Transactions Found for {key} in Block: {i}");
						}
					}
				}

				Console.WriteLine("Time Taken:"+timer.Elapsed.ToString());
				timer.Stop();
			}else
            {
				Console.WriteLine("ERROR: Reading File!!");
            }

			stream.Close();
			return userTransactions;
		}

		static void InternalAppendSQLiteIndex(Block[] blocks)
		{
			Stopwatch timer = new Stopwatch();
			User[] users = masterUsers;
			Transaction utilities = new Transaction();
			SQLiteController sql = new SQLiteController(database);

			timer.Start();
			foreach (User user in users)
			{
				user.locationCSV = sql.ReadDataForAppending("location", "users", $"WHERE wallet='{user.name}'", false);
				user.locationCSV = InternalDecodeIndex(user.locationCSV);
				
			}

			for (int i = 0; i < blocks.Length; i++)
			{
				Dictionary<string, char> result = utilities.GetUsersForIndex(blocks[i], users);
				Console.WriteLine($"Progress: {i + 1}/{blocks.Length}");
				foreach (KeyValuePair<string, char> user in result)
				{
					if (user.Key != "SYSTEM" && user.Key != "System2")
					{
						int location = int.Parse(user.Key) - 3000;
						users[location].locationString.Add(user.Value.ToString());

					}
				}
			}
			//remember to clear locationCSV
			foreach (User user in users) 
			{
				user.locationCSV += string.Join("",user.locationString);
				string temp = InternalIndexCreation(user.locationCSV);
				sql.CustomCommand($"UPDATE users SET location = '{temp}' WHERE wallet='{user.name}'");
				user.locationCSV = "";
				user.locationString = new List<string>();
			}


			Console.WriteLine($"Time Taken for sql{timer.Elapsed}");
			timer.Stop();
			sql.CloseConnection();
		}

        #endregion

        #region read/write functions

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
            try{
				engine.WriteFile($"C:/temp/{text}.txt", list);
				Console.WriteLine($"Written to {text}.txt");
			}
			catch
            {
				Console.WriteLine("Engine Failure");
            }
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

		#endregion

		#region utilities

		static void InternalShowProgress(int place, long total)
		{
			if (place == (total) * (0.25))
			{
				Console.WriteLine("25% is done");
			}
			else if (place == (total) * (0.5))
			{
				Console.WriteLine("Half way there, 50% is done");
			}
			else if (place == (total) * (0.75))
			{
				Console.WriteLine("Nearly There,75% is done");
			}
			else if (place == (total) * (0.9))
			{
				Console.WriteLine("So Close, 90% is done");
			}
			else if (place == (total) * (0.15))
			{
				Console.WriteLine("We've barely started, 15% is done");
			}
		}

		static void ShowIncorrectCommand()
		{
			Console.WriteLine("Ups! I don't understand...");
			Console.WriteLine("");
		}

		static void showLine()
        {
			Console.WriteLine("-----------------");
		}

		static void InternalDisplayStringList(List<string> input)
        {
            foreach (string text in input)
            {
				Console.WriteLine(text);
            }
        }

		static void InternalShowAllUsers(User[] users)
        {
			foreach(User user in users)
            {
				Console.WriteLine(user.name);
            }
			Console.WriteLine("Length"+users.Length);
        }

        #endregion

        #region SQLiteIndexGeneration

        static string InternalIndexCreation(string input)
        {
			string answer = string.Join("",InternalToLetters(input,new List<char>()));
			answer = InternalConvertAb(InternalRunLengthEncodingOfValues(answer), new List<string>());
			answer = InternalRunLengthEncodingOfAB(answer);
			return answer;
		}

		static string InternalDecodeIndex(string index)
        {
			string temp = InternalInverseToLetters(InternalDecodeRuneLengthRemoveAB(InternalDecodeRunLengthRemoveNumbers(index)));
			return temp;
		}

		static string InternalRunLengthEncodingOfValues(string input)
        {
			List<string> toReturn = new List<string>();
			List<char> temp = new List<char>();
			for(int i = 0; i< input.Length; i++)
            {
				if (temp.Count == 0)
				{
					temp.Add(input[i]);
				} else if (input[i] == temp[0])
				{
					temp.Add(input[i]);
				} else
				{
					int count = temp.Count;
					string result;
					if (count != 1)
					{
						result = $"{count}{temp[0]}";
					} else
					{
						result = $"{temp[0]}";
					}
					toReturn.Add(result);
					temp = new List<char>();
					temp.Add(input[i]);
				}	
            }

			if(temp.Count > 0)
            {
				string result;
				if (temp.Count != 1)
                {
					result = $"{temp.Count}{temp[0]}";
				}else
                {
					result = $"{temp[0]}";
                }
				toReturn.Add(result);
            }
			return string.Join("",toReturn);
        }

		static string InternalRunLengthEncodingOfAB(string input)
        {
			List<string> toReturn = new List<string>();
			List<char> temp = new List<char>();
            for (int i = 0; i < input.Length; i++)
            {
				char current = input[i];
                if (current == 'A' || current == 'B')
                {
					temp.Add(current);
                }
                else
                {
					if (temp.Count > 0)
					{
						if (current != temp[0])
						{
							if (temp.Count != 1)
							{
								int num = temp.Count;
								toReturn.Add($"{num}{temp[0]}");
							}
							else
							{
								toReturn.Add($"{temp[0]}");
							}
							toReturn.Add(current.ToString());
							temp = new List<char>();
						}
					}else
                    {
						toReturn.Add(current.ToString());
                    }

				}
            }
			if(temp.Count > 0)
            {
				if(temp.Count != 1)
                {
					int num = temp.Count;
					toReturn.Add($"{num}{temp[0]}");
                }else
                {
					toReturn.Add($"{temp[0]}");
                }
            }

			return string.Join("",toReturn);
        }

		static List<char> InternalToLetters(string index, List<char> toReturn)
        {

            for (int i = 0; i < index.Length;i++)
            {
				switch (index[i])
				{
					case '0':
						toReturn.Add('F');
						break;
					case '1':
						toReturn.Add('T');
						break;
					default:
						break;
				}
			}

			return toReturn;
        }

		static string InternalInverseToLetters(string index)
        {
			List<char> toReturn = new List<char>();
            for (int i = 0; i < index.Length; i++)
            {
				char current = index[i];
                switch (current)
                {
					case 'T':
						toReturn.Add('1') ;
						break;
					case 'F':
						toReturn.Add('0');
						break;
					default:
						break;
                }
            }

			return string.Join("",toReturn);
        }

		static string InternalConvertAb(string index, List<string> toReturn)
        {
			List<char> digits = new List<char>();
			for (int i = 0;i < index.Length; i++)
            {
                if (char.IsDigit(index[i]))
                {
					digits.Add(index[i]);
					
                }else if (!char.IsDigit(index[i]) && digits.Count > 0)
                {
					
					string num = string.Join("",digits);
					toReturn.Add($"{num}{index[i]}");
					digits = new List<char>();
                }else if (i <index.Length -1 &&  index.Substring(i,2) == "TF")
                {
					toReturn.Add("A");
					i++;
                }else if (i < index.Length -1 && index.Substring(i,2) == "FT")
                {
					toReturn.Add("B");
					i++;
                }else
                {
					toReturn.Add($"{index[i]}");
                }
            }
			
            if (digits.Count > 0)
            {
				string num = string.Join("",digits);
				toReturn.Add($"{num}{index[index.Length-1]}");
				digits = new List<char>();
            }
			return string.Join("",toReturn);
        }

		static string InternalDecodeRunLengthRemoveNumbers(string index) {
			List<string> toReturn = new List<string>();
			List<char> digits = new List<char>();
			for (int i =0; i < index.Length; i++)
            {
				char current = index[i];
                if (char.IsDigit(current))
                {
					digits.Add(current);
                }else if (!char.IsDigit(current) && digits.Count > 0)
                {
					string num = string.Join("",digits);
					int number = int.Parse(num);
                    for (int j = 0; j < number; j++) {
						toReturn.Add($"{current}");
					}
					digits = new List<char>();
                }else
                {
					toReturn.Add(current.ToString());
                }
            }
			
			return string.Join("",toReturn);
		}

		static string InternalDecodeRuneLengthRemoveAB(string index)
        {
			List<string> toReturn = new List<string>();
			for (int i = 0; i < index.Length; i++)
            {
				char current = index[i];
                if (current == 'A')
                {
					toReturn.Add("TF");
                }else if (current == 'B')
                {
					toReturn.Add("FT");
                }else
                {
					toReturn.Add(current.ToString());
                }
            }
			return string.Join("",toReturn);
        }

        #endregion

        #endregion

        #region CreatedCommands

        #region Writing&Reading Commands

        static void WriteFromFixedLengthToBinary(string savePath)
		{
			//Write from Blockchain to Text
			InternalWriteToFixedLengthRecord(savePath);

			string origin = $"C:/temp/{savePath}.txt";
			StreamReader textReader = new StreamReader(origin);
			Stream writeStream = File.Open(master, FileMode.Append);
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
			Console.WriteLine($"Successfull Write to {master}");
			//2 streams for binary reader and final stream for text writer
			InternalReadFromBinaryToConvert(master);
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

		#endregion

		#region SQLCommands

		static void GenerateSQLLite(bool primaryKey)
		{
			string tableName = "users";
			string columns = "";
			if (primaryKey)
			{
				columns = "(wallet TEXT PRIMARY KEY, location TEXT)";
			}
			else
			{
				columns = "(wallet TEXT, location TEXT)";
			}

			SQLiteController sQLite = new SQLiteController("C:/temp/SQLite/blockchain");

			if (sQLite.CheckForTable(tableName))
			{
				sQLite.CreateTable(tableName, columns);
			}
			else
			{
				Console.WriteLine("Table Already Exists!");
			}
		}

		static void GetLocationOfBlocks()
		{
			Stopwatch timer = new Stopwatch();
			timer.Start();
			User[] users = masterUsers;
			Stream readStream = File.Open(master, FileMode.Open);
			BinaryReader binReader = new BinaryReader(readStream, Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;
			SQLiteController sql = new SQLiteController("C:/temp/SQLite/blockchain");

			Console.WriteLine("Started Getting all locations of all users");
			for (int i = 0; i < fileLength / blockSize; i++)
			{
				readStream.Seek(i * blockSize, SeekOrigin.Begin);
				string blockData = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
				blockData = blockData.Substring(85, 12129);
				Dictionary<string, char> result = utilities.GetUsersForIndex(blockData, users);
				Console.WriteLine($"Progress: {i}/{fileLength / blockSize}");

				InternalShowProgress(i, fileLength / blockSize);

				foreach (KeyValuePair<string, char> user in result)
				{
					if (user.Key != "SYSTEM")
					{
						if (user.Key != "System2")
						{
							int location = int.Parse(user.Key) - 3000;
							users[location].locationString.Add(user.Value.ToString());
						}
					}
				}
			}

			foreach (User user in users)
			{

				user.locationCSV = string.Join("", user.locationString);
				user.locationString = new List<string>();
				user.locationCSV = InternalIndexCreation(user.locationCSV);
				sql.InsertData("users", $"(wallet, location) VALUES('{user.name}', '{user.locationCSV}')");
			}
			Console.WriteLine("Time Passed:" + timer.Elapsed.ToString());
			timer.Stop();
			readStream.Close();
			binReader.Close();

		}

		#endregion

		
		//before running this run 'genSQL' 
		static void GenerateBlocks(int blocks)
		{
			//to avoid resetting weights
            if (weight[0] == 0)
            {
				InternalSetupWeights();
			}
			long BlockLength = 1;
            if (File.Exists(master))
            {
				BlockLength = SimpleBlockchainLength(false);
			}
			PointerForIndex indexUtil = new PointerForIndex();
			Transaction util = new Transaction();
			Stopwatch timer = new Stopwatch();
			timer.Start();
			int transNo = 512;
			int blockNo = blocks;
			Random rnd = new Random();
			List<Block> newBlocks = new List<Block>();
			IWeightedRandomizer<string> amountRandomizer = new DynamicWeightedRandomizer<string>();
			amountRandomizer.Add("+", 6);
			amountRandomizer.Add("-", 4);
			//Creating Transactions
			for (int i = 0; i < transNo * blockNo; i++)
			{
				string[] amountValue = new string[2];
				amountValue[0] = amountRandomizer.NextWithReplacement();
				amountValue[1] = rnd.Next(1, 1000).ToString();
				string tempAmount = string.Join("",amountValue);
				//randomizer object comes from Weighted Randomizer sol
				//used for weighted randomisation 
				//Look at "Block Generation" subsection
				string sender = randomizer.NextWithReplacement();
				string receiver = randomizer.NextWithReplacement();

				while (sender == receiver)
				{
					receiver = randomizer.NextWithReplacement();
				}
				CommandTransactionsAdd(sender, receiver.ToString(), tempAmount, i.ToString());

				if ((i % transNo) == 0 && blockchainServices.Blockchain.PendingTransactions.Count != 1)
				{
					Block tempBlock = CommandBlockchainMine("System2");
					HashSet<string> users = util.GetUsersForPointerIndex(tempBlock);
					newBlocks.Add(tempBlock);
					Dictionary<string, string> result = indexUtil.CreatePointerIndexFile(BlockLength++,users);
                    if (result != null)
                    {
						HashSet<long> positionsToBeEdit = indexUtil.PreparePositionsForEditBlockIndexFile(result, BlockLength);
						Dictionary<long,long> positions2 = indexUtil.EditBlockIndexFile(positionsToBeEdit,BlockLength-1);
						indexUtil.OverWritePointerIndexBlockList(positions2);
					}

				}
			}
			Block block = CommandBlockchainMine("System2");
			newBlocks.Add(block);
			HashSet<string> tempUsers = util.GetUsersForPointerIndex(block);
			//indexUtil.CreateLastSeen(BlockLength++,tempUsers);
			Dictionary<string, string> result2 = indexUtil.CreatePointerIndexFile(BlockLength++,tempUsers);
            if (result2 != null)
            {
				HashSet<long> positionsToEdit = indexUtil.PreparePositionsForEditBlockIndexFile(result2,BlockLength);
				Dictionary<long,long> positions = indexUtil.EditBlockIndexFile(positionsToEdit,BlockLength-1);
				indexUtil.OverWritePointerIndexBlockList(positions);
			}
			WriteFromFixedLengthToBinary("temp");
			Console.WriteLine($"Time Taken for generating {blocks}:" + timer.Elapsed.ToString());
			blockchainServices.RefreshBlockchain();
			Console.WriteLine("Updating SQLite...");
			InternalAppendSQLiteIndex(newBlocks.ToArray());
			Console.WriteLine("Finished!!");
			timer.Stop();
		}

		static void GetFrequencyDistribution()
		{
			Stopwatch timer = new Stopwatch();
			timer.Start();
			User[] users = masterUsers;
			Stream readStream = File.Open(master, FileMode.Open);
			BinaryReader binReader = new BinaryReader(readStream, Encoding.ASCII);
			long fileLength = binReader.BaseStream.Length;
			StreamWriter writer = new StreamWriter("C:/temp/users.csv");
			
			Console.WriteLine("Started Getting Frequency of transactions");
			int total = 0;
			for (int i = 0; i < fileLength / blockSize; i++)
			{
				readStream.Seek(i * blockSize, SeekOrigin.Begin);
				string blockData = Encoding.ASCII.GetString(binReader.ReadBytes(blockSize));
				blockData = blockData.Substring(85, 12129);
				List<string> result = utilities.PartialGetUserCountFromText(blockData);

				InternalShowProgress(i,fileLength/blockSize);
				
				foreach(string user in result)
                {
                    try
                    {
						int num = int.Parse(user) - 3000;
						users[num].transactionCount++;
					}catch(Exception e)
                    {
						if(user == users[users.Length-1].name)
                        {
							users[users.Length - 1].transactionCount++;
                        }else if(user == users[users.Length - 2].name)
                        {
							users[users.Length - 2].transactionCount++;
                        }
                    }
                }
			}

			foreach (User user in users)
			{
				writer.WriteLine(user.name + "," + user.transactionCount);
				total += user.transactionCount;
			}
			//use only for debugging
			//writer.WriteLine("Total," + total);
			Console.WriteLine("Finished generating Frequency CSV at C:/temp");
			Console.WriteLine("Time Taken:"+ timer.Elapsed.ToString());
			readStream.Close();
			binReader.Close();
			writer.Close();
			timer.Stop();
		}

		static long SimpleBlockchainLength(bool show)
		{
			Stream stream = File.Open(master, FileMode.Open);
			BinaryReader binary = new BinaryReader(stream, Encoding.ASCII);
			long temp = binary.BaseStream.Length / blockSize;
            if (show)
            {
				Console.WriteLine(temp);
			}
			
			stream.Close();
			binary.Close();

			return temp;
		}

		#region SearchCommands


		static void SearchTransactionsByNode(string key, string showAll)
		{
			StreamWriter writer = new StreamWriter($"C:/temp/BlockList/{key}.csv");
			Stopwatch timer = new Stopwatch();
			decimal amount = 0;

			if (key == "-")
			{
				Console.WriteLine("Invalid Token Inputted");
			}
			else
			{
				//timer.Start();
				List<UserTransaction> result = InternalSeekTransactionsFromFile(key.Trim());
				List<int> foundInBlocks = new List<int>();
				foreach (UserTransaction user in result)
				{
					if (!foundInBlocks.Contains(user.blockIndex))
					{
						foundInBlocks.Add(user.blockIndex);
					}
				}


				showLine();
				if (result.Count == 0)
				{
					Console.WriteLine("No Transactions Found");
				}
				else
				{
					int count = 0;
					foreach (int index in foundInBlocks)
					{
						writer.WriteLine($"{key} appeared in Block," + index);
						count++;
					}

					if (showAll == "true")
					{
						foreach (UserTransaction trans in result)
						{
							Console.WriteLine(trans.ToString());
							showLine();
						}
					}
					foreach (UserTransaction trans in result)
					{
						if (trans.ReceiverAddress == key || trans.SenderAddress == key)
						{
							amount += trans.Amount;
						}
					}
					Console.WriteLine($"Balance for {key}: {amount}");
					Console.WriteLine($"Transactions for {key} found in No of Blocks: " + count);
					Console.WriteLine($"Transactions for {key}:" + result.Count);
					//Console.WriteLine($"Time Taken for Searching for {key}:" + timer.Elapsed.ToString());
				}
			}
			timer.Stop();
			writer.Close();
		}

		static string[] SearchForWalletInSQLite(string key)
		{
			Stopwatch timer = new Stopwatch();
			SQLiteController sql = new SQLiteController(database);
			timer.Start();
			StreamReader reader = new StreamReader(sql.ReadData("users", "location", false, $"WHERE wallet='{key}'"));
			List<string> locations = new List<string>();
			List<char> digits = new List<char>();

			string result = reader.ReadToEnd();
			result = InternalDecodeIndex(result);
			result = InternalRunLengthEncodingOfValues(string.Join("", InternalToLetters(result, new List<char>())));


			bool found = result.Contains('T');
			//Console.WriteLine(result.Trim());
			Console.WriteLine($"Started Searching for: {key}");
			if (found)
			{
				for (int i = 0; i < result.Length; i++)
				{
					char current = result[i];
					if (char.IsDigit(current))
					{
						digits.Add(current);

					}
					else if (!char.IsDigit(current) && digits.Count > 0)
					{
						int num = int.Parse(string.Join("", digits));
						locations.Add($"{num}{current}");
						digits = new List<char>();
					}
					else
					{
						if (current.ToString().Trim() != "")
						{
							locations.Add(current.ToString());
						}
					}
					//found = result.Contains('T');
				}
			}
			else
			{
				showLine();
				Console.WriteLine("No Transactions found");
			}


			reader.Close();
			InternalParseBlockLocations(locations.ToArray(), key);
			Console.WriteLine("Time Taken:" + timer.Elapsed.ToString());
			timer.Stop();

			return locations.ToArray();
		}

		#endregion

		#endregion

		#region Blockchain Commands
		static Block CommandBlockchainMine(string RewardAddress)
		{
			Console.WriteLine($"  Mining new block... Difficulty {blockchainServices.Blockchain.Difficulty}.");
			Block temp = blockchainServices.MineBlock(RewardAddress);
			Console.WriteLine($"  Block has been added to blockchain. Blockhain length is {blockchainServices.BlockchainLength().ToString()}.");
			Console.WriteLine("");
			if (useNetwork)
			{
				NetworkBlockchainMine(blockchainServices.LatestBlock());
			}
			Console.WriteLine("");
			return temp;
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
			Console.WriteLine("r, read, For reading from binary file");
			Console.WriteLine("s, For searching how many transactions a user has Ex. s 3002. Will also generate a csv as a log file");
			Console.WriteLine("f, freq, For generating a user id frequency csv");
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
		//obsolete
		static void CommandBalance(string Address)
		{
			var balance = blockchainServices.Balance(Address);
			Console.WriteLine($"  Balance for address {Address} is {balance.ToString()}.");
			Console.WriteLine("");
		}
		//obsolete
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

