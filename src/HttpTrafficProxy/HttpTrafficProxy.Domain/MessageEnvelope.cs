namespace HttpTrafficProxy.Domain;

public record MessageEnvelope(string RequestKey, byte[] Data);
