namespace MyTorrent.HashingServiceProviders
{
    /// <summary>
    /// Configuration options for a <see cref="StandardHashingServiceProvider"/> instance.
    /// </summary>
    public class StandardHashingServiceProviderOptions
    {
        /// <summary>
        /// The name of the hash algorithm that should be used.
        /// </summary>
        public string HashAlgorithm { get; set; } = "SHA256";
    }
}
