using Android.BillingClient.Api;
using Android.Content;
using VpnHood.Client.App.Abstractions;

namespace VpnHood.Client.App.Droid.GooglePlay;

public class GooglePlayBillingService: IAppBillingService
{
    private readonly BillingClient _billingClient;
    private GooglePlayBillingService(Context context)
    {
        var builder = BillingClient.NewBuilder(context);
        builder.SetListener(PurchasesUpdatedListener);
        _billingClient = builder.EnablePendingPurchases().Build();
    }

    public static GooglePlayBillingService Create(Context context)
    {
        return new GooglePlayBillingService(context);
    }

    private void PurchasesUpdatedListener(BillingResult billingResult, IList<Purchase> purchases)
    {
        throw new NotImplementedException();
    }

    public async Task<SubscriptionPlan[]> GetSubscriptionPlans()
    {
        await EnsureConnected();

        var isDeviceSupportSubscription = _billingClient.IsFeatureSupported("subscriptions");//TODO Check parameter
        if (isDeviceSupportSubscription.ResponseCode == BillingResponseCode.FeatureNotSupported)
            throw new NotImplementedException();

        // Set list of the created products in the GooglePlay.
        var productDetailsParams = QueryProductDetailsParams.NewBuilder()
            .SetProductList([
                QueryProductDetailsParams.Product.NewBuilder()
                    .SetProductId("general_subscription")
                    .SetProductType(BillingClient.ProductType.Subs)
                    .Build()
            ])
            .Build();

        // Get products list from GooglePlay.
        var response = await _billingClient.QueryProductDetailsAsync(productDetailsParams);
        if (response.Result.ResponseCode != BillingResponseCode.Ok) throw new Exception($"Could not get products from google. BillingResponseCode: {response.Result.ResponseCode}");
        if (!response.ProductDetails.Any()) throw new Exception($"Product list is empty. ProductList: {response.ProductDetails}");

        var productDetails = response.ProductDetails.First();

        var plans = productDetails.GetSubscriptionOfferDetails();

        var subscriptionPlans = plans
            .Where(plan => plan.PricingPhases.PricingPhaseList.Any())
            .Select(plan => new SubscriptionPlan()
            {
                SubscriptionPlanId = plan.BasePlanId,
                PlanPrice = plan.PricingPhases.PricingPhaseList.First().FormattedPrice,
            })
            .ToArray();

        return subscriptionPlans;
    }

    private async Task EnsureConnected()
    {
        if (_billingClient.IsReady)
         return;

        var billingResult = await _billingClient.StartConnectionAsync();

        if (billingResult.ResponseCode != BillingResponseCode.Ok)
            throw new Exception(billingResult.DebugMessage);
    }

    public void Dispose()
    {
        _billingClient.Dispose();
    }
}