namespace Business.Constants;

internal static class WorkflowCacheKeys
{
    // "WorkflowRules_{institutionId}_{eventName}"
    internal static string Rules(int institutionId, string eventName)
        => $"WorkflowRules_{institutionId}_{eventName}";
}
