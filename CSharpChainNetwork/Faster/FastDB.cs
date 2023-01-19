using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FASTER.core;

namespace CSharpChainNetwork.Faster
{
    public class FastDB
    {
        FasterKVSettings<string, string> config;
        FasterKV<string, string> store;
       public FastDB(string path)
        {
            //"C:/temp/FASTER"
            
            config = new FasterKVSettings<string, string>(path) { TryRecoverLatest = true};
            store = new FasterKV<string, string>(config);

            
            
        }

        public void Destroy()
        {
            store.Dispose();
            config.Dispose();
        }

        public string SearchForValueWith(string key)
        {
            string output = "";
            var funcs = new SimpleFunctions<string, string>((a, b) => a + b);
            var session = store.NewSession(funcs);
            if (Exists(store))
                Console.WriteLine($"Recovered from Snapshot");

            store.Log.FlushAndEvict(true);
            var status = session.Read(ref key, ref output);
            if (status.IsPending)
            {
                session.CompletePendingWithOutputs(out var iter, true);

                while (iter.Next())
                {
                    if (iter.Current.Status.Found) {
                        output = iter.Current.Output;
                        Console.WriteLine(iter.Current.Output);
                    }  
                    else
                        Console.WriteLine("Not Found in Iter");
                }
                iter.Dispose();
            }
            else
                Console.WriteLine("Not Found");
            session.Dispose();
            return output;
        }

        public void Upsert(string key, string value)
        {
            var funcs = new SimpleFunctions<string, string>((a, b) => a + b);
            using (var session = store.NewSession(funcs))
            {
                session.Upsert(ref key, ref value);
                Console.WriteLine("Taking full checkpoint");
                store.TryInitiateFullCheckpoint(out _, CheckpointType.Snapshot);
                store.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
            };
        }

        private bool Exists(FasterKV<string, string> store)
        {
            if (store.RecoveredVersion == 1)
            {
                return false;
            }
            return true;
        }
    }
}
