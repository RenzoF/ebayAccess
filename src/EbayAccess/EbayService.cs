﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EbayAccess.Misc;
using EbayAccess.Models.Credentials;
using EbayAccess.Models.CredentialsAndConfig;
using EbayAccess.Models.GetOrdersResponse;
using EbayAccess.Models.GetSellerListCustomResponse;
using EbayAccess.Models.GetSellerListResponse;
using EbayAccess.Models.GetSellingManagerSoldListingsResponse;
using EbayAccess.Models.ReviseInventoryStatusRequest;
using EbayAccess.Models.ReviseInventoryStatusResponse;
using EbayAccess.Services;
using Item = EbayAccess.Models.GetSellerListCustomResponse.Item;
using Order = EbayAccess.Models.GetOrdersResponse.Order;

namespace EbayAccess
{
	public class EbayService : IEbayService
	{
		private const int Maxtimerange = 119;
		private readonly DateTime _ebayWorkingStart = new DateTime( 1995, 1, 1, 0, 0, 0 );

		private IEbayServiceLowLevel EbayServiceLowLevel { get; set; }

		private void PopulateOrdersItemsDetails( IEnumerable< Order > orders )
		{
			foreach( var order in orders )
			{
				foreach( var transaction in order.TransactionArray )
				{
					transaction.Item.ItemDetails = this.EbayServiceLowLevel.GetItem( transaction.Item.ItemId );
					transaction.Item.Sku = transaction.Item.ItemDetails.Sku;
				}
			}
		}

		public EbayService( EbayUserCredentials credentials, EbayConfig ebayConfig, IWebRequestServices webRequestServices )
		{
			this.EbayServiceLowLevel = new EbayServiceLowLevel( credentials, ebayConfig, webRequestServices );
		}

		public EbayService( EbayUserCredentials credentials, EbayConfig ebayConfig ) : this( credentials, ebayConfig, new WebRequestServices() )
		{
		}

		/// <summary>
		/// Just for auth
		/// </summary>
		/// <param name="ebayConfig"></param>
		public EbayService( EbayConfig ebayConfig ) : this( new EbayUserCredentials( "empty", "empty" ), ebayConfig, new WebRequestServices() )
		{
		}

		#region GetOrders
		public IEnumerable< Order > GetOrders( DateTime dateFrom, DateTime dateTo )
		{
			var methodParameters = string.Format( "{{dateFrom:{0},dateTo:{1}}}", dateFrom, dateTo );
			var restInfo = this.EbayServiceLowLevel.ToJson();
			const string currentMenthodName = "GetOrders";
			var mark = Guid.NewGuid().ToString();
			try
			{
				EbayLogger.LogTraceStarted( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) );

				var getOrdersResponse = this.EbayServiceLowLevel.GetOrders( dateFrom, dateTo, GetOrdersTimeRangeEnum.ModTime );

				if( getOrdersResponse.Error != null && getOrdersResponse.Error.Any() )
					throw new Exception( string.Join( ",", getOrdersResponse.Error.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) ) );

				var resultOrdersBriefInfo = getOrdersResponse.Orders.ToJson();
				EbayLogger.LogTraceEnded( string.Format( "MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}, MethodResult:{4}", currentMenthodName, restInfo, methodParameters, mark, resultOrdersBriefInfo ) );

				return getOrdersResponse.Orders;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", dateFrom, dateTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Order > > GetOrdersAsync( DateTime dateFrom, DateTime dateTo )
		{
			var methodParameters = string.Format( "{{dateFrom:{0},dateTo:{1}}}", dateFrom, dateTo );
			var restInfo = this.EbayServiceLowLevel.ToJson();
			const string currentMenthodName = "GetOrdersAsync";
			var mark = Guid.NewGuid().ToString();
			try
			{
				EbayLogger.LogTraceStarted( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) );

				var getOrdersResponse = await this.EbayServiceLowLevel.GetOrdersAsync( dateFrom, dateTo, GetOrdersTimeRangeEnum.ModTime ).ConfigureAwait( false );

				if( getOrdersResponse.Error != null && getOrdersResponse.Error.Any() )
					throw new Exception( string.Join( ",", getOrdersResponse.Error.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) ) );

