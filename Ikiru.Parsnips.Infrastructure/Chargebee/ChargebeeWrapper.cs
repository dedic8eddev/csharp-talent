using AutoMapper;
using ChargeBee.Api;
using ChargeBee.Exceptions;
using ChargeBee.Models;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Addon = ChargeBee.Models.Addon;
using Coupon = ChargeBee.Models.Coupon;
using Plan = ChargeBee.Models.Plan;
using Subscription = ChargeBee.Models.Subscription;
using SubscriptionEstimate = Ikiru.Parsnips.Application.Infrastructure.Subscription.Models.SubscriptionEstimate;

namespace Ikiru.Parsnips.Infrastructure.Chargebee
{
    public class ChargebeeWrapper : ISubscription
    {
        private readonly ILogger<ChargebeeWrapper> _logger;
        private readonly IMapper _mapper;
        private readonly IChargebeeSDkWrapper _chargebeeSDkWrapper;

        public ChargebeeWrapper(ILogger<ChargebeeWrapper> logger,
                                IMapper mapper,
                                IChargebeeSDkWrapper chargebeeSDkWrapper)
        {
            _logger = logger;
            _mapper = mapper;
            _chargebeeSDkWrapper = chargebeeSDkWrapper;
        }

        public async Task<CreatedPaymentIntent> CreatePaymentIntent(int amount, string currencyCode, string customerId)
        {
            var createdPaymentIntent = new CreatedPaymentIntent();

            try
            {
                var paymentIntent = await _chargebeeSDkWrapper.CreatePaymentIntent(amount, currencyCode, customerId);
                createdPaymentIntent = _mapper.Map<CreatedPaymentIntent>(paymentIntent.PaymentIntent);
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError($"Unable to create payment intent, message : {ex.Message}");
                createdPaymentIntent.GeneralException = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to create payment intent, message : {ex.Message}");
                createdPaymentIntent.GeneralException = true;
            }

            return createdPaymentIntent;

        }

        public async Task<string> CreateCustomer(Application.Infrastructure.Subscription.Models.Customer customerDetails)
        {
            var customerResult = await _chargebeeSDkWrapper.CreateCustomer(customerDetails);

            var resultStatusCode = (int)customerResult.StatusCode;

            if (resultStatusCode < 200 || resultStatusCode > 299)
            {
                _logger.LogError($"Customer has not been created for '{customerDetails.SearchFirmName}' ('{customerDetails.SearchFirmId}'), email = '{customerDetails.MainEmail}'. Result code {resultStatusCode}.");
                throw new ExternalApiException("Subscription", "Error creating subscription/customer.");
            }

            return customerResult.Customer.Id;
        }

        public async Task UpdateCustomerBillingAddress(string customerId, string vatNumber, string billingAddressLine1, string billingAddressCity,
                                                    string billingAddressCountryCode, string billingAddressZipOrPostCode, string billingAddressEmail)
        {
            try
            {
                var customer = ChargeBee.Models.Customer.UpdateBillingInfo(customerId);

                customer.BillingAddressLine1(billingAddressLine1);
                customer.BillingAddressCity(billingAddressCity);
                customer.BillingAddressZip(billingAddressZipOrPostCode);
                customer.BillingAddressCountry(billingAddressCountryCode);
                customer.BillingAddressEmail(billingAddressEmail);

                if (!string.IsNullOrEmpty(vatNumber))
                {
                    customer.VatNumber(vatNumber);
                }
                else
                {
                    customer.BusinessCustomerWithoutVatNumber(false);
                }

                await customer.RequestAsync();
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError($"Unable to update customer billing address, message : {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to update customer billing address, message : {ex.Message}");
            }
        }

        public async Task<List<ChargebeePlan>> GetActivePlans()
        {
            List<ChargebeePlan> result = new List<ChargebeePlan>();
            try
            {
                var plansList = await Plan.List().Limit(100)
                                          .Status().Is(Plan.StatusEnum.Active)
                                          .RequestAsync();

                foreach (var plan in plansList.List)
                {
                    var addons = plan.Plan.ApplicableAddons?.ToList()?.Select(x => x.Id());

                    result.Add(new ChargebeePlan()
                    {

                        PlanId = plan.Plan.Id,
                        PeriodUnit = ConvertStringToPeriodUnit(plan.Plan.PeriodUnit.ToString("G")),
                        PlanType = ConvertStringToPlanType(plan.Plan.MetaData?["plan"]?.ToString()),
                        DefaultTokens = Convert.ToInt32(plan.Plan.MetaData?["default_tokens"]?.ToString()),
                        Period = plan.Plan.Period,
                        Price = plan.Plan.Price ?? 0,
                        CurrencyCode = plan.Plan.CurrencyCode,
                        ApplicableAddons = addons?.ToList(),
                        Status = (Domain.Enums.PlanStatus)plan.Plan.Status,
                    });
                }
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError($"Unable to get active plans, message : {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to get active plans, message : {ex.Message}");
            }

            return result;
        }

