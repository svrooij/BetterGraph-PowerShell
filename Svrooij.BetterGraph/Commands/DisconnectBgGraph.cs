using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Svrooij.BetterGraph.Commands;
/// <summary>
/// Remove the authentication provider from the current session.
/// </summary>
[Cmdlet(VerbsCommunications.Disconnect, "BgGraph")]
public class DisconnectBgGraph : PSCmdlet
{
    /// <inheritdoc />
    protected override void BeginProcessing()
    {
        ConnectBgGraph.ResetAuthenticationProvider();
        base.BeginProcessing();
    }
}
