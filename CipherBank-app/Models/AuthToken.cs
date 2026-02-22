using System;

namespace CipherBank_app.Models;

public record AuthToken(string AccessToken, string RefreshToken, DateTimeOffset ExpiresUtc);