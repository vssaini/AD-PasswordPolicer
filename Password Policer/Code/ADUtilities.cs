using System;
using System.DirectoryServices;
using System.Text;

namespace PasswordPolicer.Code
{
    /// <summary>
    /// Provide different methods for performing tasks with active directory.
    /// </summary>
    class ADUtilities
    {
        /// <summary>
        /// The LDAP path root
        /// </summary>
        private const string LDAPPathRoot = "LDAP://";

        /// <summary>
        ///     Gets the account policy for the domain
        /// </summary>
        /// <param name="domainName">The domain name</param>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <returns>The domain account policy</returns>
        public static AccountPolicy GetAccountPolicy(string domainName, string username, string password)
        {
            // Init return value
            var policy = new AccountPolicy();

            // Format the path for the root entry
            string rootPath = string.Format("{0}{1}", LDAPPathRoot, GetDomainDNFromName(domainName));

            // Get root directory entry
            using (var entry = new DirectoryEntry(rootPath, username, password))
            {
                // Create a directory searcher to retrieve the password policy
                using (var ds = new DirectorySearcher(entry))
                {
                    ds.Filter = "(objectClass=domainDNS)";
                    ds.PropertiesToLoad.AddRange(new[]
                    {
                        "maxPwdAge", "minPwdAge", "minPwdLength", "pwdProperties", "pwdHistoryLength", "lockoutDuration",
                        "lockOutObservationWindow", "lockoutThreshold"
                    });
                    ds.SearchScope = SearchScope.Base;

                    // Find the domain dns
                    SearchResult result = ds.FindOne();

                    // Check for maximum password age
                    if (result.Properties.Contains("maxPwdAge"))
                    {
                        // Get the maximum password age in ticks
                        long ticks = Math.Abs((long)result.Properties["maxPwdAge"][0]);

                        // If not 0 (set), set the policy property
                        if (ticks != 0)
                            policy.MaximumPasswordAge = TimeSpan.FromTicks(ticks);
                    }

                    // Check for minimum password age
                    if (result.Properties.Contains("minPwdAge"))
                    {
                        // Get the minimum password age in ticks
                        long ticks = Math.Abs((long)result.Properties["minPwdAge"][0]);

                        // If not 0 (set), set the policy property
                        if (ticks != 0)
                            policy.MinimumPasswordAge = TimeSpan.FromTicks(ticks);
                    }

                    // Check for minimum password age
                    if (result.Properties.Contains("minPwdLength"))
                    {
                        policy.MinimumPasswordLength = (int)result.Properties["minPwdLength"][0];
                    }

                    // Check for password history length
                    if (result.Properties.Contains("pwdHistoryLength"))
                    {
                        policy.PasswordHistoryLength = (int)result.Properties["pwdHistoryLength"][0];
                    }

                    // Check for password properties flags
                    if (result.Properties.Contains("pwdProperties"))
                    {
                        // Cast password properties into an enum (flags)
                        policy.PasswordProperties = (PasswordProperties)result.Properties["pwdProperties"][0];
                    }

                    // Check for lockout duration
                    if (result.Properties.Contains("lockoutDuration"))
                    {
                        long ticks = Math.Abs((long)result.Properties["lockoutDuration"][0]);

                        // If not 0 (set), set the policy property
                        if (ticks != 0)
                            policy.LockoutDuration = new TimeSpan(ticks);
                    }

                    // Check for lockout threshold
                    if (result.Properties.Contains("lockoutThreshold"))
                    {
                        policy.LockoutThreshold = (int)result.Properties["lockoutThreshold"][0];
                    }

                    // Check for lockout observation window
                    if (result.Properties.Contains("lockOutObservationWindow"))
                    {
                        long ticks = Math.Abs((long)result.Properties["lockOutObservationWindow"][0]);

                        // If not 0 (set), set the policy property
                        if (ticks != 0)
                            policy.LockoutObservationWindow = new TimeSpan(ticks);
                    }
                }
            }

            return policy;
        }

        /// <summary>
        ///     Returns the domain's DN (distinguishedName) from its name
        /// </summary>
        /// <param name="domainName">The domain name</param>
        /// <returns>The domain's DN (distinguishedName) from its name</returns>
        private static string GetDomainDNFromName(string domainName)
        {
            // Create string builder
            var sbPath = new StringBuilder();

            // Split domain name by dots
            string[] dCs = domainName.Trim().Split('.');

            // Add domain components
            foreach (string t in dCs)
                sbPath.AppendFormat("DC={0},", t);

            // Remove last "," character and return path
            return sbPath.ToString().TrimEnd(',');
        }
    }
}
