using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

public class Program
{
    static int TID => Thread.CurrentThread.ManagedThreadId;

    static void Log(string msg = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine($"{msg} (T{TID} - {memberName}:{lineNumber})");
    }

    static async Task Main(string[] args)
    {
        var stopWatch = Stopwatch.StartNew();

        Log("Starting");

        List<int> primes = [5, 4, 3, 2, 1, 2, 3, 4, 5];
        await RunnerAsync(primes, Fib);

        Log("Done");

        stopWatch.Stop();

        Log($"Elapsed: {stopWatch.ElapsedMilliseconds}ms");
    }

    static async Task RunnerAsync(List<int> primes, Func<int, int> func)
    {
        var inChannel = Channel.CreateBounded<int>(1);
        var taskChannel = Channel.CreateUnbounded<Task<int>>();
        var outChannel = Channel.CreateBounded<int>(1);

        async Task ReadFromInChannelAndWriteToTaskChannel()
        {
            //Keep reading until the inChannel is completed.
            await foreach (var item in inChannel.Reader.ReadAllAsync())
            {
                var task = Task.Run(() => func(item));
                await taskChannel.Writer.WriteAsync(task);
            }

            taskChannel.Writer.Complete();
            Log("The taskChannel Completed.");
        }
        _ = Task.Run(ReadFromInChannelAndWriteToTaskChannel);

        async Task ReadFromTaskChannelAndWriteToOutChannel()
        {
            //Keep reading until the taskChannel is completed.
            await foreach (var task in taskChannel.Reader.ReadAllAsync())
            {
                var result = await task;
                await outChannel.Writer.WriteAsync(result);
            }

            outChannel.Writer.Complete();
            Log("The outChannel Completed.");
        }
        _ = Task.Run(ReadFromTaskChannelAndWriteToOutChannel);

        var readFromOutChannelTask = Task.Run(async () =>
        {
            var results = new List<int>();

            //Keep reading until the outChannel is completed.
            await foreach (var res in outChannel.Reader.ReadAllAsync())
            {
                results.Add(res);
            }

            Log("Result: " + string.Join(", ", results));
        });

        foreach (var prime in primes)
        {
            await inChannel.Writer.WriteAsync(prime);
        }

        //Signal that the inChannel is completed.
        inChannel.Writer.Complete();
        Log("The inChannel Completed.");

        //Make sure not to return before the readFromOutTask is completed.
        await readFromOutChannelTask;
    }

    // Fibunachi
    static int Fib(int n)
    {
        Thread.Sleep(n * 1000);
        return n;

        //if (n == 0) return 1;
        //if (n == 1) return 1;
        //return Fib(n - 1) + Fib(n - 2);
    }
}