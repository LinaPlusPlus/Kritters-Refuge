using Robust.Shared.Console;
using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Content.Shared._CR.Serde;
using Content.Server._CR.Serde;

using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Administration.Toolshed;


[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class SerdeCommand : ToolshedCommand
{

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    [CommandImplementation("getDebugLogging")]
    public bool GetDebugLogging(
        IInvocationContext ctx,
        [PipedArgument] EntityUid entity
    )
    {
        if (EntityManager.TryGetComponent<SerdeComponent>(entity, out var debugging)){
            return debugging.DebugLogging;
        }
        return false;
    }

    [CommandImplementation("setDebugLogging")]
    public void SetDebugLogging(
        IInvocationContext ctx,
        [PipedArgument] EntityUid entity,
        [CommandArgument] bool enabled
    )
    {

        EntityUid? invoker = ctx.Session?.AttachedEntity;
        var invokerName = invoker is not null ? EntityManager.ToPrettyString(invoker) : "host console";
        var targetName = EntityManager.ToPrettyString(entity);

        _sawmill = _logManager.GetSawmill("crSerde.logs");

        _sawmill.Warning($"{invokerName:uid} {(enabled ? "enabled" : "disabled")} serde debugging for {targetName:target}");
        _adminLogger.Add(
            LogType.AdminCommands,
            LogImpact.Extreme,
            $"{invokerName:user} {(enabled ? "enabled" : "disabled")} serde debugging for {targetName:target}"
        );

        if (enabled)
        {
            EntityManager.EnsureComponent<SerdeComponent>(entity, out var debugging);
            debugging.DebugLogging = true;
        }
        else
        {
            if (EntityManager.TryGetComponent<SerdeComponent>(entity, out var debugging)) {
                debugging.DebugLogging = false;
            }
        }
    }

    [CommandImplementation("emitIn")]
    public void EmitIn(
        IInvocationContext ctx,
        [PipedArgument] EntityUid entity,
        [CommandArgument] string command,
        [CommandArgument] string text,
        [CommandArgument] int a,
        [CommandArgument] float x,
        [CommandArgument] float y
    )
    {
        int executionId = 0;
        var serdeSystem = EntityManager.System<SerdeSystem>();
        serdeSystem.CommandRaiseIn(entity, executionId, command, text, a, x, y);
    }

    [CommandImplementation("emitOut")]
    public void EmitOut(
        IInvocationContext ctx,
        [PipedArgument] EntityUid entity,
        [CommandArgument] string command,
        [CommandArgument] string text,
        [CommandArgument] int a,
        [CommandArgument] float x,
        [CommandArgument] float y
    )
    {
        int executionId = 0;
        var serdeSystem = EntityManager.System<SerdeSystem>();
        serdeSystem.CommandRaiseOut(entity, executionId, command, text, a, x, y);
    }
}
