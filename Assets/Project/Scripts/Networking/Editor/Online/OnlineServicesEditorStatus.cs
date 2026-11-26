#if UNITY_EDITOR
using UnityEditor;

// Describes the editor's service link without recording account details or access tokens.
public static class OnlineServicesEditorStatus
{
    public static bool IsReady => CloudProjectSettings.projectBound
        && !string.IsNullOrEmpty(CloudProjectSettings.userId)
        && !string.Equals(CloudProjectSettings.userName, "anonymous", System.StringComparison.OrdinalIgnoreCase);

    public static string Describe()
    {
        bool loggedIn = !string.IsNullOrEmpty(CloudProjectSettings.userId)
            && !string.Equals(CloudProjectSettings.userName, "anonymous", System.StringComparison.OrdinalIgnoreCase);
        return "Unity Services editor status\n"
            + "LoggedIn=" + loggedIn + "\n"
            + "ProjectBound=" + CloudProjectSettings.projectBound + "\n"
            + "ProjectIdPresent=" + !string.IsNullOrEmpty(CloudProjectSettings.projectId) + "\n"
            + "ProjectNamePresent=" + !string.IsNullOrEmpty(CloudProjectSettings.projectName) + "\n"
            + "OrganizationIdPresent=" + !string.IsNullOrEmpty(CloudProjectSettings.organizationId) + "\n"
            + "OrganizationNamePresent=" + !string.IsNullOrEmpty(CloudProjectSettings.organizationName);
    }
}
#endif