				var resultOrdersBriefInfo = getOrdersResponse.Orders.ToJson();
				EbayLogger.LogTraceEnded( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}, MethodResult:{4}}}", currentMenthodName, restInfo, methodParameters, mark, resultOrdersBriefInfo ) );

				return getOrdersResponse.Orders;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called:{0}", string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< List< string > > GetSaleRecordsNumbersAsync( params string[] saleRecordsIDs )
		{
			var methodParameters = string.Format( "{{}}", string.Join( ",", saleRecordsIDs ) );
			var restInfo = this.EbayServiceLowLevel.ToJson();
			const string currentMenthodName = "GetSaleRecordsNumbersAsync";
			var mark = Guid.NewGuid().ToString();
			try
			{
				EbayLogger.LogTraceStarted( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) );

				var seleRecordsIdsFilteredOnlyExisting = new List< string >();

				if( saleRecordsIDs == null || !saleRecordsIDs.Any() )
					return seleRecordsIdsFilteredOnlyExisting;

				var salerecordIds = saleRecordsIDs.ToList();

				var getSellingManagerOrdersByRecordNumberTasks = salerecordIds.Select( x => this.EbayServiceLowLevel.GetSellngManagerOrderByRecordNumberAsync( x ) );

				var commonTask = Task.WhenAll( getSellingManagerOrdersByRecordNumberTasks );
				await commonTask.ConfigureAwait( false );

				var getSellingManagerSoldListingsResponses = commonTask.Result;

				if( getSellingManagerSoldListingsResponses.Any( x => x.Error != null && x.Error.Any() ) )
				{
					var aggregatedErrors = getSellingManagerSoldListingsResponses.SelectMany( x => x.Error );

					var errrosInfo = aggregatedErrors.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) );

					throw new Exception( string.Join( "; ", errrosInfo ) );
				}

				if( !getSellingManagerSoldListingsResponses.Any() )
					return seleRecordsIdsFilteredOnlyExisting;

				var allReceivedOrders = getSellingManagerSoldListingsResponses.SelectMany( x => x.Orders ).ToList();

				var alllReceivedOrdersDistinct = allReceivedOrders.Distinct( new OrderEqualityComparerByRecordId() ).Select( x => x.SaleRecordID ).ToList();

				seleRecordsIdsFilteredOnlyExisting = ( from s in saleRecordsIDs join d in alllReceivedOrdersDistinct on s equals d select s ).ToList();

				var resultSaleRecordNumbersBriefInfo = seleRecordsIdsFilteredOnlyExisting.ToJson();
				EbayLogger.LogTraceEnded( string.Format( "MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}, MethodResult:{4}", currentMenthodName, restInfo, methodParameters, mark, resultSaleRecordNumbersBriefInfo ) );

				return seleRecordsIdsFilteredOnlyExisting;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called:{0}", string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< List< string > > GetOrdersIdsAsync( params string[] sourceOrdersIds )
		{
			var methodParameters = string.Format( "{{}}", string.Join( ",", sourceOrdersIds ) );
			var restInfo = this.EbayServiceLowLevel.ToJson();
			const string currentMenthodName = "GetOrdersIdsAsync";
			var mark = Guid.NewGuid().ToString();
			try
			{
				EbayLogger.LogTraceStarted( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) );

				var existsOrders = new List< string >();

				if( sourceOrdersIds == null || !sourceOrdersIds.Any() )
					return existsOrders;

				var getOrdersResponse = await this.EbayServiceLowLevel.GetOrdersAsync( sourceOrdersIds ).ConfigureAwait( false );

				if( getOrdersResponse.Error != null && getOrdersResponse.Error.Any() )
					throw new Exception( string.Join( ",", getOrdersResponse.Error.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) ) );

				if( getOrdersResponse.Orders == null )
					return existsOrders;

				var distinctOrdersIds = getOrdersResponse.Orders.Distinct( new OrderEqualityComparerById() ).Select( x => x.GetOrderId( false ) ).ToList();

				existsOrders = ( from s in sourceOrdersIds join d in distinctOrdersIds on s equals d select s ).ToList();

				var resultSaleRecordNumbersBriefInfo = existsOrders.ToJson();
				EbayLogger.LogTraceEnded( string.Format( "MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}, MethodResult:{4}", currentMenthodName, restInfo, methodParameters, mark, resultSaleRecordNumbersBriefInfo ) );

				return existsOrders;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called:{0}", string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		internal class OrderEqualityComparerById : IEqualityComparer< Order >
		{
			public bool Equals( Order x, Order y )
			{
				if( ReferenceEquals( x, y ) )
					return true;

				if( ReferenceEquals( x, null ) || ReferenceEquals( y, null ) )
					return false;

				//Check whether the products' properties are equal. 
				return x.GetOrderId() == y.GetOrderId();
			}

			public int GetHashCode( Order order )
			{
				if( ReferenceEquals( order, null ) )
					return 0;

				var hashProductName = string.IsNullOrWhiteSpace( order.GetOrderId() ) ? 0 : order.GetOrderId().GetHashCode();

				return hashProductName;
			}
		}
		#endregion

		#region GetProducts
		public async Task< IEnumerable< Item > > GetActiveProductsAsync()
		{
			var methodParameters = string.Format( "{{{0}}}", PredefinedValues.NotAvailable );
			var restInfo = this.EbayServiceLowLevel.ToJson();
			const string currentMenthodName = "GetActiveProductsAsync";
			var mark = Guid.NewGuid().ToString();
			try
			{
				EbayLogger.LogTraceStarted( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) );

				var sellerListsAsync = await this.EbayServiceLowLevel.GetSellerListCustomResponsesAsync( DateTime.UtcNow, DateTime.UtcNow.AddDays( Maxtimerange ), GetSellerListTimeRangeEnum.EndTime ).ConfigureAwait( false );

				if( sellerListsAsync.Any( x => x.Error != null && x.Error.Any() ) )
				{
					var aggregatedErrors = sellerListsAsync.Where( x => x.Error != null ).ToList().SelectMany( x => x.Error ).ToList();
					var requestsWithErrorsInfo = string.Join( ",", aggregatedErrors.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) );
					throw new Exception( requestsWithErrorsInfo );
				}

				var items = sellerListsAsync.SelectMany( x => x.ItemsSplitedByVariations );

				var resultSellerListBriefInfo = ToJson( items );
				EbayLogger.LogTraceEnded( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}, MethodResult:{4}}}", currentMenthodName, restInfo, methodParameters, mark, resultSellerListBriefInfo ) );

				return items;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called:{0}", string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Item > > GetProductsByEndDateAsync( DateTime endDateFrom, DateTime endDateTo )
		{
			try
			{
				var products = new List< Item >();

				var quartalsStartList = GetListOfTimeRanges( endDateFrom, endDateTo ).ToList();

				var getSellerListAsyncTasks = new List< Task< GetSellerListCustomResponse > >();

				var sellerListAsync = await this.EbayServiceLowLevel.GetSellerListCustomAsync( quartalsStartList[ 0 ], quartalsStartList[ 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.EndTime ).ConfigureAwait( false );

				if( sellerListAsync.Error != null && sellerListAsync.Error.Any() )
					throw new Exception( string.Join( ",", sellerListAsync.Error.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) ) );

				products.AddRange( sellerListAsync.Items );

				for( var i = 1; i < quartalsStartList.Count - 1; i++ )
				{
					getSellerListAsyncTasks.Add( this.EbayServiceLowLevel.GetSellerListCustomAsync( quartalsStartList[ i ], quartalsStartList[ i + 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.EndTime ) );
				}

				await Task.WhenAll( getSellerListAsyncTasks ).ConfigureAwait( false );

				products.AddRange( getSellerListAsyncTasks.SelectMany( task => task.Result.ItemsSplitedByVariations ).ToList() );

				return products;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", endDateFrom, endDateTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Models.GetSellerListResponse.Item > > GetProductsDetailsAsync( DateTime createTimeFromStart, DateTime createTimeFromTo )
		{
			try
			{
				var products = new List< Models.GetSellerListResponse.Item >();

				var quartalsStartList = GetListOfTimeRanges( createTimeFromStart, createTimeFromTo ).ToList();

				var getSellerListAsyncTasks = new List< Task< GetSellerListResponse > >();

				var sellerListAsync = await this.EbayServiceLowLevel.GetSellerListAsync( quartalsStartList[ 0 ], quartalsStartList[ 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.StartTime ).ConfigureAwait( false );

				if( sellerListAsync.Error != null && sellerListAsync.Error.Any() )
					throw new Exception( string.Join( ",", sellerListAsync.Error.Select( x => string.Format( "{{Code:{0},ShortMessage:{1},LongMaeesage:{2}}}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) ) );

				products.AddRange( sellerListAsync.Items );

				for( var i = 1; i < quartalsStartList.Count - 1; i++ )
				{
					getSellerListAsyncTasks.Add( this.EbayServiceLowLevel.GetSellerListAsync( quartalsStartList[ i ], quartalsStartList[ i + 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.StartTime ) );
				}

				await Task.WhenAll( getSellerListAsyncTasks ).ConfigureAwait( false );

				products.AddRange( getSellerListAsyncTasks.SelectMany( task => task.Result.Items ).ToList() );

				var productsDetails = await this.GetItemsAsync( products ).ConfigureAwait( false );

				var productsDetailsDevidedByVariations = SplitByVariationsOrReturnEmpty( productsDetails );

				return productsDetailsDevidedByVariations;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", createTimeFromStart, createTimeFromTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Models.GetSellerListResponse.Item > > GetProductsDetailsAsync()
		{
			try
			{
				return await this.GetProductsDetailsAsync( this._ebayWorkingStart, DateTime.Now ).ConfigureAwait( false );
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		protected static IEnumerable< DateTime > GetListOfTimeRanges( DateTime firstQuartalStart, DateTime lastQuartalEnd )
		{
			if( lastQuartalEnd < firstQuartalStart )
				return Enumerable.Empty< DateTime >();

			var quartalsStart = new List< DateTime > { firstQuartalStart };

			while( firstQuartalStart < lastQuartalEnd )
			{
				firstQuartalStart = firstQuartalStart.AddDays( Maxtimerange );
				quartalsStart.Add( firstQuartalStart < lastQuartalEnd ? firstQuartalStart : lastQuartalEnd );
			}

			return quartalsStart;
		}

		protected async Task< IEnumerable< Models.GetSellerListResponse.Item > > GetItemsAsync( IEnumerable< Models.GetSellerListResponse.Item > items )
		{
			var itemsDetailsTasks = items.Select( x => this.EbayServiceLowLevel.GetItemAsync( x.ItemId ) );

			var productsDetails = await Task.WhenAll( itemsDetailsTasks ).ConfigureAwait( false );

			var productsDetailsDevidedByVariations = SplitByVariationsOrReturnEmpty( productsDetails );

			return productsDetailsDevidedByVariations;
		}

		protected static List< Models.GetSellerListResponse.Item > SplitByVariationsOrReturnEmpty( IEnumerable< Models.GetSellerListResponse.Item > productsDetails )
		{
			var productsDetailsDevidedByVariations = new List< Models.GetSellerListResponse.Item >();

			if( productsDetails == null || !productsDetails.Any() )
				return productsDetailsDevidedByVariations;

			foreach( var productDetails in productsDetails )
			{
				if( productDetails.IsItemWithVariations() && productDetails.HaveMultiVariations() )
					productsDetailsDevidedByVariations.AddRange( productDetails.SplitByVariations() );
				else
					productsDetailsDevidedByVariations.Add( productDetails );
			}
			return productsDetailsDevidedByVariations;
		}
		#endregion

		#region UpdateProducts
		public void UpdateProducts( IEnumerable< InventoryStatusRequest > products )
		{
			var commonCallInfo = string.Empty;
			try
			{
				var productsCount = products.Count();
				var productsTemp = products.ToList();
				commonCallInfo = String.Format( "Products count {0} : {1}", productsCount, string.Join( "|", productsTemp.Select( x => string.Format( "Sku:{0},Qty{1}", x.Sku, x.Quantity ) ).ToList() ) );

				var reviseInventoriesStatus = this.EbayServiceLowLevel.ReviseInventoriesStatus( products );

				if( reviseInventoriesStatus.Any( x => x.Error != null && x.Error.Any() ) )
				{
					var errors = reviseInventoriesStatus.Where( x => x.Error != null ).SelectMany( x => x.Error ).ToList();
					var requestsWithErrorsInfo = string.Join( ",", errors.Select( x => string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) );
					throw new Exception( requestsWithErrorsInfo );
				}
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0})", commonCallInfo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< InventoryStatusResponse > > UpdateProductsAsync( IEnumerable< InventoryStatusRequest > products )
		{
			var methodParameters = ToJson( products );
			var restInfo = this.EbayServiceLowLevel.ToJson();
			const string currentMenthodName = "UpdateProductsAsync";
			var mark = Guid.NewGuid().ToString();

			try
			{
				EbayLogger.LogTraceStarted( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) );

				var reviseInventoriesStatus = await this.EbayServiceLowLevel.ReviseInventoriesStatusAsync( products ).ConfigureAwait( false );

				if( reviseInventoriesStatus.Any( x => x.Error != null && x.Error.Any() ) )
				{
					var errors = reviseInventoriesStatus.Where( x => x.Error != null ).SelectMany( x => x.Error ).ToList();
					var requestsWithErrorsInfo = string.Join( ",", errors.Select( x => string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", x.ErrorCode, x.ShortMessage, x.LongMessage ) ) );
					throw new Exception( requestsWithErrorsInfo );
				}

				var resultOrdersBriefInfo = ToJson( reviseInventoriesStatus.SelectMany( x => x.Items ) );
				EbayLogger.LogTraceEnded( string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}, MethodResult:{4}}}", currentMenthodName, restInfo, methodParameters, mark, resultOrdersBriefInfo ) );

				return reviseInventoriesStatus;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error.{0})", string.Format( "{{MethodName:{0}, RestInfo:{1}, MethodParameters:{2}, Mark:{3}}}", currentMenthodName, restInfo, methodParameters, mark ) ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}
		#endregion

		#region Authentication
		public string GetUserToken()
		{
			try
			{
				var sessionId = this.EbayServiceLowLevel.GetSessionId();
				this.EbayServiceLowLevel.AuthenticateUser( sessionId );
				var userToken = this.EbayServiceLowLevel.FetchToken( sessionId );
				return userToken;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public string GetUserSessionId()
		{
			try
			{
				var sessionId = this.EbayServiceLowLevel.GetSessionId();
				return sessionId;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public string GetAuthUri( string sessionId )
		{
			try
			{
				var uri = this.EbayServiceLowLevel.GetAuthenticationUri( sessionId );
				return uri.AbsoluteUri;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with({0})", sessionId ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public string FetchUserToken( string sessionId )
		{
			try
			{
				var userToken = this.EbayServiceLowLevel.FetchToken( sessionId );
				return userToken;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with({0})", sessionId ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}
		#endregion

		private static void LogTraceException( string message, EbayException ebayException )
		{
			EbayLogger.Log().Trace( message, ebayException );
		}

		private static string ToJson( IEnumerable< InventoryStatusRequest > source )
		{
			var orders = source as IList< InventoryStatusRequest > ?? source.ToList();
			var items = string.Join( ",", orders.Select( x => string.Format( "{{id:{0},sku:{1},qty:{2}}}",
				x.ItemId == null ? PredefinedValues.NotAvailable : x.ItemId.Value.ToString( CultureInfo.InvariantCulture ),
				string.IsNullOrWhiteSpace( x.Sku ) ? PredefinedValues.NotAvailable : x.Sku,
				x.Quantity == null ? PredefinedValues.NotAvailable : x.Quantity.ToString() ) ) );
			var res = string.Format( "{{Count:{0}, Items:[{1}]}}", orders.Count(), items );
			return res;
		}

		private static string ToJson( IEnumerable< Models.ReviseInventoryStatusResponse.Item > source )
		{
			var orders = source as IList< Models.ReviseInventoryStatusResponse.Item > ?? source.ToList();
			var items = string.Join( ",", orders.Select( x => string.Format( "{{id:{0},sku:{1},qty:{2}}}",
				x.ItemId.HasValue ? x.ItemId.Value.ToString( CultureInfo.InvariantCulture ) : PredefinedValues.NotAvailable,
				string.IsNullOrWhiteSpace( x.Sku ) ? PredefinedValues.NotAvailable : x.Sku,
				x.Quantity.ToString() ) ) );
			var res = string.Format( "{{Count:{0}, Items:[{1}]}}", orders.Count(), items );
			return res;
		}

		private static string ToJson( IEnumerable< Item > source )
		{
			var orders = source as IList< Item > ?? source.ToList();
			var items = string.Join( ",", orders.Select( x => string.Format( "{{id:{0},sku:{1},qty:{2}}}",
				string.IsNullOrWhiteSpace( x.ItemId ) ? PredefinedValues.NotAvailable : x.ItemId,
				string.IsNullOrWhiteSpace( x.Sku ) ? PredefinedValues.NotAvailable : x.Sku,
				x.Quantity.ToString( CultureInfo.InvariantCulture ) ) ) );
			var res = string.Format( "{{Count:{0}, Items:[{1}]}}", orders.Count(), items );
			return res;
		}
	}
}