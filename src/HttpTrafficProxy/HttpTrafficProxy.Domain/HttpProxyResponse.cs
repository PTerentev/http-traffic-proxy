namespace HttpTrafficProxy.Domain;

public record HttpProxyResponse(int StatusCode, string? Body);
