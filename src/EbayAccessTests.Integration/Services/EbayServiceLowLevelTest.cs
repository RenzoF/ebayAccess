﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayAccess;
using EbayAccess.Misc;
using EbayAccess.Models.ReviseInventoryStatusRequest;
using EbayAccess.Services;
using EbayAccessTests.Integration.TestEnvironment;
using FluentAssertions;
using NUnit.Framework;

namespace EbayAccessTests.Integration.Services
{
	[ TestFixture ]
	public class EbayServiceLowLevelTest : TestBase
	{
		#region ReviseInventoriesStatusAsync
		[ Test ]
		public void ReviseInventoriesStatusAsync_EbayServiceWithNonVariationFixedPriceItems_QuantityUpdatedForAll()
		{
			//A
			var ebayServiceLowLevel = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			var ebayService = new EbayService( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			var temp1 = ebayService.GetActiveProductsAsync( CancellationToken.None, true );
			temp1.Wait();
			var activeProducts = temp1.Result.Where( x => !x.IsItemWithVariations() ).ToList();
			var activeProductWithoutVariations1 = activeProducts.Skip( 0 ).First();
			var activeProductWithoutVariations2 = activeProducts.Skip( 1 ).First();

			//A
			var updateProductsAsyncTask1 = ebayServiceLowLevel.ReviseInventoriesStatusAsync( new List< InventoryStatusRequest >
			{
				new InventoryStatusRequest { ItemId = activeProductWithoutVariations1.ItemId.ToLongOrDefault(), Quantity = activeProductWithoutVariations1.Quantity + this.QtyUpdateFor },
				new InventoryStatusRequest { ItemId = activeProductWithoutVariations2.ItemId.ToLongOrDefault(), Quantity = activeProductWithoutVariations2.Quantity + this.QtyUpdateFor },
			}, new Guid().ToString() );
			updateProductsAsyncTask1.Wait();

			var updateProductsAsyncTask2 = ebayServiceLowLevel.ReviseInventoriesStatusAsync( new List< InventoryStatusRequest >
			{
				new InventoryStatusRequest { ItemId = activeProductWithoutVariations1.ItemId.ToLongOrDefault(), Quantity = activeProductWithoutVariations1.Quantity },
				new InventoryStatusRequest { ItemId = activeProductWithoutVariations2.ItemId.ToLongOrDefault(), Quantity = activeProductWithoutVariations2.Quantity },
			}, new Guid().ToString() );
			updateProductsAsyncTask2.Wait();

			//A
			updateProductsAsyncTask1.Result.ToList().TrueForAll( x => x.Items.Count == 2 ).Should().Be( true );
			updateProductsAsyncTask2.Result.ToList().TrueForAll( x => x.Items.Count == 2 ).Should().Be( true );

			var item1Update1 = updateProductsAsyncTask1.Result.ToList().First().Items.Where( x => x.ItemId == activeProductWithoutVariations1.ItemId.ToLongOrDefault( false ) ).First();
			var item1Update2 = updateProductsAsyncTask2.Result.ToList().First().Items.Where( x => x.ItemId == activeProductWithoutVariations1.ItemId.ToLongOrDefault( false ) ).First();
			var item2Update1 = updateProductsAsyncTask1.Result.ToList().First().Items.Where( x => x.ItemId == activeProductWithoutVariations2.ItemId.ToLongOrDefault( false ) ).First();
			var item2Update2 = updateProductsAsyncTask2.Result.ToList().First().Items.Where( x => x.ItemId == activeProductWithoutVariations2.ItemId.ToLongOrDefault( false ) ).First();

			( item1Update1.Quantity - item1Update2.Quantity ).Should().Be( this.QtyUpdateFor );
			( item2Update1.Quantity - item2Update2.Quantity ).Should().Be( this.QtyUpdateFor );
		}

		[ Test ]
		public void ReviseInventoriesStatusAsync_EbayServiceWithVariationFixedPriceItems_QuantityUpdatedForAll()
		{
			//A
			var ebayService = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var updateProductsAsyncTask1 = ebayService.ReviseInventoriesStatusAsync( new List< InventoryStatusRequest >
			{
				new InventoryStatusRequest { ItemId = ExistingProducts.FixedPrice1WithVariation1.ItemId, Sku = ExistingProducts.FixedPrice1WithVariation1.Sku, Quantity = ExistingProducts.FixedPrice1WithVariation1.Quantity + this.QtyUpdateFor },
				new InventoryStatusRequest { ItemId = ExistingProducts.FixedPrice1WithVariation2.ItemId, Sku = ExistingProducts.FixedPrice1WithVariation2.Sku, Quantity = ExistingProducts.FixedPrice1WithVariation2.Quantity + this.QtyUpdateFor },
			}, new Guid().ToString() );
			updateProductsAsyncTask1.Wait();
			var updateProductsAsyncTask2 = ebayService.ReviseInventoriesStatusAsync( new List< InventoryStatusRequest >
			{
				ExistingProducts.FixedPrice1WithVariation1,
				ExistingProducts.FixedPrice1WithVariation2,
			}, new Guid().ToString() );
			updateProductsAsyncTask2.Wait();

			//A
			( updateProductsAsyncTask1.Result.ToList().First( x => x.Items[ 0 ].ItemId == ExistingProducts.FixedPrice1WithVariation1.ItemId ).Items[ 0 ].Quantity - updateProductsAsyncTask2.Result.ToList().First( x => x.Items[ 0 ].ItemId == ExistingProducts.FixedPrice1WithVariation1.ItemId ).Items[ 0 ].Quantity ).Should().Be( this.QtyUpdateFor );
			( updateProductsAsyncTask1.Result.ToList().First( x => x.Items[ 0 ].ItemId == ExistingProducts.FixedPrice1WithVariation2.ItemId ).Items[ 0 ].Quantity - updateProductsAsyncTask2.Result.ToList().First( x => x.Items[ 0 ].ItemId == ExistingProducts.FixedPrice1WithVariation2.ItemId ).Items[ 0 ].Quantity ).Should().Be( this.QtyUpdateFor );
		}
		#endregion

		#region GetItems
		[ Test ]
		public void GetItemAsync_EbayServiceWithExistingFixedPriceVariationProduct_HookupItemId()
		{
			//A
			var ebayService = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var inventoryStat1Task = ebayService.GetItemAsync( ExistingProducts.FixedPrice1WithVariation1.ItemId.ToString(), new Guid().ToString() );
			inventoryStat1Task.Wait();
			var inventoryStat1 = inventoryStat1Task.Result;
			//A
			inventoryStat1.ItemId.Should().Be( ExistingProducts.FixedPrice1WithVariation1.ItemId.ToString() );
		}

		[ Test ]
		public void GetItemAsync_EbayServiceWithExistingFixedPriceVariationProduct_HookupItemVariationsSku()
		{
			//A
			var ebayService = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var inventoryStat1Task = ebayService.GetItemAsync( ExistingProducts.FixedPrice1WithVariation1.ItemId.ToString(), new Guid().ToString() );
			inventoryStat1Task.Wait();
			var inventoryStat1 = inventoryStat1Task.Result;

			//A
			inventoryStat1.Variations.TrueForAll( x => !string.IsNullOrWhiteSpace( x.Sku ) ).Should().BeTrue();
		}
		#endregion

		#region GetOrders
		[ Test ]
		public void GetOrdersAsync_GetByIdEbayServiceWithExistingOrders_HookupOrders()
		{
			//A
			var ebayServiceLowLevel = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var ordersTask = ebayServiceLowLevel.GetOrdersAsync( "", ExistingOrdersIds.OrdersIds.ToArray() );
			ordersTask.Wait();
			//A
			ordersTask.Result.Orders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetOrdersAsync_EbayServiceWithExistingOrders_HookupOrders()
		{
			//A
			var ebayServiceLowLevel = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var ordersTask = ebayServiceLowLevel.GetOrdersAsync( ExistingOrdersCreatedInRange.DateFrom, ExistingOrdersCreatedInRange.DateTo, GetOrdersTimeRangeEnum.CreateTime );
			ordersTask.Wait();
			//A
			ordersTask.Result.Orders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetOrdersAsync_EbayServiceWithNotExistingOrders_EmptyOrdersCollection()
		{
			//A
			var ebayServiceLowLevel = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var ordersTask = ebayServiceLowLevel.GetOrdersAsync( NotExistingOrdersInRange.DateFrom, NotExistingOrdersInRange.DateTo, GetOrdersTimeRangeEnum.CreateTime );
			ordersTask.Wait();

			//A
			ordersTask.Result.Orders.Count().Should().Be( 0 );
		}
		#endregion

		#region GetSessionId
		[ Test ]
		public void GetSessionId_EbayServiceWithCorrectRuName_HookupSessionId()
		{
			//A
			var ebayService = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var sessionId = ebayService.GetSessionId( new Guid().ToString() );

			//A
			sessionId.Should().NotBeNullOrWhiteSpace();
		}
		#endregion

		#region Jobs
		[ Test ]
		public async Task CreateUploadJob_EbayServiceWithCorrectRuName_HookupSessionId()
		{
			//A
			var ebayServiceLowLevel = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var createuploadJobResponse = await ebayServiceLowLevel.CreateUploadJobAsync( Guid.NewGuid(), new Guid().ToString() ).ConfigureAwait( false );
			var abortJobResponse = await ebayServiceLowLevel.AbortJobAsync( createuploadJobResponse.JobId, new Guid().ToString() ).ConfigureAwait( false );

			//A
			createuploadJobResponse.Errors.Should().BeNull();
			abortJobResponse.Errors.Should().BeNull();
		}
		#endregion

		[ Test ]
		[ Ignore ]
		public void FetchToken_EbayServiceWithCorrectRuName_HookupToken()
		{
			////Attention!!! This code will regenerate youe credentials!!!

			//A
			var ebayService = new EbayServiceLowLevel( this._credentials.GetEbayUserCredentials(), this._credentials.GetEbayConfigSandbox() );

			//A
			var sessionId = ebayService.GetSessionId( new Guid().ToString() );
			ebayService.AuthenticateUser( sessionId );
			var userToken = ebayService.FetchToken( sessionId, new Guid().ToString() );

			//A
			sessionId.Should().NotBeNullOrWhiteSpace();
			userToken.Should().NotBeNullOrWhiteSpace();
		}
	}
}