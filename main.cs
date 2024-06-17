using System.Threading.Channels;
using System.Runtime.CompilerServices;

public class Program
{
    static int TID => Thread.CurrentThread.ManagedThreadId;

    static void Log(String msg = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine($"{memberName}:{lineNumber} {msg} T{TID}");
    }

    static async Task Main(string[] args)
    {
        Log("Starting");

        var primes = new List<int> { 2, 3, 5, 7, 11, 7, 5, 9 };
        await Runner(primes, 5, Fib);

        Log("Done");
    }

    static async Task Runner(List<int> primes, int workers, Func<int, int> f)
    {
        Log("Starting");
        var inChannel = Channel.CreateBounded<int>(1);
        var outChannel = Channel.CreateBounded<int>(1);

        async Task Run()
        {
            while (true)
            {
                var item = await inChannel.Reader.ReadAsync();
                var result = f(item);
                outChannel.Writer.WriteAsync(result);

            }
        }

        Enumerable.Range(0, workers).Select(num => Run()).ToArray();

        foreach (var prime in primes)
        {
            await inChannel.Writer.WriteAsync(prime);
        }

        var results = new List<int>();

        foreach (var _ in primes)
        {

            var res = await outChannel.Reader.ReadAsync();
            results.Add(res);
        };

        Log(string.Join(", ", results));
    }


    // Fibunachi 
    static int Fib(int n)
    {
        if (n == 0) return 1;
        if (n == 1) return 1;
        return Fib(n - 1) + Fib(n - 2);
    }
}