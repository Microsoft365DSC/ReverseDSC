using System.Management.Automation;

namespace ReverseDSC.Commands
{
    /// <summary>
    /// Returns the lowercased username when it is already stored in the central list of required
    /// credentials, and nothing when it is not.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Credentials")]
    [OutputType(typeof(string))]
    public sealed class GetCredentialsCommand : ReverseDscCmdlet
    {
        /// <summary>Username to look up.</summary>
        [Parameter(Mandatory = true)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Writes the stored username, or nothing when it is unknown.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(CredentialRepository.Get(State, UserName));
        }
    }

    /// <summary>
    /// Returns the name of the PSCredential variable that represents the specified user inside the
    /// extracted configuration. Credential variables are always named $Creds&lt;username&gt;, with the
    /// characters that are valid in usernames but not in variable names removed.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Resolve, "Credentials")]
    [OutputType(typeof(string))]
    public sealed class ResolveCredentialsCommand : ReverseDscCmdlet
    {
        /// <summary>Username to build the variable name for.</summary>
        [Parameter(Mandatory = true)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Writes the credential variable name.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(CredentialRepository.Resolve(UserName));
        }
    }

    /// <summary>
    /// Adds the username to the central list of required credentials when it is not already part
    /// of it.
    /// </summary>
    [Cmdlet(VerbsData.Save, "Credentials")]
    public sealed class SaveCredentialsCommand : ReverseDscCmdlet
    {
        /// <summary>Username to store.</summary>
        [Parameter(Mandatory = true)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Stores the username. Writes nothing.</summary>
        protected override void ProcessRecord()
        {
            CredentialRepository.Save(State, UserName);
        }
    }

    /// <summary>
    /// Checks whether the username is already part of the central list of required credentials.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Credentials")]
    [OutputType(typeof(bool))]
    public sealed class TestCredentialsCommand : ReverseDscCmdlet
    {
        /// <summary>Username to check.</summary>
        [Parameter(Mandatory = true)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Writes true when the username is stored, otherwise false.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(CredentialRepository.Test(State, UserName));
        }
    }

    /// <summary>
    /// Adds the username to the list of user accounts the source environment requires, so that
    /// placeholders can be created in the destination environment.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "ReverseDSCUserName")]
    public sealed class AddReverseDscUserNameCommand : ReverseDscCmdlet
    {
        /// <summary>Username to add.</summary>
        [Parameter(Mandatory = true)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Adds the username. Writes nothing.</summary>
        protected override void ProcessRecord()
        {
            UserNameRepository.Add(State, UserName);
        }
    }

    /// <summary>
    /// Clears the list of user accounts the source environment requires.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "ReverseDSCUserNames")]
    public sealed class ClearReverseDscUserNamesCommand : ReverseDscCmdlet
    {
        /// <summary>Empties the list. Writes nothing.</summary>
        protected override void ProcessRecord()
        {
            UserNameRepository.Clear(State);
        }
    }
}
