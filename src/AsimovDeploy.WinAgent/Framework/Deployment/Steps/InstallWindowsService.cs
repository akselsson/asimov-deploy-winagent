using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsimovDeploy.WinAgent.Framework.Common;
using AsimovDeploy.WinAgent.Framework.Models.Units;

namespace AsimovDeploy.WinAgent.Framework.Deployment.Steps
{
    public class InstallWindowsService : IDeployStep
    {
        private readonly InstallableConfig _installableConfig;

        public InstallWindowsService(InstallableConfig installableConfig)
        {
            if (installableConfig == null) throw new ArgumentNullException(nameof(installableConfig));
            _installableConfig = installableConfig;
        }

        public void Execute(DeployContext context) => InstallService(context);

        private void InstallService(DeployContext context)
        {
            if (string.IsNullOrEmpty(_installableConfig.TargetPath))
                _installableConfig.TargetPath = Path.Combine(@"\Services", context.DeployUnit.Name);

            CopyFiles(context);

            if (!string.IsNullOrEmpty(_installableConfig.Install))
                InstallServiceUsingInstallCommandFromConfig(context);
            else if (_installableConfig.InstallType.Equals("NServiceBus", StringComparison.InvariantCultureIgnoreCase))
                InstallNServiceBusHandler(context);
            else if (_installableConfig.InstallType.Equals("TopShelf", StringComparison.InvariantCultureIgnoreCase))
                InstallTopShelfService(context);
            else
                context.Log.Info($"Asimov Install was not able to do it's job! Check 'Installable' config for '{context.DeployUnit.Name}'.");
        }

        private void InstallNServiceBusHandler(DeployContext context)
        {
            context.Log.Info("Service is marked as NServiceBus, running install");

            var exePath = $"{_installableConfig.TargetPath}\\NServiceBus.Host.exe";

            var serviceName = (context.DeployUnit as WindowsServiceDeployUnit)?.ServiceName ?? context.DeployUnit.Name;

            context.Log.Info($"Installing: {exePath} as {serviceName}");

            var args = new List<string>
            {
                "/install",
                "/servicename:" + serviceName,
                "/displayname:" + serviceName,
                "NServiceBus.Production",
                "NServiceBus.PerformanceCounters"
            };

            args.AddRange(context.ParameterValues.Select(credential => $"/{credential.Key}:{credential.Value}"));

            ProcessUtil.ExecuteCommand(exePath, args.ToArray(), context.Log);

        }

        private void InstallTopShelfService(DeployContext context)
        {
            context.Log.InfoFormat("Service is marked as TopShelf, running install");

            var exePath = $"{_installableConfig.TargetPath}\\{_installableConfig.AssemblyName ?? context.DeployUnit.Name}.exe";

            context.Log.Info($"Installing: {exePath}");

            var args = new List<string> { "install" };

            args.AddRange(context.ParameterValues.Select(credential => $"-{credential.Key}={credential.Value}"));

            ProcessUtil.ExecuteCommand(exePath, args.ToArray(), context.Log);

        }

        private void InstallServiceUsingInstallCommandFromConfig(DeployContext context)
        {
            ProcessUtil.ExecutePowershellScript(
                _installableConfig.TargetPath,
                _installableConfig.Install,
                context.ParameterValues,
                context.Log);
        }

        private void CopyFiles(DeployContext context)
        {
            context.Log.Info($"Copying files from {context.TempFolderWithNewVersionFiles} to {_installableConfig.TargetPath}...");
            DirectoryUtil.CopyDirectory(context.TempFolderWithNewVersionFiles, _installableConfig.TargetPath);
        }
    }
}