        public async Task<(List<ChargebeeAddon>, string)> GetActiveAddons()
        {
            var result = new List<ChargebeeAddon>();
            try
            {
                var addonList = await Addon.List()
                                   .Status().Is(Addon.StatusEnum.Active)
                                   .Limit(100)
                                   .RequestAsync();

                foreach (var addonEntity in addonList.List)
                {
                    var addon = addonEntity.Addon;

                    var type = Domain.Enums.AddonType.Unknown;
                    var metaData = addon.MetaData;

                    var addonMetaData = metaData?.ToObject<AddonMetaData>();
                    if (addonMetaData != null)
                        type = addonMetaData.Type;

                    result.Add(new ChargebeeAddon
                    {
                        AddonId = addon.Id,
                        AddonType = type,
                        Status = (Domain.Enums.AddonStatus)addon.Status,
                        Period = addon.Period ?? 0,
                        PeriodUnit = (Domain.Enums.PeriodUnitEnum)addon.PeriodUnit,
                        Price = addon.Price ?? 0,
                        CurrencyCode = addon.CurrencyCode
                    });
                }

            }
            catch (Exception ex)
            {
                var errorMessage = $"Unable to get active coupons, message : {ex.Message}";
                _logger.LogError(errorMessage);
                return (result, errorMessage);
            }

            return (result, null);
        }

        public async Task<List<ChargebeeCoupon>> GetActiveCoupons()
        {
            List<ChargebeeCoupon> result = new List<ChargebeeCoupon>();
            try
            {
                var couponList = await Coupon.List().Limit(100)
                                             .Status().Is(Coupon.StatusEnum.Active)
                                             .RequestAsync();

                foreach (var coupon in couponList.List)
                {
                    var applyAutomatically = false;
                    var metaData = coupon.Coupon.MetaData;

                    var couponMetaData = metaData?.ToObject<CouponMetaData>();
                    if (couponMetaData != null)
                        applyAutomatically = couponMetaData.ApplyAutomatically;

                    result.Add(new ChargebeeCoupon()
                    {
                        CouponId = coupon.Coupon.Id,
                        PlanIds = coupon.Coupon.PlanIds,
                        Status = (Domain.Enums.CouponStatus)coupon.Coupon.Status,
                        ValidTill = coupon.Coupon.ValidTill, // Convertion from DateTime to DateTimeOffset, I don't know what kind of DateTime it is: Local, Unspecified or UTC, so, just keep it as it is
                        ApplyAutomatically = applyAutomatically
                    });
                }
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError($"Unable to get active coupons, message : {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to get active coupons, message : {ex.Message}");
            }

            return result;
        }

        public async Task<CreatedSubscription> CreateSubscriptionForCustomer(string customerId, CreateSubscriptionRequest subscriptionRequest, string addonId, int addonQuantity)
        {
            var createdSubscription = new CreatedSubscription();
            EntityResult result;
            try
            {
                var request = Subscription.CreateForCustomer(customerId)
                                          .PlanId(subscriptionRequest.SubscriptionPlanId);

                if (!string.IsNullOrEmpty(subscriptionRequest.PaymentIntentId))
                    request = request.PaymentIntentId(subscriptionRequest.PaymentIntentId);

                if (subscriptionRequest.CouponIds != null && subscriptionRequest.CouponIds.Any())
                    request = request.CouponIds(subscriptionRequest.CouponIds);

                if (!string.IsNullOrEmpty(addonId) && addonQuantity > 0)
                    request = request.AddonId(0, addonId)
                                     .AddonQuantity(0, addonQuantity);

                result = await request.PlanQuantity(subscriptionRequest.UnitQuantity)
                                      .MetaData(JToken.FromObject(new { subscriptionRequest.SearchFirmId }))
                                      .RequestAsync();

                createdSubscription.SubscriptionId = result.Subscription.Id;
                createdSubscription.SubscriptionCurrentTermEnd = result.Subscription.CurrentTermEnd;
                createdSubscription.SubscriptionStatus = (Domain.Chargebee.Subscription.StatusEnum)result.Subscription.Status;
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError(ex, $"Unable to create subscription for search firm '{subscriptionRequest.SearchFirmId}', with {addonQuantity} of '{addonId}' addons.");

                throw new ExternalApiException(nameof(Subscription), "Unable to create subscription.");
            }

            var resultStatusCode = result == null ? 0 : (int)result.StatusCode;
            if (resultStatusCode >= 200 && resultStatusCode <= 299)
                return createdSubscription;

            _logger.LogError($"Subscription has not been created for search firm {subscriptionRequest.SearchFirmId} with {addonQuantity} of '{addonId}' addons. Result code {resultStatusCode}.");
            throw new ExternalApiException("Subscription", "Error creating subscription.");
        }

