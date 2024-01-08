using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDota.DotaReplay
{
    abstract class MDFactory<T> : IMDFactory where T : class, new()
    {
        private static class InternalClass<T> where T : class, new()
        {
            public static T instance = new T();
        }

        public static T Instance
        {
            get
            {
                return InternalClass<T>.instance;
            }
        }

         Queue<Action> _task_queue;

        public async Task Init()
        {
            _task_queue = new Queue<Action>();
            while (true)
            {
                await Task.Delay(5000);
                if (_task_queue.Count > 0)
                {
                    Action action = _task_queue.Dequeue();
                    await Task.Run(() => { action(); });
                }
            }
        }

        public void Add(MDReplayGenerator mDReplayGenerator)
        {
            mDReplayGenerator.block = true;
            _task_queue.Enqueue(new Action(() =>
            {
                Work(mDReplayGenerator).Wait();
            }));
        }

        virtual public async Task Work(MDReplayGenerator mDReplayGenerator)
        { 
            
        }
    }
}
