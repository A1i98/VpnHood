﻿using System.Drawing;
using VpnHood.Client.App.Resources;
using VpnHood.Client.App.Win.Common;
using VpnHood.Client.App.Win.Common.WpfSpa;

namespace VpnHood.Client.App.Win.Connect;

public class App : VpnHoodWpfSpaApp
{
    [STAThread]
    public static void Main()
    {
        var app = new App();
        app.Run();
    }
    public override bool SpaListenToAllIps => AppConfigs.Instance.SpaListenToAllIps;
    public override int? SpaDefaultPort => AppConfigs.Instance.SpaDefaultPort;

    protected override AppOptions CreateAppOptions()
    {
        var appConfigs = AppConfigs.Load();

        // load app settings and resources
        var resources = DefaultAppResource.Resources;
        resources.Strings.AppName = appConfigs.AppName;
        resources.Colors.NavigationBarColor = Color.FromArgb(21, 14, 61);
        resources.Colors.WindowBackgroundColor = Color.FromArgb(21, 14, 61);
        resources.Colors.ProgressBarColor = Color.FromArgb(231, 180, 129);

        return new AppOptions("com.vpnhood.connect.windows", AppConfigs.IsDebugMode) {
            UiName = "VpnHoodConnect",
            StorageFolderPath = AppOptions.BuildStorageFolderPath(appId: "VpnHoodConnect"),
            Resource = resources,
            AccessKeys = [appConfigs.DefaultAccessKey],
            UpdateInfoUrl = appConfigs.UpdateInfoUrl,
            UpdaterProvider = new AdvancedInstallerUpdaterProvider(),
            IsAddAccessKeySupported = false,
            SingleLineConsoleLog = false,
            LocalSpaHostName = "my-vpnhood-connect",
            AllowEndPointTracker = appConfigs.AllowEndPointTracker,
            Ga4MeasurementId = appConfigs.Ga4MeasurementId
        };
    }

}