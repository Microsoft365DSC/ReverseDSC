using System.Management.Automation;

namespace ReverseDSC.Commands
{
    /// <summary>
    /// Base class of the ReverseDSC cmdlets. Resolves the state that belongs to the module the
    /// cmdlet was imported with, so that a re-import starts from an empty state and concurrent
    /// runspaces never share one.
    /// </summary>
    public abstract class ReverseDscCmdlet : PSCmdlet
    {
        internal ModuleState State => ModuleState.For(MyInvocation?.MyCommand?.Module ?? SessionState?.Module);
    }
}
