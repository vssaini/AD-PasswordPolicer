using System;

namespace PasswordPolicer.Code
{
    /// <summary>
    /// Represent account policy for active directory.
    /// </summary>
    public class AccountPolicy
    {
        /// <summary>
        /// The maximum password age
        /// </summary>
        public TimeSpan? MaximumPasswordAge { get; set; }

        /// <summary>
        /// The minimum password age
        /// </summary>
        public TimeSpan? MinimumPasswordAge { get; set; }

        /// <summary>
        /// The minimum password length
        /// </summary>
        public int? MinimumPasswordLength { get; set; }

        /// <summary>
        /// The password history length
        /// </summary>
        public int? PasswordHistoryLength { get; set; }

        /// <summary>
        /// Password properties flags
        /// </summary>
        public PasswordProperties? PasswordProperties { get; set; }

        /// <summary>
        /// The account lockout duration
        /// </summary>
        public TimeSpan? LockoutDuration { get; set; }

        /// <summary>
        /// The account lockout threshold
        /// </summary>
        public int? LockoutThreshold { get; set; }

        /// <summary>
        /// The account lockout observation window (Reset account lockout counter after)
        /// </summary>
        public TimeSpan? LockoutObservationWindow { get; set; }
    }

    /// <summary>
    /// Password properties flags
    /// </summary>
    [Flags]
    public enum PasswordProperties
    {
        /// <summary>
        /// The password must have a mix of at least two of the following types of characters: Uppercase characters, lowercase characters and numerals
        /// </summary>
        DOMAIN_PASSWORD_COMPLEX = 1,

        /// <summary>
        /// The password cannot be changed without logging on. Otherwise, if your password has expired, you can change your password and then log on.
        /// </summary>
        DOMAIN_PASSWORD_NO_ANON_CHANGE = 2,

        /// <summary>
        /// Forces the client to use a protocol that does not allow the domain controller to get the plaintext password.
        /// </summary>
        DOMAIN_PASSWORD_NO_CLEAR_CHANGE = 4,

        /// <summary>
        /// Allows the built-in administrator account to be locked out from network logons.
        /// </summary>
        DOMAIN_LOCKOUT_ADMINS = 8,

        /// <summary>
        /// The directory service is storing a plaintext password for all users instead of a hash function of the password.
        /// </summary>
        DOMAIN_PASSWORD_STORE_CLEARTEXT = 16,

        /// <summary>
        /// Removes the requirement that the machine account password be automatically changed every week.
        /// </summary>
        DOMAIN_REFUSE_PASSWORD_CHANGE = 32
    }
}
