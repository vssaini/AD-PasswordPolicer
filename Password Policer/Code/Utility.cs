using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace PasswordPolicer.Code
{
    /// <summary>
    /// Provide different methods for getting expiry date.
    /// </summary>
    class Utility
    {
        // Global variables (PSO = Password Setting Object)
        /// <summary>
        /// AD user's attribute that represents PSO in case of Fine grained password policy.
        /// </summary>
        private const string MsDsResultantPso = "msDS-ResultantPSO";

        /// <summary>
        /// AD user's attribute that represents computed password expirty time by AD.
        /// </summary>
        //private const string PassExpiryTime = "msDS-UserPasswordExpiryTimeComputed";

        const int UfDontExpirePasswd = 0x10000;
        private const string DateFormat = "{0:MMMM d, yyyy hh:mm tt}";

        /// <summary>
        /// Get password expiry date for specific user in domain as per policy.
        /// </summary>
        /// <param name="domain">The domain name</param>
        /// <param name="userName">sAMAccountName of AD user</param>
        /// <returns>Return password expiration date</returns>
        public static string GetPasswordExpiryDate(string domain, string userName)
        {
            DirectoryEntry psoEntry = null;

            // Get user for which we need details
            var principalContext = new PrincipalContext(ContextType.Domain, domain, "Administrator", "Pass99");
            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, userName);
            if (userPrincipal == null) return null;

            var userEntry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;

            // Get constructed attribute 'msDS-ResultantPSO' value
            if (userEntry != null)
            {
                // To load constructed attribute else will not work
                userEntry.RefreshCache(new[] { MsDsResultantPso });

                if (userEntry.Properties.Contains(MsDsResultantPso))
                {
                    var pso = userEntry.Properties[MsDsResultantPso].Value as string;

                    if (pso != null)
                    {
                        var ldapPath = string.Format("LDAP://{0}", pso);
                        psoEntry = new DirectoryEntry(ldapPath);
                    }
                }
            }

            // Get account policy
            var accountPolicy = ADUtilities.GetAccountPolicy(domain, "Administrator", "Pass99");
            if (!accountPolicy.MaximumPasswordAge.HasValue) return null;

            // Get expiry date
            var dateTime = GetExpiration(userEntry, psoEntry, accountPolicy);
            return string.Format(DateFormat, dateTime);
        }

        /// <summary>
        /// Get password expiry date for specific user in domain. For more details visit <see href="http://stackoverflow.com/questions/3764327/active-directory-user-password-expiration-date-net-ou-group-policy">here</see>.
        /// </summary>
        /// <param name="domain">The domain name</param>
        /// <param name="userName">sAMAccountName of AD user</param>
        /// <returns>Return password expiration date</returns>
        public static string GetPasswordExpiryDateByWinNT(string domain, string userName)
        {
            DateTime? dateTime;
            using (var userEntry = new DirectoryEntry(string.Format("WinNT://{0}/{1},user", domain, userName)))
            {
                dateTime = (DateTime)userEntry.InvokeGet("PasswordExpirationDate");
            }

            return String.Format(DateFormat, dateTime);
        }

        /// <summary>
        /// Get password expiry date for specific user in domain (2nd Method). For more details visit <see href="http://stackoverflow.com/questions/9768944/how-can-i-find-out-an-adusers-password-expiry-date-or-days-left-until-password">here</see>.
        /// </summary>
        /// <param name="domain">The domain name</param>
        /// <param name="userName">sAMAccountName of AD user</param>
        /// <returns>Return password expiration date</returns>
        public static string GetPasswordExpiryDateByWinNTSecond(string domain, string userName)
        {
            using (var userEntry = new DirectoryEntry(string.Format("WinNT://{0}/{1},user", domain, userName)))
            {
                var maxPasswordAge = (int)userEntry.Properties.Cast<PropertyValueCollection>().First(p => p.PropertyName == "MaxPasswordAge").Value;
                var passwordAge = (int)userEntry.Properties.Cast<PropertyValueCollection>().First(p => p.PropertyName == "PasswordAge").Value;
                var time = TimeSpan.FromSeconds(maxPasswordAge) - TimeSpan.FromSeconds(passwordAge);

                var dateTime = DateTime.Today.Add(time);

                return String.Format(DateFormat, dateTime);
            }
        }

        /// <summary>
        /// Gets the Password Expiration Date for a domain user. Returns MaxValue if never expiring
        /// Returns MinValue if user must change password at next logon.
        /// </summary>
        /// <param name="userEntry">The directory entry for user</param>
        /// <param name="psoEntry">The directory entry for PSO</param>
        /// <param name="policy">The object of account policy</param>
        /// <returns>Return date and time when user's password will expire</returns>
        public static DateTime GetExpiration(DirectoryEntry userEntry, DirectoryEntry psoEntry, AccountPolicy policy)
        {
            DateTime expiryDate;

            #region Get pwdLastSet value

            var flags = (int)userEntry.Properties["userAccountControl"][0];

            //check to see if passwords expire
            if (Convert.ToBoolean(flags & UfDontExpirePasswd))
            {
                //the user's password will never expire
                return DateTime.MaxValue;
            }

            long ticks = GetInt64(userEntry, "pwdLastSet");

            //user must change password at next logon
            if (ticks == 0) return DateTime.MinValue;

            //password has never been set
            if (ticks == -1)
            {
                throw new InvalidOperationException("User does not have a password");
            }

            //get when the user last set their password;
            var pwdLastSet = DateTime.FromFileTime(ticks);

            #endregion

            var psoMaxPassAge = GetPsoMaxPassAge(psoEntry);
            if (psoMaxPassAge == null)
            {
                //use our policy class to determine when it will expire
                expiryDate = policy.MaximumPasswordAge != null ? pwdLastSet.Add((TimeSpan)policy.MaximumPasswordAge) : pwdLastSet.Add(TimeSpan.MaxValue);
            }
            else
            {
                expiryDate = pwdLastSet.Add((TimeSpan)psoMaxPassAge);
            }

            return expiryDate;
        }

        /// <summary>
        /// Get PSO maximum password age (if any).
        /// </summary>
        private static TimeSpan? GetPsoMaxPassAge(DirectoryEntry psoEntry)
        {
            if (psoEntry != null)
            {
                var maxpwdage = GetInt64(psoEntry, "msDS-MaximumPasswordAge");
                var maxPassAgeDate = TimeSpan.FromTicks(GetAbsValue(maxpwdage));
                return maxPassAgeDate;
            }
            return null;
        }

        private static Int64 GetInt64(DirectoryEntry entry, string attr)
        {
            //we will use the marshaling behavior of the searcher
            var ds = new DirectorySearcher(entry, String.Format("({0}=*)", attr), new[] { attr }, SearchScope.Base);
            var sr = ds.FindOne();
            if (sr == null) return -1;

            if (sr.Properties.Contains(attr))
                return (Int64)sr.Properties[attr][0];
            return -1;
        }

        /// <summary>
        /// Invert the interval values. For some odd reason, the intervals are all stored as negative numbers.
        /// </summary>
        private static long GetAbsValue(long longInt)
        {
            return Math.Abs(longInt);
        }

        //private static Int64 GetInt64FromLargeInteger(object comValue)
        //{
        //    int low;
        //    int high;
        //    byte[] valBytes;
        //    IADsLargeInteger longInt = (IADsLargeInteger)comValue;

        //    BitConverter.GetBytes(low).CopyTo(valBytes, 0);
        //    BitConverter.GetBytes(high).CopyTo(valBytes, 4);

        //    return BitConverter.ToInt64(valBytes, 0);


        //}

        //        Function GetInt64FromLargeInteger(byval largeInteger as Object) as Int64

        //dim low as int32
        //dim high as int32
        //dim valBytes(7) as byte

        //dim longInt as IADsLargeInteger = Ctype(largeInteger, IADsLargeInteger)
        //low = longInt.LowPart
        //high = longInt.HighPart

        //BitConverter.GetBytes(low).CopyTo(valBytes, 0)
        //BitConverter.GetBytes(high).CopyTo(valBytes, 4)

        //Return BitConverter.ToInt64(valBytes, 0)

        //End Function

        //private TimeSpan GetTimeLeft(DirectoryEntry user, DateTime willExpire)
        //{
        //    if (willExpire == DateTime.MaxValue)
        //        return TimeSpan.MaxValue;

        //    if (willExpire == DateTime.MinValue)
        //        return TimeSpan.MinValue;

        //    if (willExpire.CompareTo(DateTime.Now) > 0)
        //    {
        //        //the password has not expired
        //        //(pwdLast + MaxPwdAge)- Now = Time Left
        //        return willExpire.Subtract(DateTime.Now);
        //    }

        //    //the password has already expired
        //    return TimeSpan.MinValue;
        //}

    }
}
