using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using FileHelpers;
using CSharpChainModel;

namespace CSharpChainNetwork.MatrixIndex
{
    public class Matrix
    {
		public string matrixLoc;
		public string transactionPathStub = "C:/temp/Matrix/TransIndexFiles";
        public Matrix(string matrixLoc)
        {
			this.matrixLoc = matrixLoc;
        }

		public void SerialiseMatrix(Dictionary<string,string> matrix,string guidPrefix)
		{
			Stream stream = File.OpenWrite($"{transactionPathStub}/{guidPrefix}.dat");
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, matrix);
			stream.Close();

		}

		public void SerialiseMatrix(KeyValuePair<string, string> matrix)
        {
			Stream stream = File.OpenWrite($"C:/temp/Matrix/IndexFiles/{matrix.Key}.dat");
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, matrix);
			stream.Close();
		}

		public Dictionary<string,string> DeSerialiseTransMatrix(string guidPrefix)
		{
			using (Stream stream = File.OpenRead($"{transactionPathStub}/{guidPrefix}.dat"))
            {
				BinaryFormatter formatter = new BinaryFormatter();
				Dictionary<string, string> result = (Dictionary<string, string>)formatter.Deserialize(stream);
				stream.Close();
				return result;
			}
		}

		public KeyValuePair<string, string> DeSerialiseMatrix(string name)
        {
			Stream stream = File.OpenRead($"C:/temp/Matrix/IndexFiles/{name}.dat");
			BinaryFormatter formatter = new BinaryFormatter();
			KeyValuePair<string, string> result = (KeyValuePair<string, string>)formatter.Deserialize(stream);
			return result;
		}

		public void BuildMatrixIndex(string master, long blockSize)
        {
			BinaryReader reader = new BinaryReader(File.OpenRead(master),Encoding.ASCII);
			long fileLength = reader.BaseStream.Length;
			Dictionary<string, StringBuilder> tempIndex = new Dictionary<string, StringBuilder>();
			Dictionary<string, string> index = new Dictionary<string, string>();
			Transaction util = new Transaction();
			for (int i = 3000; i < 5000; i++)
			{
				tempIndex.Add(i.ToString(), new StringBuilder());
			}

			for (long i = 0; i < fileLength/blockSize; i++)
            {
				Console.WriteLine($"{i}/{fileLength/blockSize}");
				reader.BaseStream.Seek(i * blockSize,SeekOrigin.Begin);
				string blockData = Encoding.ASCII.GetString(reader.ReadBytes((int)blockSize));
				HashSet<string> users = util.GetUsersForPointerIndex(blockData.Substring(85, 37803));
                foreach (string user in users)
                {
                    
					tempIndex[user].Append($",{i}");
                    
                }
                
            }

            foreach (KeyValuePair<string,StringBuilder> kvp in tempIndex)
            {
				kvp.Value.Remove(0,1);
				index.Add(kvp.Key,kvp.Value.ToString());
            }
            foreach (KeyValuePair<string,string> kvp in index)
            {
				SerialiseMatrix(kvp);
            }

        }

		public void BuildTransactionIndex(string master, long blockSize,int lower, int upper)
        {
			
			int prefixLength = 5;
			Stream stream = File.OpenRead(master);
			BinaryReader reader = new BinaryReader(stream,Encoding.ASCII);
			long fileLength = reader.BaseStream.Length;
			var engine = new FileHelperEngine<Block>();
			List<Tuple<long, long>> limits = new List<Tuple<long, long>>();
			limits.Add(new Tuple<long, long>(0, 25000));
			limits.Add(new Tuple<long, long>(25001, 50000));
			limits.Add(new Tuple<long, long>(50001, 75000));
			limits.Add(new Tuple<long, long>(75001, 100000));
			limits.Add(new Tuple<long, long>(100001, 125000));
			limits.Add(new Tuple<long, long>(125001, 150000));
			limits.Add(new Tuple<long, long>(150001, 175000));
			limits.Add(new Tuple<long, long>(175001, 200000));
			Console.WriteLine();
			for (int x = lower; x < upper; x++)
            {
				Dictionary<Block, long> transLocations = new Dictionary<Block, long>();
				for (long i = limits[x].Item1; i <= limits[x].Item2; i++)
				{
					Console.WriteLine($"{i}/{fileLength / blockSize}");
					reader.BaseStream.Seek(i * blockSize, SeekOrigin.Begin);
					string blockData = Encoding.ASCII.GetString(reader.ReadBytes((int)blockSize));
                    if (blockData != "")
                    {
						Block block = engine.ReadString(blockData)[0];
						transLocations.Add(block, i);
					}
					if (i % 25000 == 0)
					{
						Dictionary<string, List<Tuple<string, string>>> index = new Dictionary<string, List<Tuple<string, string>>>();
						foreach (KeyValuePair<Block, long> kvp in transLocations)
						{
							Console.WriteLine(kvp.Value);
							foreach (Transaction trans in kvp.Key.Transactions)
							{
								string guidPrefix = trans.Guid.ToString().Substring(0, prefixLength);
								if (index.ContainsKey(guidPrefix))
								{
									index[guidPrefix].Add(new Tuple<string, string>(trans.Guid.ToString(), kvp.Value.ToString()));

								}
								else
								{
									index.Add(guidPrefix, new List<Tuple<string, string>>());
									index[guidPrefix].Add(new Tuple<string, string>(trans.Guid.ToString(), kvp.Value.ToString()));
								}
							}
						}

						foreach (KeyValuePair<string, List<Tuple<string, string>>> kvp in index)
						{
							string guidStub = kvp.Key;
							Dictionary<string, string> retrieved = new Dictionary<string, string>();
							Console.WriteLine(guidStub);
							if (File.Exists($"{transactionPathStub}/{guidStub}.dat"))
							{
								retrieved = DeSerialiseTransMatrix(guidStub);
								
							}
							foreach (Tuple<string, string> trans in kvp.Value)
							{
								retrieved.Add(trans.Item1, trans.Item2);
							}
							SerialiseMatrix(retrieved, guidStub);


							///serialise the list of tuples 

						}
						transLocations = new Dictionary<Block, long>();
						index = new Dictionary<string, List<Tuple<string, string>>>();
					}
				}
			}
			
			
		}
	}
}
