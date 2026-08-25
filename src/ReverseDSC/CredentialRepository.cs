namespace ReverseDSC
{
    internal static class CredentialRepository
    {
        internal static string? Get(ModuleState state, string userName)
        {
            string lowered = userName.ToLower();
            return state.Credentials.Contains(lowered) ? lowered : null;
        }

        internal static void Save(ModuleState state, string userName)
        {
            string lowered = userName.ToLower();
            if (!state.Credentials.Contains(lowered))
            {
                state.Credentials.Add(lowered);
            }
        }

        internal static bool Test(ModuleState state, string userName)
        {
            return state.Credentials.Contains(userName.ToLower());
        }

        internal static string Resolve(string userName)
        {
            string[] userNameParts = userName.ToLower().Split('\\');
            return userNameParts.Length > 1
                ? DscValueConverter.FormatCredentialVariableName(userNameParts[1])
                : DscValueConverter.FormatCredentialVariableName(userName);
        }
    }

    internal static class UserNameRepository
    {
        internal static void Add(ModuleState state, string userName)
        {
            if (!state.UserNames.Contains(userName))
            {
                state.UserNames.Add(userName);
            }
        }

        internal static string[] GetAll(ModuleState state)
        {
            return [.. state.UserNames];
        }

        internal static void Clear(ModuleState state)
        {
            state.UserNames.Clear();
        }
    }
}