        public async Task<SubscriptionEstimate> GetEstimateForSubscription(int unitQuantity,
                                                                            string subscriptionPlanId,
                                                                            DateTimeOffset subscriptionStartDate,
                                                                            List<string> couponids,
                                                                            string customerVatNumber,
                                                                            string billingAddressCountryCode = "",
                                                                            string billingAddressZipOrPostCode = "")
        {
            var coupons = couponids == null ? "null" : couponids.Any() ? string.Join(',', couponids) : "<empty>";

            _logger.LogDebug($"Calling estimate for Plan '{subscriptionPlanId}', {unitQuantity} units, starting on '{subscriptionStartDate}' (current time is '{DateTimeOffset.UtcNow}'), coupons: {coupons}");

            var subscriptionEstimate = new SubscriptionEstimate();

            if (unitQuantity == 0)
            {
                unitQuantity = 1;
            }

            EntityResult generatedEstimate = null;
            try
            {
                generatedEstimate = await _chargebeeSDkWrapper.GetEstimateForSubscription(unitQuantity,
                                                                                              subscriptionPlanId,
                                                                                              subscriptionStartDate,
                                                                                              couponids,
                                                                                              customerVatNumber,
                                                                                              billingAddressCountryCode,
                                                                                              billingAddressZipOrPostCode);

                var statusCode = (int)generatedEstimate.StatusCode;
                if (statusCode < 200 || statusCode > 299)
                {
                    _logger.LogWarning($"Estimate for '{subscriptionPlanId}' has not been successful. Payload: {JsonConvert.SerializeObject(generatedEstimate)}");
                    subscriptionEstimate.GeneralException = true;

                    return subscriptionEstimate;
                }

                var invoiceEstimate = generatedEstimate.Estimate?.InvoiceEstimate;
                if (invoiceEstimate?.Total == null)
                {
                    _logger.LogWarning($"Estimate for '{subscriptionPlanId}' returned null or no invoice estimate. Payload: {JsonConvert.SerializeObject(generatedEstimate)}");
                    subscriptionEstimate.GeneralException = true;

                    return subscriptionEstimate;
                }

                subscriptionEstimate.Total = invoiceEstimate.Total.Value;

                var lineItem = invoiceEstimate.LineItems
                                              .Single(i => i.EntityType() == InvoiceEstimate.InvoiceEstimateLineItem.EntityTypeEnum.Plan &&
                                                           i.EntityId() == subscriptionPlanId);

                subscriptionEstimate.Amount = lineItem.Amount().Value;
                subscriptionEstimate.UnitQuantity = lineItem.Quantity().Value;

                if (generatedEstimate.Estimate.InvoiceEstimate.Taxes != null && generatedEstimate.Estimate.InvoiceEstimate.Taxes.Any())
                {
                    subscriptionEstimate.TaxAmount = generatedEstimate.Estimate.InvoiceEstimate.Taxes.FirstOrDefault().Amount();
                }

                if (generatedEstimate.Estimate.InvoiceEstimate.Discounts != null && generatedEstimate.Estimate.InvoiceEstimate.Discounts.Any())
                {
                    subscriptionEstimate.Discount = generatedEstimate.Estimate.InvoiceEstimate.Discounts.FirstOrDefault().Amount();
                }
            }
            catch (InvalidRequestException ex)
            {
                ProcessInvalidRequestSubscriptionException(ex);
                subscriptionEstimate.GeneralException = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General Chargebee estimate exception. Payment response: {JsonConvert.SerializeObject(generatedEstimate)}");
                throw;
            }

            return subscriptionEstimate;

        }

