#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assemblies;

// Uses the signed-in editor to bind the existing cloud project selected in Player Settings.
public static class OnlineServicesProjectLink
{
    public static async Task LinkConfiguredProjectAsync(string reportPath)
    {
        try
        {
            string projectId = Application.cloudProjectId;
            if (!Guid.TryParse(projectId, out _))
                throw new InvalidOperationException("Player Settings needs a valid existing Cloud Project ID.");
            if (string.IsNullOrEmpty(CloudProjectSettings.userId)
                || string.Equals(CloudProjectSettings.userName, "anonymous", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sign in to Unity in the editor before linking.");
            if (CloudProjectSettings.projectBound)
            {
                if (!string.Equals(projectId, CloudProjectSettings.projectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The editor is already bound to another project.");
                File.WriteAllText(reportPath, "SUCCESS: existing editor link verified.\n" + OnlineServicesEditorStatus.Describe());
                return;
            }
            File.WriteAllText(reportPath, "LINKING: finding the configured project in the signed-in editor's organizations.");
            Type requests = FindEditorType("UnityEditor.Connect.UnityConnectRequests");
            using (var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
            {
                IEnumerable organizations = (IEnumerable)await CallAsync(requests, "GetOrganizationsAsync", cancellation.Token);
                foreach (object organization in organizations)
                {
                    string organizationGenesisId = ReadString(organization, "GenesisId");
                    IEnumerable projects = (IEnumerable)await CallAsync(requests, "GetOrganizationProjectsAsync",
                        organizationGenesisId, cancellation.Token);
                    foreach (object project in projects)
                    {
                        if (!string.Equals(ReadString(project, "Id"), projectId, StringComparison.OrdinalIgnoreCase)) continue;
                        object legacyOrganization = await CallAsync(requests, "LegacyGetOrganizationIdAsync",
                            organizationGenesisId, cancellation.Token);
                        string legacyId = ReadString(legacyOrganization, "LegacyId");
                        string projectName = ReadString(project, "Name");
                        if (string.IsNullOrEmpty(legacyId) || string.IsNullOrEmpty(projectName))
                            throw new InvalidOperationException("Unity returned incomplete project binding details.");
                        // Unity keeps this binding API internal, so isolate access in this editor-only tool.
                        Type connectType = FindEditorType("UnityEditor.Connect.UnityConnect");
                        object connect = connectType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public)
                            ?.GetValue(null);
                        MethodInfo bind = connectType.GetMethod("BindProject", BindingFlags.Instance | BindingFlags.Public,
                            null, new[] { typeof(string), typeof(string), typeof(string) }, null);
                        if (connect == null || bind == null) throw new MissingMethodException("Unity's project binding API changed.");
                        bind.Invoke(connect, new object[] { projectId, projectName, legacyId });
                        while (!CloudProjectSettings.projectBound
                            || string.IsNullOrEmpty(CloudProjectSettings.organizationName))
                            await Task.Delay(200, cancellation.Token);
                        if (!string.Equals(projectId, CloudProjectSettings.projectId, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Unity did not bind the selected project.");
                        File.WriteAllText(reportPath, "SUCCESS: Unity editor project linked.\n" + OnlineServicesEditorStatus.Describe());
                        return;
                    }
                }
                throw new InvalidOperationException("The configured project was not found in this account.");
            }
        }
        catch (Exception exception)
        {
            // Backend exceptions can contain account details; keep them out of the setup report.
            File.WriteAllText(reportPath, "FAILED: editor project linking (" + exception.GetType().Name
                + "). Check Edit > Project Settings > Services.\n" + OnlineServicesEditorStatus.Describe());
        }
    }

    private static Type FindEditorType(string name)
        => CurrentAssemblies.GetLoadedAssemblies().Select(assembly => assembly.GetType(name))
            .FirstOrDefault(type => type != null) ?? throw new TypeLoadException("Unity's editor services API changed.");

    private static string ReadString(object value, string property)
        => value.GetType().GetProperty(property)?.GetValue(value) as string;

    private static async Task<object> CallAsync(Type type, string name, params object[] arguments)
    {
        MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        Task task = (Task)method.Invoke(null, arguments);
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }
}
#endif
