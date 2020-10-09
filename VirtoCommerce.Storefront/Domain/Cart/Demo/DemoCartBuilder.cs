using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Caching;
using VirtoCommerce.Storefront.Model.Cart;
using VirtoCommerce.Storefront.Model.Cart.Services;
using VirtoCommerce.Storefront.Model.Cart.Validators;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing.Services;
using VirtoCommerce.Storefront.Model.Services;
using VirtoCommerce.Storefront.Model.Subscriptions.Services;
using VirtoCommerce.Storefront.Model.Tax.Services;

namespace VirtoCommerce.Storefront.Domain.Cart.Demo
{
    public class DemoCartBuilder : CartBuilder
    {
        public DemoCartBuilder(
            IWorkContextAccessor workContextAccessor,
            ICartService cartService,
            ICatalogService catalogSearchService,
            IStorefrontMemoryCache memoryCache,
            IPromotionEvaluator promotionEvaluator,
            ITaxEvaluator taxEvaluator,
            ISubscriptionService subscriptionService
            )
            : base(workContextAccessor, cartService, catalogSearchService, memoryCache, promotionEvaluator, taxEvaluator, subscriptionService)
        {
        }




        public override Task RemoveItemAsync(string lineItemId)
        {
            EnsureCartExists();

            var configureLineItem = Cart.ConfiguredItems.FirstOrDefault(x => x.ConfiguredLineItem.Id.Equals(lineItemId, StringComparison.InvariantCulture));

            if (configureLineItem != null)
            {
                foreach (var configuirablePieceLineItem in Cart.Items.Where(x => x.ConfiguredProductId.Equals(lineItemId, StringComparison.InvariantCulture)))
                {
                    Cart.Items.Remove(configuirablePieceLineItem);
                }
            }

            return base.RemoveItemAsync(lineItemId);
        }

        public override async Task<bool> AddItemAsync(AddCartItem addCartItem)
        {
            EnsureCartExists();

            var result = await new AddCartItemValidator(Cart).ValidateAsync(addCartItem, ruleSet: Cart.ValidationRuleSet);
            if (result.IsValid)
            {
                var lineItem = addCartItem.Product.ToLineItem(Cart.Language, addCartItem.Quantity);                
                lineItem.Product = addCartItem.Product;
                lineItem.ConfiguredProductId = addCartItem.ConfiguredProductId;
                if (addCartItem.Price != null)
                {
                    var listPrice = new Money(addCartItem.Price.Value, Cart.Currency);
                    lineItem.ListPrice = listPrice;
                    lineItem.SalePrice = listPrice;
                }
                if (!string.IsNullOrEmpty(addCartItem.Comment))
                {
                    lineItem.Comment = addCartItem.Comment;
                }

                if (!addCartItem.DynamicProperties.IsNullOrEmpty())
                {
                    lineItem.DynamicProperties = new MutablePagedList<DynamicProperty>(addCartItem.DynamicProperties.Select(x => new DynamicProperty
                    {
                        Name = x.Key,
                        Values = new[] { new LocalizedString { Language = Cart.Language, Value = x.Value } }
                    }));
                }

                await AddLineItemAsync(lineItem);
            }
            return result.IsValid;
        }
    }
}
