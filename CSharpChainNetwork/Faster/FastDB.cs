using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpChainModel;
using FASTER.core;

namespace CSharpChainNetwork.Faster
{
    public class FastDB
    {
        FasterKVSettings<string, string> config;
        FasterKV<string, string> store;
        public string name;
        FasterKVSettings<byte[], byte []> byteConfig;
        FasterKV<byte[], byte[]> byteStore;
        public FastDB(string path, bool isByte) {
            var log = Devices.CreateLogDevice($"{path}/Snapshot.log");
            byteConfig = new FasterKVSettings<byte[], byte[]>(path) { TryRecoverLatest = true,MutableFraction = 0.9 };
            byteStore = new FasterKV<byte[], byte []>(byteConfig);
            //byteStore.Log.EmptyPageCount = byteStore.Log.BufferSize - 1;
            name = path;
        }
        public void Destroy(bool isByte)
        {
            if (isByte)
            {
                //Console.WriteLine(byteStore.ToString());
                //Console.WriteLine(byteStore.Log.BufferSize);
                
                byteStore.DumpDistribution();
                //Console.WriteLine(byteStore.OverflowBucketCount);
                byteStore.Log.FlushAndEvict(true);
                byteStore.Log.DisposeFromMemory();
                
                byteConfig.Dispose();
                byteStore.Dispose();
                byteStore = null;
                byteConfig = null;
                GC.Collect();
            }
            
        }

        public string SearchForKey(byte [] key)
        {
            byte[] outputBytes = new byte[1];
            var funcs = new SimpleFunctions<byte[], byte[]>();
            using (var session = byteStore.NewSession(funcs))
            {
                byteStore.Log.FlushAndEvict(true);
                var status = session.Read(ref key, ref outputBytes);
                if (status.IsPending)
                {
                    session.CompletePendingWithOutputs(out var iter, true);

                    while (iter.Next())
                    {
                        if (iter.Current.Status.Found)
                        {
                            outputBytes = iter.Current.Output;
                            //Console.WriteLine(iter.Current.Output);
                        }
                        else
                            Console.WriteLine("Not Found in Iter");
                    }
                    iter.Dispose();
                }
            }
            

            return GetString(outputBytes);
        }

        public string SearchForTransaction(byte[] byteKey)
        {
            string key = GetString(byteKey);
            byteKey = GetBytes(key);
            byte[] output = new byte[1];
            var funcs = new SimpleFunctions<byte[], byte[]>();
            var session = byteStore.NewSession(funcs);
            

            byteStore.Log.FlushAndEvict(true);
            var status = session.Read(ref byteKey, ref output);
            if (status.IsPending)
            {
                session.CompletePendingWithOutputs(out var iter, true);

                while (iter.Next())
                {
                    if (iter.Current.Status.Found)
                    {
                        output = iter.Current.Output;
                        //Console.WriteLine(GetString(iter.Current.Output));
                    }
                    else
                        Console.WriteLine("Not Found in Iter");
                }
                iter.Dispose();
            }
            else
                Console.WriteLine("Not Found");
            session.Dispose();
            return GetString(output);
        }

        public void Upsert(byte[] key, byte[] value)
        {
            var funcs = new SimpleFunctions<byte[], byte[]>((a, b) => {
                List<byte> final = a.ToList();
                final.AddRange(b);
                return final.ToArray();
            });
            using (var session = byteStore.NewSession(funcs))
            {
                session.Upsert(ref key, ref value);
                //Console.WriteLine("Taking full checkpoint");
                //.TryInitiateFullCheckpoint(out _, CheckpointType.Snapshot);
                //byteStore.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
                
            };
            
        }

        public void TakeByteCheckPoint()
        {
            var funcs = new SimpleFunctions<byte[], byte[]>((a,b) => {
                List<byte> final = a.ToList();
                final.AddRange(b);
                return final.ToArray();
            });
            using (var session = byteStore.NewSession(funcs))
            {
                Console.WriteLine("Taking full checkpoint");
                byteStore.TryInitiateHybridLogCheckpoint(out _, CheckpointType.Snapshot,tryIncremental:true);
                byteStore.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
            };
        }

        public void Update(byte[] key, byte[] input)
        {
            var funcs = new SimpleFunctions<byte[], byte[]>((a, b) => {
                List<byte> final = a.ToList();
                final.AddRange(b);
                return final.ToArray();
            });
            using (var session = byteStore.NewSession(funcs))
            {
                session.RMW(ref key, ref input);
            };
        }

        public bool Exists()
        {
            if (byteStore.RecoveredVersion == 1)
            {
                return false;
            }
            return true;
        }

        public void BuildWalletIndex(long blockSize, string master)
        {
            int locationLimit = 20000;
            int intBlock = int.Parse(blockSize.ToString());
            BinaryReader reader = new BinaryReader(File.OpenRead(master), Encoding.ASCII);
            long fileLength = reader.BaseStream.Length / blockSize;
            Dictionary<string, StringBuilder> index = new Dictionary<string, StringBuilder>();
            Transaction util = new Transaction();
            for (int i = 3000; i < 5000; i++)
            {
                index.Add(i.ToString(), new StringBuilder());
            }


            for (long i = 1;i<fileLength; i++)
            {
                Console.WriteLine($"{i}/{fileLength}");
                reader.BaseStream.Seek(i * blockSize,SeekOrigin.Begin);
                string blockData = GetString(reader.ReadBytes(intBlock));
                blockData = blockData.Substring(85, 37803);
                HashSet<string> users = util.GetUsersForPointerIndex(blockData);
                foreach (string user in users)
                {
                    index[user].Append($",{i}");
                }

            }
            List<FastDB> dbList = new List<FastDB>();
            dbList.Add(new FastDB($"C:/temp/FASTER/wallets/{0}", true));
            foreach (KeyValuePair<string, StringBuilder> kvp in index)
            {
                int overflowCount = 0;
                string temp = kvp.Value.ToString();
                string[] tempArr = temp.Substring(1).Split(',');
                StringBuilder builder = new StringBuilder();          
                for (int i = 0; i < tempArr.Length; i++)
                {
                    builder.Append(tempArr[i]);
                    builder.Append(',');
                    if (((i % locationLimit) == 0) && i != 0)
                    {  
                        try
                        {
                            dbList[overflowCount].Upsert(GetBytes(kvp.Key), GetBytes(builder.ToString()));
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine(e.Message);
                            dbList.Add(new FastDB($"C:/temp/FASTER/wallets/{overflowCount}", true));
                            dbList[overflowCount].Upsert(GetBytes(kvp.Key), GetBytes(builder.ToString()));
                        }
                        overflowCount++;
                        builder = new StringBuilder();
                    }
                }
                try{
                    dbList[overflowCount].Upsert(GetBytes(kvp.Key), GetBytes(builder.ToString()));
                }
                catch (Exception e)
                {
                    dbList.Add(new FastDB($"C:/temp/FASTER/wallets/{overflowCount}", true));
                    dbList[overflowCount].Upsert(GetBytes(kvp.Key), GetBytes(builder.ToString()));
                }
                
            }
            foreach (FastDB fast in dbList)
            {
                fast.TakeByteCheckPoint();
                fast.Destroy(true);
            }
            //TakeByteCheckPoint();
            reader.Close();
        }

        private byte[] GetBytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }

        private string GetString(byte[] input)
        {
            return Encoding.ASCII.GetString(input);
        }


    }
}