        public async Task<RenewalEstimate> RenewalEstimate(string subscriptionId)
        {
            _logger.LogDebug($"Calling renewal estimate for subscription '{subscriptionId}'");

            var estimate = new RenewalEstimate();

            EntityResult generatedEstimate = null;
            try
            {
                generatedEstimate = await _chargebeeSDkWrapper.SubscriptionRenewalEstimate(subscriptionId);

                var statusCode = (int)generatedEstimate.StatusCode;
                if (statusCode < 200 || statusCode > 299)
                {
                    _logger.LogWarning($"Renewal estimate for '{subscriptionId}' has not been successful. Payload: {JsonConvert.SerializeObject(generatedEstimate)}");
                    estimate.GeneralException = true;

                    return estimate;
                }

                if (generatedEstimate.Estimate == null)
                {
                    _logger.LogWarning($"Renewal estimate for '{subscriptionId}' have not returned estimate payload. Returned payload: {JsonConvert.SerializeObject(generatedEstimate)}");
                    estimate.GeneralException = true;

                    return estimate;
                }

                var subscriptionEstimate = generatedEstimate.Estimate.SubscriptionEstimate;

                estimate.NextBillingAt = subscriptionEstimate.NextBillingAt;
                var invoiceEstimate = generatedEstimate.Estimate.InvoiceEstimate;
                if (invoiceEstimate.AmountDue == null)
                {
                    _logger.LogWarning($"Estimate AmountDue for '{subscriptionId}' returned null or no invoice estimate. Payload: {JsonConvert.SerializeObject(generatedEstimate)}");
                    estimate.GeneralException = true;

                    return estimate;
                }

                estimate.AmountDue = invoiceEstimate.AmountDue.Value;
                estimate.ValueBeforeTax = invoiceEstimate.SubTotal;
                estimate.CurrencyCode = invoiceEstimate.CurrencyCode;

                estimate.PlanQuantity = invoiceEstimate.LineItems
                                                       .Where(i => i.EntityType() == InvoiceEstimate.InvoiceEstimateLineItem.EntityTypeEnum.Plan && i.Quantity() != null)
                                                       .Select(i => i.Quantity().Value)
                                                       .Sum();

                if (generatedEstimate.Estimate.InvoiceEstimate.Taxes != null && generatedEstimate.Estimate.InvoiceEstimate.Taxes.Any())
                {
                    estimate.TaxAmount = generatedEstimate.Estimate.InvoiceEstimate.Taxes.Select(t => t.Amount()).Sum();
                }

                if (generatedEstimate.Estimate.InvoiceEstimate.Discounts != null && generatedEstimate.Estimate.InvoiceEstimate.Discounts.Any())
                {
                    estimate.Discount = generatedEstimate.Estimate.InvoiceEstimate.Discounts.Select(d => d.Amount()).Sum();
                }
            }
            catch (InvalidRequestException ex)
            {
                ProcessInvalidRequestSubscriptionException(ex);
                estimate.GeneralException = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General Chargebee subscription renewal exception. Payment response: {JsonConvert.SerializeObject(generatedEstimate)}");
                throw;
            }

            return estimate;
        }

        private void ProcessInvalidRequestSubscriptionException(InvalidRequestException exception)
        {

            if (string.IsNullOrEmpty(exception.Param))
            {
                _logger.LogError(exception, "Chargebee error returned for estimate");
                return;
            }

            if (exception.Param.Contains("coupon"))
            {
                _logger.LogInformation(exception, "Invalid chargebee coupon used for estimate");
            }

            if (exception.Param.Contains("subscription[plan_id]"))
            {
                _logger.LogInformation(exception, "Invalid chargebee subcription plan used for estimate");
            }
        }

        public static Domain.Enums.PlanType ConvertStringToPlanType(string plan)
        {
            if (!string.IsNullOrEmpty(plan) && Enum.TryParse<Domain.Enums.PlanType>(plan, true, out Domain.Enums.PlanType result))
            {
                return result;
            }
            else
            {
                return Domain.Enums.PlanType.Unknown;
            }
        }

        public static Domain.Enums.PeriodUnitEnum ConvertStringToPeriodUnit(string periodUnit)
        {
            if (!string.IsNullOrEmpty(periodUnit) && Enum.TryParse<Domain.Enums.PeriodUnitEnum>(periodUnit, true, out Domain.Enums.PeriodUnitEnum result))
            {
                return result;
            }
            else
            {
                return Domain.Enums.PeriodUnitEnum.UnKnown;
            }
        }
    }
}
