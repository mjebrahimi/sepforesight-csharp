using System.Runtime.CompilerServices;
using System.Threading.Channels;

public class Program_old
{
    static int TID => Thread.CurrentThread.ManagedThreadId;

    static void Log(string msg = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine($"{msg} (T{TID} - {memberName}:{lineNumber})");
    }

    static async Task Main(string[] args)
    {
        Log("Starting");

        List<int> primes = [2, 3, 5, 7, 11, 7, 5, 9];
        await RunnerAsync(primes, 5, Fib);

        Log("Done");
    }

    static async Task RunnerAsync(List<int> primes, int workers, Func<int, int> func)
    {
        var inChannel = Channel.CreateBounded<int>(1);
        var outChannel = Channel.CreateBounded<int>(1);

        using var countDownEvent = new CountdownEvent(workers);

        async Task Run(int num)
        {
            Log($"Worker {num} started");

            //Keep reading until the inChannel is completed.
            await foreach (var item in inChannel.Reader.ReadAllAsync())
            {
                var result = func(item);
                await outChannel.Writer.WriteAsync(result);
            }

            //Signal that I'm done and wait for other Workers to be done.
            countDownEvent.Signal();
            Log($"Worker {num} is done and waits for others to be done.");
            countDownEvent.Wait();

            if (outChannel.Writer.TryComplete())
                Log("The outChannel Completed.");
        }

        _ = Enumerable.Range(1, workers).Select(Run).ToArray();

        var readFromOutTask = Task.Run(async () =>
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
        await readFromOutTask;
    }

    // Fibunachi
    static int Fib(int n)
    {
        if (n == 0) return 1;
        if (n == 1) return 1;
        return Fib(n - 1) + Fib(n - 2);
    }
}