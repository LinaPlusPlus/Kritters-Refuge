using Content.Shared._CR.Serde;
using Content.Server._CR.Serde;

using Robust.Shared.Toolshed;

using Content.Shared.Administration;
using Content.Server.Administration.Logs;

namespace Content.Server.Administration.Toolshed;


[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class SerdeCommand : ToolshedCommand
{

    [CommandImplementation("pause")]
    public void Pause(
        [PipedArgument] EntityUid entity
    )
    {
        EntityManager.EnsureComponent<SerdeComponent>(entity, out var serdeComponent);
        serdeComponent.AcceptingCommands = false;

        var serdeSystem = EntityManager.System<SerdeSystem>();
        serdeSystem.CommandRaiseOut(entity, 0, "paused", "admin", 0, 0, 0);
    }

    [CommandImplementation("resume")]
    public void Resume(
        [PipedArgument] EntityUid entity
    )
    {
        EntityManager.EnsureComponent<SerdeComponent>(entity, out var serdeComponent);
        serdeComponent.AcceptingCommands = true;

        var serdeSystem = EntityManager.System<SerdeSystem>();
        serdeSystem.CommandRaiseOut(entity, 0, "resumed", "admin", 0, 0, 0);
    }

    [CommandImplementation("emitIn")]
    public void EmitIn(
        [PipedArgument] EntityUid entity,
        [CommandArgument] string command,
        [CommandArgument] string text,
        [CommandArgument] int a,
        [CommandArgument] float x,
        [CommandArgument] float y
    )
    {
        var serdeSystem = EntityManager.System<SerdeSystem>();
        serdeSystem.CommandRaiseIn(entity, 0, command, text, a, x, y);
    }

    [CommandImplementation("emitOut")]
    public void EmitOut(
        [PipedArgument] EntityUid entity,
        [CommandArgument] string command,
        [CommandArgument] string text,
        [CommandArgument] int a,
        [CommandArgument] float x,
        [CommandArgument] float y
    )
    {
        var serdeSystem = EntityManager.System<SerdeSystem>();
        serdeSystem.CommandRaiseOut(entity, 0, command, text, a, x, y);
    }
}
