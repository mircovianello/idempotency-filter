using System.ComponentModel.DataAnnotations;

namespace idempotency_filter.Idempotency;

/// <summary>
/// Caching options.
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// Gets or sets sliding expire time in minutes to use.
    /// </summary>
    /// <value>The sliding expire time in minutes.</value>
    [Required(ErrorMessage = "Setting value for SlidingExpireTimeInMinutes is null or empty!")]
    public int SlidingExpireTimeInMinutes { get; set; }

    /// <summary>
    /// Gets or sets absolute expire time in minutes to use.
    /// </summary>
    /// <value>The absolute expire time in minutes.</value>
    [Required(ErrorMessage = "Setting value for AbsoluteExpireTimeInMinutes is null or empty!")]
    public int AbsoluteExpireTimeInMinutes { get; set; }

    /// <summary>
    /// Idempotency Expiration is a time to cach response
    /// </summary>
    [Required(ErrorMessage = "Setting value for IdempotencyExpirationInSecond is null or empty!")]
    public int IdempotencyExpirationInSecond { get; set; }
}
