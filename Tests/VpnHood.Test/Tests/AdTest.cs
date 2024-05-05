﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using VpnHood.Client.App;
using VpnHood.Client.App.Abstractions;
using VpnHood.Client.App.Exceptions;
using VpnHood.Common.Collections;
using VpnHood.Common.Utils;
using VpnHood.Server.Access.Managers.File;

namespace VpnHood.Test.Tests;

[TestClass]
public class AdTest : TestBase
{
    private class TestAdService(AdAccessManager accessManager) : IAppAdService
    {
        public bool FailAlways { get; set; }

        public Task ShowAd(IAppUiContext uiContext, string customData, CancellationToken cancellationToken)
        {
            if (FailAlways)
                throw new Exception("Ad failed");

            accessManager.AddAdData(customData);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    private class AdAccessManager(string storagePath, FileAccessManagerOptions options)
        : FileAccessManager(storagePath, options)
    {

        private readonly TimeoutDictionary<string, TimeoutItem> _adsData = new(TimeSpan.FromMinutes(10));
        public bool RejectAllAds { get; set; }

        public void AddAdData(string adData)
        {
            if (!RejectAllAds)
                _adsData.TryAdd(adData, new TimeoutItem());
        }

        protected override bool IsValidAd(string? adData)
        {
            return adData != null && _adsData.TryRemove(adData, out _);
        }
    }

    [TestMethod]
    public async Task Session_must_be_closed_after_few_minutes_if_ad_is_not_accepted()
    {
        // create server
        using var fileAccessManager = new AdAccessManager(TestHelper.CreateAccessManagerWorkingDir(),
            TestHelper.CreateFileAccessManagerOptions());
        using var testAccessManager = new TestAccessManager(fileAccessManager);
        await using var server = TestHelper.CreateServer(testAccessManager);

        // create access item
        var accessItem = fileAccessManager.AccessItem_Create(isAdRequired: true);
        accessItem.Token.ToAccessKey();

        // create client app
        await using var app = TestHelper.CreateClientApp();
        var adService = new TestAdService(fileAccessManager);
        adService.FailAlways = true;
        app.Services.AdService = adService;

        // connect
        var clientProfile = app.ClientProfileService.ImportAccessKey(accessItem.Token.ToAccessKey());
        await Assert.ThrowsExceptionAsync<AdException>(() => app.Connect(clientProfile.ClientProfileId));
        await TestHelper.WaitForClientStateAsync(app, AppConnectionState.None);
    }

    [TestMethod]
    public async Task Session_expiration_must_increase_by_ad()
    {
        // create server
        using var fileAccessManager = new AdAccessManager(TestHelper.CreateAccessManagerWorkingDir(),
            TestHelper.CreateFileAccessManagerOptions());
        using var testAccessManager = new TestAccessManager(fileAccessManager);
        await using var server = TestHelper.CreateServer(testAccessManager);

        // create access item
        var accessItem = fileAccessManager.AccessItem_Create(isAdRequired: true);
        accessItem.Token.ToAccessKey();

        // create client app
        await using var app = TestHelper.CreateClientApp();
        var adService = new TestAdService(fileAccessManager);
        app.Services.AdService = adService;

        // connect
        var clientProfile = app.ClientProfileService.ImportAccessKey(accessItem.Token.ToAccessKey());
        await app.Connect(clientProfile.ClientProfileId);

        // assert
        await VhTestUtil.AssertEqualsWait(null, () => app.State.SessionStatus?.AccessUsage?.ExpirationTime);
    }

    [TestMethod]
    public async Task Session_exception_should_be_short_if_ad_is_not_accepted()
    {
        // create server
        using var fileAccessManager = new AdAccessManager(TestHelper.CreateAccessManagerWorkingDir(),
            TestHelper.CreateFileAccessManagerOptions());
        using var testAccessManager = new TestAccessManager(fileAccessManager);
        await using var server = TestHelper.CreateServer(testAccessManager);

        // create access item
        var accessItem = fileAccessManager.AccessItem_Create(isAdRequired: true);
        accessItem.Token.ToAccessKey();
        fileAccessManager.RejectAllAds = true; // server will reject all ads

        // create client app
        await using var app = TestHelper.CreateClientApp();
        var adService = new TestAdService(fileAccessManager);
        app.Services.AdService = adService;

        // connect
        var clientProfile = app.ClientProfileService.ImportAccessKey(accessItem.Token.ToAccessKey());
        await app.Connect(clientProfile.ClientProfileId);

        // asserts
        Assert.IsNotNull(app.State.SessionStatus?.AccessUsage?.ExpirationTime);
        Assert.IsTrue(app.State.SessionStatus.AccessUsage.ExpirationTime < DateTime.UtcNow.AddMinutes(10));
    }
}